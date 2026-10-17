// Сетевой smoke-тест клиента: два NetworkSession против реального релея.
// Хост + гость, команда, снапшот, чат, host_left, передача роли хоста.
// Запуск: mono net.exe ws://127.0.0.1:8787/ws  (или wss://.../ws)
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using NPP.Sim;
using NPP.Net;

class NetTest
{
    static NetTest() { try { Console.OutputEncoding = System.Text.Encoding.UTF8; } catch { } }
    static int fails = 0;
    static void Check(bool ok, string what)
    {
        Console.WriteLine((ok ? "  OK   " : "  FAIL ") + what);
        if (!ok) fails++;
    }

    static void Pump(NetworkSession a, NetworkSession b, int ms)
    {
        var end = DateTime.UtcNow.AddMilliseconds(ms);
        while (DateTime.UtcNow < end)
        {
            SandboxClock.Now += 0.02f;
            a.Update(); b.Update();
            Thread.Sleep(5);
        }
    }

    static void Main(string[] args)
    {
        string url = args.Length > 0 ? args[0] : "ws://127.0.0.1:8788/ws";
        NetConfig.ServerUrl = url;
        Console.WriteLine("URL: " + url);

        var host = new NetworkSession();
        var guest = new NetworkSession();
        ControlId gotCmd = ControlId.None; float gotVal = -1; string gotFrom = null;
        PlantSnapshot gotState = default(PlantSnapshot);
        bool stateSeen = false, hostLeft = false, chatSeen = false;

        host.CommandReceived += (id, v, from) => { gotCmd = id; gotVal = v; gotFrom = from; };
        guest.StateReceived += s => { gotState = s; stateSeen = true; };
        guest.RoomClosed += m => { hostLeft = true; };

        string room = "T" + Environment.TickCount.ToString("X").Substring(Math.Max(0, Environment.TickCount.ToString("X").Length - 3));
        host.StartHost(room, "Хост");
        Pump(host, guest, 1500);
        Check(host.Role == NetRole.Host && host.InRoom, "хост вошёл как Host (role=" + host.Role + ", phase=" + host.Phase + ")");

        guest.StartJoin(room, "Гость");
        Pump(host, guest, 1500);
        Check(guest.Role == NetRole.Guest && guest.InRoom, "гость вошёл как Guest (role=" + guest.Role + ", phase=" + guest.Phase + ")");
        Check(host.Peers.Count == 2 || guest.Peers.Count == 2, "в комнате видно 2 участника (host=" + host.Peers.Count + ", guest=" + guest.Peers.Count + ")");

        guest.SendCommand(ControlId.RodTarget, 0.25f);
        Pump(host, guest, 800);
        Check(gotCmd == ControlId.RodTarget && Math.Abs(gotVal - 0.25f) < 1e-3, "хост получил команду RodTarget=0.25 (got " + gotCmd + "=" + gotVal + ")");

        var sim = new PlantSimulation();
        for (int i = 0; i < 100; i++) sim.Step(0.05);
        host.BroadcastState(sim.Snapshot());
        Pump(host, guest, 800);
        Check(stateSeen, "гость получил снапшот");
        if (stateSeen)
            Check(Math.Abs(gotState.power - sim.Snapshot().power) < 0.01f && Math.Abs(gotState.steamPress - sim.Snapshot().steamPress) < 0.05f,
                  "снапшот совпадает (P=" + gotState.power.ToString("F3") + ", Pst=" + gotState.steamPress.ToString("F2") + ")");

        guest.SendChat("привет с поста");
        Pump(host, guest, 600);
        string last = host.Chat.Count > 0 ? host.Chat[host.Chat.Count - 1].text : "";
        Check(host.Chat.Exists(c => c.text == "привет с поста"), "чат доставлен хосту (последнее: '" + last + "')");

        host.Leave();
        Pump(host, guest, 1200);
        Check(hostLeft, "гость получил host_left после выхода хоста");

        // после освобождения комнаты роль хоста переходит следующему
        var g2 = new NetworkSession();
        g2.StartJoin(room, "Второй");
        Pump(g2, g2, 1200);
        Check(g2.Role == NetRole.Host, "следующий вошедший стал хостом (role=" + g2.Role + ")");

        Console.WriteLine(fails == 0 ? "NET TEST: PASS" : "NET TEST: FAIL (" + fails + ")");
        Environment.Exit(fails == 0 ? 0 : 1);
    }
}
