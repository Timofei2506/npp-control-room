const fs = require('fs');
let src = fs.readFileSync('physics.js','utf8');
// подрежем прогоны, оставим только сценарий 3
src = src.split('run("1.')[0];
eval(src);
mem = { scram: false, turbineTripped: false, breakerLatched: true };
ctl = { rod: P.rodRef, pumps: 3, valve: 0.85, feed: 1.0, breaker: true, vent: false };
const s = newState(); const dt = 0.02;
for (let i = 0; i < 1000; i++) {
  const t = i*dt;
  if (t > 5) mem.scram = true;
  step(s, dt);
  if (i % 25 === 0 || !isFinite(s.P)) {
    console.log(t.toFixed(2), 'P', s.P.toExponential(2), 'C', s.C.toExponential(2), 'Qd', s.Qd.toFixed(3), 'Tf', s.Tf.toFixed(1), 'Tc', s.Tc.toFixed(1), 'Pst', s.Pst.toFixed(3), 'Lvl', s.Lvl.toFixed(1), 'W', s.W.toFixed(0), 'Xe', s.Xe.toFixed(3), 'rho', s.rho.toExponential(2));
    if (!isFinite(s.P)) break;
  }
}
