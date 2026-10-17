/**
 * Проверка релея: два клиента, комната, роли, команда и снапшот.
 * node smoke-test.js wss://npp-relay.<subdomain>.workers.dev/ws
 */
const WebSocket = require("ws");
const base = process.argv[2] || "ws://127.0.0.1:8787/ws";
const room = "T" + Math.random().toString(36).slice(2, 5).toUpperCase();
const url = base + (base.includes("?") ? "&" : "?") + "room=" + room;

const state = { roles: {}, gotCmd: false, gotState: false, cmdId: null, cmdValue: null };

const finish = (code, msg) => {
  console.log(msg, { room, roles: state.roles, gotCmd: state.gotCmd, gotState: state.gotState });
  process.exit(code);
};

const timeout = setTimeout(() => finish(1, "RELAY TIMEOUT"), 9000);

function client(name) {
  const ws = new WebSocket(url);
  ws.name = name;
  ws.on("open", () => ws.send(JSON.stringify({ t: "join", name })));
  ws.on("message", (raw) => {
    let d;
    try { d = JSON.parse(raw.toString()); } catch (e) { return; }
    if (d.t === "joined") {
      state.roles[name] = d.role;
      kick();
    }
    if (d.t === "cmd") {
      state.gotCmd = true;
      state.cmdId = d.id;
      state.cmdValue = d.value;
      kick();
    }
    if (d.t === "state") {
      try {
        const st = JSON.parse(d.state);
        state.gotState = typeof st.p === "number" && Math.abs(st.p - 0.97) < 1e-9;
      } catch (e) { state.gotState = false; }
      kick();
    }
    if (d.t === "error") console.log("server error:", d.message);
  });
  ws.on("error", (e) => console.log("ws error", name, e.message));
  return ws;
}

let kicked = false;

function kick() {
  const names = Object.keys(state.roles);
  if (names.length < 2) return;
  const hostName = names.find((n) => state.roles[n] === "host");
  const guestName = names.find((n) => state.roles[n] === "guest");
  if (!hostName || !guestName) return;

  if (!kicked) {
    kicked = true;
    setTimeout(() => {
      // гость шлёт команду, хост — снапшот
      guests[guestName].send(JSON.stringify({ t: "cmd", id: 3, value: 0.42 }));
      hosts[hostName].send(JSON.stringify({ t: "state", state: JSON.stringify({ p: 0.97, rod: 0.6 }) }));
    }, 400);
  }

  if (state.gotCmd && state.gotState) {
    clearTimeout(timeout);
    const ok = state.cmdId === 3 && Math.abs(state.cmdValue - 0.42) < 1e-6;
    finish(ok ? 0 : 1, ok ? "RELAY OK" : "RELAY FAIL (bad cmd payload)");
  }
}

const hosts = {};
const guests = {};
const a = client("alpha");
const b = client("beta");
hosts.alpha = a; hosts.beta = b; guests.alpha = a; guests.beta = b;
