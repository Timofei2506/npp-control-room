using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NPP.Net
{
    /// <summary>
    /// Простой потокобезопасный WebSocket-клиент (System.Net.WebSockets, без сторонних пакетов).
    /// Входящие сообщения складываются в очередь Inbox — разбирать в Update() на главном потоке.
    /// </summary>
    public class RelayClient
    {
        ClientWebSocket _ws;
        CancellationTokenSource _cts;
        readonly ConcurrentQueue<string> _inbox = new ConcurrentQueue<string>();
        readonly ConcurrentQueue<string> _outbox = new ConcurrentQueue<string>();

        public ConcurrentQueue<string> Inbox { get { return _inbox; } }
        public volatile bool Connected;
        public volatile string Error;

        int _generation;

        public void Connect(string url)
        {
            Close();
            _cts = new CancellationTokenSource();
            Error = null;
            Connected = false;
            int gen = ++_generation;
            var token = _cts.Token;
            Task.Run(() => RunAsync(url, token, gen));
        }

        async Task RunAsync(string url, CancellationToken token, int gen)
        {
            try
            {
                _ws = new ClientWebSocket();
                _ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);
                Uri uri = new Uri(url);
                await _ws.ConnectAsync(uri, token).ConfigureAwait(false);
                Connected = true;

                Task recv = ReceiveLoop(token);
                Task send = SendLoop(token);
                await Task.WhenAny(recv, send).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { /* штатное закрытие */ }
            catch (Exception e)
            {
                Error = e.Message;
            }
            finally
            {
                // старое (отменённое) подключение не должно гасить флаг нового
                if (gen == _generation)
                {
                    Connected = false;
                    try { if (_ws != null) _ws.Dispose(); } catch { }
                    _ws = null;
                }
            }
        }

        async Task ReceiveLoop(CancellationToken token)
        {
            var buffer = new byte[16 * 1024];
            var ms = new System.IO.MemoryStream();
            while (!token.IsCancellationRequested && _ws != null && _ws.State == WebSocketState.Open)
            {
                WebSocketReceiveResult res = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), token).ConfigureAwait(false);
                if (res.MessageType == WebSocketMessageType.Close)
                {
                    try { await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).ConfigureAwait(false); } catch { }
                    break;
                }
                ms.Write(buffer, 0, res.Count);
                if (res.EndOfMessage)
                {
                    string msg = Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length);
                    ms.SetLength(0);
                    if (!string.IsNullOrEmpty(msg)) _inbox.Enqueue(msg);
                }
            }
        }

        async Task SendLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                string msg;
                if (_outbox.TryDequeue(out msg))
                {
                    if (_ws == null || _ws.State != WebSocketState.Open) { _outbox.Enqueue(msg); await Task.Delay(50, token).ConfigureAwait(false); continue; }
                    byte[] bytes = Encoding.UTF8.GetBytes(msg);
                    try
                    {
                        await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token).ConfigureAwait(false);
                    }
                    catch
                    {
                        _outbox.Enqueue(msg);
                        await Task.Delay(100, token).ConfigureAwait(false);
                    }
                }
                else
                {
                    await Task.Delay(15, token).ConfigureAwait(false);
                }
            }
        }

        public void Send(string json)
        {
            if (!string.IsNullOrEmpty(json)) _outbox.Enqueue(json);
        }

        public void Close()
        {
            try
            {
                if (_cts != null) _cts.Cancel();
            }
            catch { }
            _cts = null;
        }
    }
}
