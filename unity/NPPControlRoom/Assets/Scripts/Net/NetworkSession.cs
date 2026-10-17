using System;
using System.Collections.Generic;
using NPP.Sim;
using UnityEngine;

namespace NPP.Net
{
    public enum NetRole { Offline, Host, Guest }

    public enum NetPhase { Idle, Connecting, InRoom, Failed }

    public class PeerInfo
    {
        public string id;
        public string name;
        public bool isHost;
    }

    public class ChatMsg
    {
        public string from;
        public string text;
        public bool system;
        public float time;
    }

    /// <summary>
    /// Сессия кооперативной игры. Хост держит симуляцию, гости шлют команды
    /// и получают снапшоты состояния 10 раз в секунду.
    /// </summary>
    public class NetworkSession
    {
        readonly RelayClient _relay = new RelayClient();

        public NetRole Role = NetRole.Offline;
        public NetPhase Phase = NetPhase.Idle;
        public string RoomCode = "";
        public string PlayerName = "Оператор";
        public string MyId = "";
        public string LastError = "";
        public int PingMs = -1;

        public readonly List<PeerInfo> Peers = new List<PeerInfo>();
        public readonly List<ChatMsg> Chat = new List<ChatMsg>();
        public readonly Queue<string> Notices = new Queue<string>();

        public event Action<ControlId, float, string> CommandReceived; // id, value, fromPeerId
        public event Action<PlantSnapshot> StateReceived;             // у гостя
        public event Action<string> RoomClosed;

        float _connectDeadline;
        float _lastPing;
        bool _joined;
        bool _joinSent;
        bool _wasConnected;

        public bool Connected => _relay.Connected;
        public bool InRoom => Phase == NetPhase.InRoom;

        public static string MakeRoomCode()
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var rnd = new System.Random(DateTime.Now.Millisecond * 7919 + Environment.TickCount);
            var chars = new char[4];
            for (int i = 0; i < chars.Length; i++) chars[i] = alphabet[rnd.Next(alphabet.Length)];
            return new string(chars);
        }

        public void StartHost(string room, string playerName)
        {
            RoomCode = string.IsNullOrEmpty(room) ? MakeRoomCode() : room.ToUpperInvariant();
            PlayerName = playerName;
            BeginConnect();
        }

        public void StartJoin(string room, string playerName)
        {
            RoomCode = (room ?? "").Trim().ToUpperInvariant();
            PlayerName = playerName;
            BeginConnect();
        }

        void BeginConnect()
        {
            Leave();
            Role = NetRole.Offline;
            Phase = NetPhase.Connecting;
            _joined = false;
            _joinSent = false;
            Peers.Clear();
            Chat.Clear();
            PingMs = -1;
            _connectDeadline = Time.realtimeSinceStartup + 10f;
            string url = NetConfig.ServerUrl;
            if (!url.Contains("?")) url += "?room=" + RoomCode;
            else url += "&room=" + RoomCode;
            _relay.Connect(url);
            _wasConnected = false;
            AddSystem("Подключение к " + RoomCode + "…");
        }

        void Fail(string err)
        {
            Phase = NetPhase.Failed;
            LastError = string.IsNullOrEmpty(err) ? "Сервер недоступен" : err;
        }

        public void Leave()
        {
            if (Phase != NetPhase.Idle && _relay.Connected)
            {
                _relay.Send(MiniJson.Serialize(new Dictionary<string, object> { { "t", "leave" } }));
            }
            _relay.Close();
            _wasConnected = false;
            _joinSent = false;
            Phase = NetPhase.Idle;
            Role = NetRole.Offline;
            Peers.Clear();
            _joined = false;
        }

        public void Update()
        {
            if (Phase == NetPhase.Connecting)
            {
                if (!_joined && Time.realtimeSinceStartup > _connectDeadline)
                    Fail(Connected ? "Сервер не подтвердил вход (комната занята?)" : "Не удалось подключиться к серверу");
            }

            // как только сокет открыт — представляемся серверу (иначе он не пришлёт joined)
            if (!_joinSent && Phase == NetPhase.Connecting && _relay.Connected)
            {
                _joinSent = true;
                _relay.Send(MiniJson.Serialize(new Dictionary<string, object>
                {
                    { "t", "join" }, { "name", PlayerName }
                }));
            }

            // обрыв уже установленного соединения обрабатываем на главном потоке
            if ((Phase == NetPhase.Connecting || Phase == NetPhase.InRoom) && _wasConnected && !_relay.Connected)
            {
                string err = string.IsNullOrEmpty(_relay.Error) ? "Соединение закрыто" : _relay.Error;
                if (Phase == NetPhase.Connecting)
                {
                    Fail(err);
                }
                else
                {
                    Phase = NetPhase.Failed;
                    LastError = err;
                    var cb = RoomClosed; if (cb != null) cb(LastError);
                }
            }
            if (_relay.Connected) _wasConnected = true;

            string raw;
            while (_relay.Inbox.TryDequeue(out raw))
            {
                Dictionary<string, object> msg;
                try { msg = MiniJson.Deserialize(raw) as Dictionary<string, object>; }
                catch { continue; }
                if (msg == null) continue;
                Handle(msg);
            }

            if (Phase == NetPhase.InRoom && Time.realtimeSinceStartup - _lastPing > 3f)
            {
                _lastPing = Time.realtimeSinceStartup;
                _relay.Send(MiniJson.Serialize(new Dictionary<string, object>
                {
                    { "t", "ping" }, { "ts", Time.realtimeSinceStartup }
                }));
            }
        }

        void Handle(Dictionary<string, object> m)
        {
            string t = JsonHelp.Str(m, "t");
            switch (t)
            {
                case "joined":
                    _joined = true;
                    Phase = NetPhase.InRoom;
                    MyId = JsonHelp.Str(m, "id");
                    string role = JsonHelp.Str(m, "role");
                    Role = role == "host" ? NetRole.Host : NetRole.Guest;
                    RoomCode = JsonHelp.Str(m, "room", RoomCode);
                    ParsePeers(m);
                    AddSystem(Role == NetRole.Host
                        ? "Комната " + RoomCode + " создана. Вы — ХОСТ (ваш компьютер считает физику)."
                        : "Вы подключены к комнате " + RoomCode + " как гость.");
                    break;

                case "peers":
                    ParsePeers(m);
                    break;

                case "cmd":
                    {
                        ControlId id = (ControlId)JsonHelp.Int(m, "id");
                        float value = (float)JsonHelp.Num(m, "value");
                        string from = JsonHelp.Str(m, "from");
                        var cb = CommandReceived; if (cb != null) cb(id, value, from);
                    }
                    break;

                case "state":
                    {
                        var st = MiniJson.Deserialize(JsonHelp.Str(m, "state")) as Dictionary<string, object>;
                        if (st != null)
                        {
                            var snap = SnapshotCodec.FromDict(st);
                            var cb = StateReceived; if (cb != null) cb(snap);
                        }
                    }
                    break;

                case "chat":
                    Chat.Add(new ChatMsg
                    {
                        from = JsonHelp.Str(m, "name", "?"),
                        text = JsonHelp.Str(m, "text"),
                        time = Time.realtimeSinceStartup
                    });
                    TrimChat();
                    break;

                case "pong":
                    {
                        double ts = JsonHelp.Num(m, "ts", -1);
                        if (ts >= 0) PingMs = Mathf.RoundToInt((Time.realtimeSinceStartup - (float)ts) * 1000f);
                    }
                    break;

                case "error":
                    LastError = JsonHelp.Str(m, "message", "Ошибка сервера");
                    Notices.Enqueue(LastError);
                    if (!_joined) Fail(LastError);
                    break;

                case "host_left":
                    AddSystem("Хост покинул игру.");
                    var cb2 = RoomClosed; if (cb2 != null) cb2("Хост покинул игру");
                    Phase = NetPhase.Failed;
                    break;
            }
        }

        void ParsePeers(Dictionary<string, object> m)
        {
            Peers.Clear();
            var arr = JsonHelp.Arr(m, "peers");
            if (arr == null) return;
            foreach (var item in arr)
            {
                var d = item as Dictionary<string, object>;
                if (d == null) continue;
                Peers.Add(new PeerInfo
                {
                    id = JsonHelp.Str(d, "id"),
                    name = JsonHelp.Str(d, "name"),
                    isHost = JsonHelp.Str(d, "role") == "host"
                });
            }
        }

        public void SendCommand(ControlId id, float value)
        {
            if (Phase != NetPhase.InRoom) return;
            _relay.Send(MiniJson.Serialize(new Dictionary<string, object>
            {
                { "t", "cmd" }, { "id", (int)id }, { "value", value }
            }));
        }

        public void BroadcastState(PlantSnapshot snap)
        {
            if (Role != NetRole.Host || Phase != NetPhase.InRoom) return;
            var payload = new Dictionary<string, object>
            {
                { "t", "state" },
                { "state", MiniJson.Serialize(SnapshotCodec.ToDict(snap)) }
            };
            _relay.Send(MiniJson.Serialize(payload));
        }

        public void SendChat(string text)
        {
            if (Phase != NetPhase.InRoom || string.IsNullOrEmpty(text)) return;
            text = text.Length > 200 ? text.Substring(0, 200) : text;
            _relay.Send(MiniJson.Serialize(new Dictionary<string, object> { { "t", "chat" }, { "text", text } }));
        }

        public void SendName(string name)
        {
            if (Phase != NetPhase.InRoom || string.IsNullOrEmpty(name)) return;
            _relay.Send(MiniJson.Serialize(new Dictionary<string, object> { { "t", "name" }, { "name", name } }));
        }

        void AddSystem(string text)
        {
            Chat.Add(new ChatMsg { from = "", text = text, system = true, time = Time.realtimeSinceStartup });
            TrimChat();
        }

        void TrimChat()
        {
            while (Chat.Count > 60) Chat.RemoveAt(0);
        }
    }

    /// <summary>Компактный кодек снапшота для сети.</summary>
    public static class SnapshotCodec
    {
        public static Dictionary<string, object> ToDict(PlantSnapshot s)
        {
            return new Dictionary<string, object>
            {
                { "p", Math.Round(s.power, 4) },
                { "rod", Math.Round(s.rod, 4) },
                { "rt", Math.Round(s.rodTarget, 4) },
                { "pm", s.pumps },
                { "pmsk", s.pumpsMask },
                { "vl", Math.Round(s.valve, 4) },
                { "fd", Math.Round(s.feed, 4) },
                { "fa", s.feedAuto ? 1 : 0 },
                { "br", s.breaker ? 1 : 0 },
                { "vt", s.vent ? 1 : 0 },
                { "tf", Math.Round(s.fuelTemp, 1) },
                { "tc", Math.Round(s.coolTemp, 1) },
                { "ps", Math.Round(s.steamPress, 3) },
                { "lv", Math.Round(s.level, 1) },
                { "rpm", Math.Round(s.rpm, 0) },
                { "pel", Math.Round(s.genMW, 0) },
                { "xe", Math.Round(s.xenon, 3) },
                { "rho", Math.Round(s.rho, 6) },
                { "rad", Math.Round(s.rad, 2) },
                { "al", s.alarms },
                { "scr", s.scrammed ? 1 : 0 },
                { "tt", s.turbineTripped ? 1 : 0 },
                { "cd", s.coreDamage ? 1 : 0 },
                { "t", Math.Round(s.t, 2) }
            };
        }

        public static PlantSnapshot FromDict(Dictionary<string, object> d)
        {
            PlantSnapshot s = default(PlantSnapshot);
            s.power = (float)JsonHelp.Num(d, "p");
            s.rod = (float)JsonHelp.Num(d, "rod");
            s.rodTarget = (float)JsonHelp.Num(d, "rt");
            s.pumps = JsonHelp.Int(d, "pm");
            s.pumpsMask = JsonHelp.Int(d, "pmsk", s.pumps == 3 ? 7 : s.pumps);
            s.valve = (float)JsonHelp.Num(d, "vl");
            s.feed = (float)JsonHelp.Num(d, "fd");
            s.feedAuto = JsonHelp.Bool(d, "fa");
            s.breaker = JsonHelp.Bool(d, "br");
            s.vent = JsonHelp.Bool(d, "vt");
            s.fuelTemp = (float)JsonHelp.Num(d, "tf");
            s.coolTemp = (float)JsonHelp.Num(d, "tc");
            s.steamPress = (float)JsonHelp.Num(d, "ps");
            s.level = (float)JsonHelp.Num(d, "lv");
            s.rpm = (float)JsonHelp.Num(d, "rpm");
            s.genMW = (float)JsonHelp.Num(d, "pel");
            s.xenon = (float)JsonHelp.Num(d, "xe");
            s.rho = (float)JsonHelp.Num(d, "rho");
            s.rad = (float)JsonHelp.Num(d, "rad");
            s.alarms = JsonHelp.Int(d, "al");
            s.scrammed = JsonHelp.Bool(d, "scr");
            s.turbineTripped = JsonHelp.Bool(d, "tt");
            s.coreDamage = JsonHelp.Bool(d, "cd");
            s.t = JsonHelp.Num(d, "t");
            return s;
        }
    }

    /// <summary>Откуда брать адрес релея: Resources/server.json + переопределение из меню.</summary>
    public static class NetConfig
    {
        const string PrefKey = "npp.server.url";
        static string _default;

        public static string DefaultUrl
        {
            get
            {
                if (_default != null) return _default;
                try
                {
                    var ta = Resources.Load<TextAsset>("server");
                    if (ta != null)
                    {
                        var d = MiniJson.Deserialize(ta.text) as Dictionary<string, object>;
                        _default = JsonHelp.Str(d, "url", "ws://127.0.0.1:8787/ws");
                    }
                }
                catch { }
                if (string.IsNullOrEmpty(_default)) _default = "ws://127.0.0.1:8787/ws";
                return _default;
            }
        }

        public static string ServerUrl
        {
            get
            {
                string saved = PlayerPrefs.GetString(PrefKey, "");
                return string.IsNullOrEmpty(saved) ? DefaultUrl : saved;
            }
            set
            {
                if (string.IsNullOrEmpty(value) || value == DefaultUrl) PlayerPrefs.DeleteKey(PrefKey);
                else PlayerPrefs.SetString(PrefKey, value);
                PlayerPrefs.Save();
            }
        }
    }
}
