/**
 * NPP Control Room — релей комнат на Cloudflare Workers + Durable Objects.
 *
 * Схема:
 *   Игрок → wss://<worker>/ws?room=ABCD → Durable Object комнаты
 *   Первый вошедший — HOST (считает физику). Остальные — гости.
 *   cmd   (гость → хост):  {t:"cmd", id:<int>, value:<float>}
 *   state (хост → гости):  {t:"state", state:"<json-снапшота>"}
 *   chat:                  {t:"chat", text:"..."}
 *
 * Сообщения сервера: joined, peers, cmd, state, chat, pong, error, host_left.
 */

export default {
  async fetch(request, env) {
    const url = new URL(request.url);

    if (url.pathname === "/health") {
      return json({ ok: true, service: "npp-relay", time: Date.now() });
    }

    if (url.pathname === "/ws") {
      const upgrade = (request.headers.get("Upgrade") || "").toLowerCase();
      if (upgrade !== "websocket") {
        return new Response("Expected WebSocket upgrade", { status: 426 });
      }
      const room = sanitizeRoom(url.searchParams.get("room") || "");
      if (!room) return json({ error: "room required" }, 400);
      const id = env.ROOMS.idFromName(room);
      const stub = env.ROOMS.get(id);
      return stub.fetch(request);
    }

    return new Response(
      "NPP relay online. WebSocket endpoint: /ws?room=CODE (4-6 символов A-Z0-9)",
      { status: 200, headers: { "content-type": "text/plain; charset=utf-8" } }
    );
  }
};

function json(obj, status = 200) {
  return new Response(JSON.stringify(obj), {
    status,
    headers: { "content-type": "application/json; charset=utf-8" }
  });
}

function sanitizeRoom(s) {
  return String(s || "").toUpperCase().replace(/[^A-Z0-9]/g, "").slice(0, 6);
}

function cleanName(s) {
  return String(s || "").replace(/[<>&"\n\r\t]/g, "").trim().slice(0, 16) || "Оператор";
}

function cleanText(s) {
  return String(s || "").replace(/[<>&\n\r\t]/g, " ").trim().slice(0, 200);
}

const MAX_PLAYERS = 8;

export class Room {
  constructor(state, env) {
    this.state = state;
    this.env = env;
    this.roomName = "";
  }

  async fetch(request) {
    const url = new URL(request.url);
    this.roomName = sanitizeRoom(url.searchParams.get("room") || "");

    const upgrade = (request.headers.get("Upgrade") || "").toLowerCase();
    if (upgrade !== "websocket") {
      return new Response("Expected WebSocket upgrade", { status: 426 });
    }

    if (this.joinedSockets().length >= MAX_PLAYERS) {
      const pair = new WebSocketPair();
      pair[1].accept();
      pair[1].send(JSON.stringify({ t: "error", message: "Комната заполнена (макс. " + MAX_PLAYERS + ")" }));
      pair[1].close(1013, "room full");
      return new Response(null, { status: 101, webSocket: pair[0] });
    }

    const pair = new WebSocketPair();
    const client = pair[0];
    const server = pair[1];
    this.state.acceptWebSocket(server);
    server.serializeAttachment({
      id: crypto.randomUUID(),
      name: "Оператор",
      role: "guest",
      joined: false
    });

    return new Response(null, { status: 101, webSocket: client });
  }

  joinedSockets() {
    return this.state.getWebSockets().filter((ws) => {
      const a = ws.deserializeAttachment();
      return a && a.joined;
    });
  }

  hasHost() {
    return this.joinedSockets().some((ws) => ws.deserializeAttachment().role === "host");
  }

  send(ws, obj) {
    try {
      ws.send(JSON.stringify(obj));
    } catch (e) {
      /* сокет мог закрыться */
    }
  }

  broadcast(obj, exclude = null) {
    const data = JSON.stringify(obj);
    for (const ws of this.joinedSockets()) {
      if (ws === exclude) continue;
      try {
        ws.send(data);
      } catch (e) {}
    }
  }

  peersPayload() {
    return {
      t: "peers",
      peers: this.joinedSockets().map((ws) => {
        const a = ws.deserializeAttachment();
        return { id: a.id, name: a.name, role: a.role };
      })
    };
  }

  async webSocketMessage(ws, message) {
    let d;
    try {
      d = JSON.parse(message);
    } catch (e) {
      return;
    }
    const meta = ws.deserializeAttachment() || {};
    if (!meta || !meta.id) return;

    switch (d.t) {
      case "join": {
        meta.name = cleanName(d.name);
        if (!meta.joined) {
          meta.joined = true;
          meta.role = this.hasHost() ? "guest" : "host";
        }
        ws.serializeAttachment(meta);

        this.send(ws, {
          t: "joined",
          id: meta.id,
          role: meta.role,
          room: this.roomName,
          peers: this.joinedSockets().map((s) => {
            const a = s.deserializeAttachment();
            return { id: a.id, name: a.name, role: a.role };
          })
        });
        this.broadcast(this.peersPayload());
        this.broadcast(
          { t: "chat", name: "СИСТЕМА", text: meta.name + " подключился" },
          ws
        );
        break;
      }

      case "cmd": {
        const host = this.joinedSockets().find((s) => s.deserializeAttachment().role === "host");
        if (!host) {
          this.send(ws, { t: "error", message: "В комнате нет хоста" });
          return;
        }
        if (meta.role !== "host") {
          this.send(host, {
            t: "cmd",
            id: Number(d.id) | 0,
            value: Number(d.value) || 0,
            from: meta.id
          });
        }
        break;
      }

      case "state": {
        if (meta.role !== "host") return;
        const payload = JSON.stringify({ t: "state", state: String(d.state || "") });
        for (const s of this.joinedSockets()) {
          if (s === ws) continue;
          try {
            s.send(payload);
          } catch (e) {}
        }
        break;
      }

      case "chat": {
        const text = cleanText(d.text);
        if (!text) return;
        this.broadcast({ t: "chat", name: meta.name, text });
        break;
      }

      case "name": {
        meta.name = cleanName(d.name);
        ws.serializeAttachment(meta);
        this.broadcast(this.peersPayload());
        break;
      }

      case "ping": {
        this.send(ws, { t: "pong", ts: d.ts });
        break;
      }

      case "leave": {
        try {
          ws.close(1000, "leave");
        } catch (e) {}
        break;
      }
    }
  }

  async webSocketClose(ws) {
    const meta = ws.deserializeAttachment() || {};
    if (meta.joined) {
      if (meta.role === "host") {
        this.broadcast({ t: "host_left" });
      } else {
        this.broadcast({ t: "chat", name: "СИСТЕМА", text: meta.name + " отключился" });
        this.broadcast(this.peersPayload());
      }
    }
  }

  async webSocketError(ws) {
    /* close придёт следом */
  }
}
