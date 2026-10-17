/**
 * NPP Control Room — локальный релей для игры по LAN/интернету без Cloudflare.
 * Протокол идентичен облачному воркеру (server/worker).
 *
 * Запуск:  node server.js            (порт 8787)
 * Игра:    в настройках укажите ws://IP-хоста:8787/ws
 */
const http = require("http");
const os = require("os");
const { WebSocketServer } = require("ws");

const PORT = parseInt(process.env.PORT || "8787", 10);
const MAX_PLAYERS = 8;

const rooms = new Map(); // code -> { sockets:Set, hostId:string|null }

const sanitizeRoom = (s) => String(s || "").toUpperCase().replace(/[^A-Z0-9]/g, "").slice(0, 6);
const cleanName = (s) => String(s || "").replace(/[<>&"\n\r\t]/g, "").trim().slice(0, 16) || "Оператор";
const cleanText = (s) => String(s || "").replace(/[<>&\n\r\t]/g, " ").trim().slice(0, 200);

function getRoom(code) {
  let r = rooms.get(code);
  if (!r) {
    r = { code, sockets: new Set(), hostId: null };
    rooms.set(code, r);
  }
  return r;
}

function joined(room) {
  return [...room.sockets].filter((ws) => ws.meta.joined);
}

function send(ws, obj) {
  try {
    if (ws.readyState === ws.OPEN) ws.send(JSON.stringify(obj));
  } catch (e) {}
}

function broadcast(room, obj, exclude = null) {
  for (const ws of joined(room)) {
    if (ws === exclude) continue;
    send(ws, obj);
  }
}

function peersPayload(room) {
  return {
    t: "peers",
    peers: joined(room).map((ws) => ({ id: ws.meta.id, name: ws.meta.name, role: ws.meta.role })),
  };
}

const server = http.createServer((req, res) => {
  if (req.url.startsWith("/health")) {
    res.writeHead(200, { "content-type": "application/json" });
    res.end(JSON.stringify({ ok: true, service: "npp-relay-lan", rooms: rooms.size }));
    return;
  }
  res.writeHead(200, { "content-type": "text/plain; charset=utf-8" });
  res.end("NPP LAN relay. WebSocket: ws://<host>:" + PORT + "/ws?room=CODE");
});

const wss = new WebSocketServer({ noServer: true });

server.on("upgrade", (req, socket, head) => {
  const url = new URL(req.url, "http://localhost");
  if (url.pathname !== "/ws") {
    socket.destroy();
    return;
  }
  const code = sanitizeRoom(url.searchParams.get("room") || "");
  if (!code) {
    socket.destroy();
    return;
  }
  const room = getRoom(code);
  if (joined(room).length >= MAX_PLAYERS) {
    socket.destroy();
    return;
  }
  wss.handleUpgrade(req, socket, head, (ws) => {
    ws.meta = { id: Math.random().toString(36).slice(2, 10), name: "Оператор", role: "guest", joined: false };
    ws.roomCode = code;
    room.sockets.add(ws);
    wss.emit("connection", ws);
  });
});

wss.on("connection", (ws) => {
  const room = getRoom(ws.roomCode);

  ws.on("message", (raw) => {
    let d;
    try {
      d = JSON.parse(raw.toString());
    } catch (e) {
      return;
    }
    const m = ws.meta;

    switch (d.t) {
      case "join": {
        m.name = cleanName(d.name);
        if (!m.joined) {
          m.joined = true;
          const host = joined(room).find((s) => s !== ws && s.meta.role === "host");
          m.role = host ? "guest" : "host";
        }
        send(ws, {
          t: "joined",
          id: m.id,
          role: m.role,
          room: room.code,
          peers: joined(room).map((s) => ({ id: s.meta.id, name: s.meta.name, role: s.meta.role })),
        });
        broadcast(room, peersPayload(room));
        broadcast(room, { t: "chat", name: "СИСТЕМА", text: m.name + " подключился" }, ws);
        break;
      }

      case "cmd": {
        if (m.role === "host") return;
        const host = joined(room).find((s) => s.meta.role === "host");
        if (!host) {
          send(ws, { t: "error", message: "В комнате нет хоста" });
          return;
        }
        send(host, { t: "cmd", id: Number(d.id) | 0, value: Number(d.value) || 0, from: m.id });
        break;
      }

      case "state": {
        if (m.role !== "host") return;
        broadcast(room, { t: "state", state: String(d.state || "") }, ws);
        break;
      }

      case "chat": {
        const text = cleanText(d.text);
        if (!text) return;
        broadcast(room, { t: "chat", name: m.name, text });
        break;
      }

      case "name": {
        m.name = cleanName(d.name);
        broadcast(room, peersPayload(room));
        break;
      }

      case "ping":
        send(ws, { t: "pong", ts: d.ts });
        break;

      case "leave":
        ws.close();
        break;
    }
  });

  ws.on("close", () => {
    const r = getRoom(ws.roomCode);
    r.sockets.delete(ws);
    if (ws.meta && ws.meta.joined) {
      if (ws.meta.role === "host") {
        broadcast(r, { t: "host_left" });
      } else {
        broadcast(r, { t: "chat", name: "СИСТЕМА", text: ws.meta.name + " отключился" });
        broadcast(r, peersPayload(r));
      }
    }
    if (r.sockets.size === 0) rooms.delete(r.code);
  });
});

server.listen(PORT, "0.0.0.0", () => {
  console.log("[NPP] LAN relay on port " + PORT);
  console.log("[NPP] Health: http://localhost:" + PORT + "/health");
  console.log("[NPP] WebSocket: ws://<IP>:" + PORT + "/ws?room=CODE");
  const nets = os.networkInterfaces();
  for (const name of Object.keys(nets)) {
    for (const ni of nets[name] || []) {
      if (ni.family === "IPv4" && !ni.internal) {
        console.log("  LAN IP: ws://" + ni.address + ":" + PORT + "/ws");
      }
    }
  }
});
