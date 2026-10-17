// NPP simulator - plant physics prototype (calibration harness, not shipped)
// Mirrors Assets/Scripts/Sim/PlantSimulation.cs 1:1.

const P = {
  beta: 0.0065, lambda: 2.0e-5, lam: 0.0784,
  rodWorth: 0.035, rodRef: 0.62, rodSpeed: 0.04, scramSpeed: 0.40,
  aFuel: 1.5e-5, aCool: 2.5e-5, aXe: 0.004,
  decayTau: 12, decayFrac: 0.07,
  ratedMW: 3000,
  hBase: 2.0, hFlow: 7.677, Cf: 60,
  kcs: 100, Cc: 250, TsgA: 170, TsgB: 15,
  Csg: 40, kTurb: 550, kFeed: 180, Q0: 2820,
  kSt: 120, cFr: 0.024, J: 6,
  XeT: 600,
  pMax: 3.0, coreDamageP: 1.5,
};

const fresh = () => ({
  t: 0, P: 1, C: P.beta / (P.lambda * P.lam), Qd: P.decayFrac,
  Tf: 600, Tc: 290, Pst: 6.0, Lvl: 50, W: 3000, Xe: 1, I: 1,
  rodTarget: P.rodRef, rod: P.rodRef,
  pumps: 3, valve: 0.85, feed: 1.0, feedAuto: true, breaker: true, vent: false,
  Pel: 0, rho: 0, rad: 0.1,
  scrammed: false, turbineTripped: false, coreDamage: false,
  alarms: {},
});

function step(s, dt) {
  const pumpFrac = s.pumps / 3;
  // feedwater auto regulator (PI on SG level, demand feed-forward)
  if (s.feedAuto) {
    const demand = (P.kTurb * s.Pst * (s.turbineTripped ? 0 : s.valve)) / P.Q0;
    const trim = 0.014 * (50 - s.Lvl);
    s.feed = Math.max(0, Math.min(1.6, demand * 1.06 + trim));
  }

  // --- rod drive (rate limited, SCRAM overrides) ---
  if (s.scrammed) {
    s.rodTarget = 1;
    s.rod = Math.min(1, s.rod + P.scramSpeed * dt);
  } else {
    const d = s.rodTarget - s.rod;
    const mv = P.rodSpeed * dt;
    s.rod += Math.abs(d) <= mv ? d : Math.sign(d) * mv;
  }

  // --- reactivity ---
  const rhoRod = P.rodWorth * (P.rodRef - s.rod);
  const rhoF = -P.aFuel * (s.Tf - 600);
  const rhoC = -P.aCool * (s.Tc - 290);
  const rhoXe = -P.aXe * (s.Xe - 1.0);
  const rho = rhoRod + rhoF + rhoC + rhoXe;
  s.rho = rho;

  // --- point kinetics, substepped, with protection inside the loop ---
  const n = 10, h = dt / n;
  for (let i = 0; i < n; i++) {
    const dP = ((rho - P.beta) / P.lambda) * s.P + P.lam * s.C;
    const dC = (P.beta / P.lambda) * s.P - P.lam * s.C;
    s.P += dP * h;
    s.C += dC * h;
    if (s.P < 1e-6) s.P = 1e-6;
    if (s.P > P.pMax) s.P = P.pMax;          // numerical ceiling (300%)
    const instRho = rhoRod + s.rho - rhoRod;  // placeholder to keep structure simple
    if (s.P > 1.15 && !s.scrammed) { s.scrammed = true; s.scramReason = 'POWER HIGH HIGH'; }
    if (s.P > P.coreDamageP) s.coreDamage = true;
  }

  // --- decay heat ---
  s.Qd += (P.decayFrac * s.P - s.Qd) * (dt / P.decayTau);

  // --- thermal ---
  const Qh = P.ratedMW * (0.93 * s.P + s.Qd);
  const hfc = P.hBase + P.hFlow * pumpFrac;
  const Qfc = hfc * (s.Tf - s.Tc);
  s.Tf += (Qh - Qfc) / P.Cf * dt;

  const Tsg = P.TsgA + P.TsgB * s.Pst;
  const kcs = P.kcs * Math.max(0.06, pumpFrac);
  const Qcs = kcs * (s.Tc - Tsg);
  s.Tc += (Qfc - Qcs) / P.Cc * dt;

  // --- steam generator ---
  let valveEff = s.valve;
  if (s.turbineTripped) valveEff = 0;
  const Qturb = P.kTurb * s.Pst * valveEff;
  const Qfeed = P.kFeed * s.feed * (s.Pst / 6);
  let Qrel = 0;
  if (s.vent) Qrel += 300 * Math.max(0, s.Pst - 5.5);
  if (s.Pst > 7.8) Qrel += 800 * (s.Pst - 7.8);
  s.Pst += (Qcs - Qturb - Qfeed - Qrel) / P.Csg * dt;
  s.Pst = Math.max(0.1, Math.min(9.5, s.Pst));
  s.Lvl += 1.2 * (s.feed - Qturb / P.Q0) * dt;
  s.Lvl = Math.max(0, Math.min(100, s.Lvl));

  // --- turbine / generator ---
  if (s.breaker) {
    s.W += (3000 - s.W) * Math.min(1, 2 * dt);
    s.Pel = 0.33 * Qturb;
  } else {
    const Tst = P.kSt * s.Pst * valveEff;
    const Tfr = P.cFr * s.W;
    s.W = Math.max(0, s.W + (Tst - Tfr) / P.J * dt);
    s.Pel = 0;
  }
  if (s.W > 3150 && !s.turbineTripped) { s.turbineTripped = true; s.breaker = false; }

  // --- xenon (accelerated) ---
  s.I += (s.P - s.I) * dt / P.XeT;
  s.Xe += (0.9 * s.I + 0.1 * s.P - s.Xe * (0.1 + 0.9 * s.P)) * dt / P.XeT;

  // --- displays ---
  s.rad = 0.1 + 20 * Math.max(0, s.P - 1.2) + 10 * Math.max(0, (s.Tf - 900) / 100)
        + (s.scrammed ? 2 : 0) + (s.coreDamage ? 500 : 0);

  // --- alarms ---
  s.alarms = {
    powerHigh: s.P > 1.05, powerHighHigh: s.P > 1.15,
    fuelHot: s.Tf > 800, fuelHotHigh: s.Tf > 1000,
    pressHigh: s.Pst > 7.0, pressHighHigh: s.Pst > 8.5,
    levelLow: s.Lvl < 30, levelLowLow: s.Lvl < 15,
    overspeed: s.W > 3100, flowLow: s.pumps < 2,
    radHigh: s.rad > 1.0, scram: s.scrammed, genOffline: !s.breaker,
    turbineTrip: s.turbineTripped, coreDamage: s.coreDamage,
  };
  s.t += dt;

  // --- protections ---
  if (s.alarms.fuelHotHigh || s.alarms.pressHighHigh) s.scrammed = true;
  if (s.pumps < 2) s.scrammed = true;                 // reactor coolant flow low
  if (s.Lvl < 15 && !s.turbineTripped) { s.turbineTripped = true; s.breaker = false; } // SG low-low level
  if (s.scrammed) s.turbineTripped = true;
  return s;
}

function simulate(name, seconds, mutate, sampleEvery = 1) {
  const s = fresh();
  const dt = 0.02;
  let nextAuto = 0;
  console.log(`\n=== ${name} ===`);
  for (let i = 0; i < Math.round(seconds / dt); i++) {
    if (mutate) mutate(s, i * dt);
    step(s, dt);
    if (i * dt >= nextAuto) {
      nextAuto += sampleEvery;
      console.log(`t=${(i * dt).toFixed(0).padStart(4)}s P=${(s.P * 100).toFixed(1).padStart(6)}% Tf=${s.Tf.toFixed(0).padStart(4)} Tc=${s.Tc.toFixed(0).padStart(4)} Pst=${s.Pst.toFixed(2)} Lvl=${s.Lvl.toFixed(0).padStart(3)} W=${s.W.toFixed(0).padStart(4)} Pel=${s.Pel.toFixed(0).padStart(4)} Xe=${s.Xe.toFixed(2)} rho=${(s.rho * 100).toFixed(3)}%${s.scrammed ? ' SCRAM' : ''}${s.coreDamage ? ' CORE-DAMAGE' : ''}`);
    }
  }
  return s;
}

module.exports = { P, fresh, step, simulate };

if (require.main === module) {
  simulate('1. Steady state 10 min (no input)', 600, null, 60);
  simulate('2. Rods to 20% at t=5s', 90, (s, t) => { if (t > 5) s.rodTarget = 0.2; }, 5);
  simulate('3. SCRAM from full power at t=5s', 300, (s, t) => { if (t > 5) s.scrammed = true; }, 30);
  simulate('4. Valve full open at t=5s', 60, (s, t) => { if (t > 5) s.valve = 1.0; }, 5);
  simulate('5. Feedwater off (manual) at t=10s', 180, (s, t) => { if (t > 5) s.feedAuto = false; if (t > 10) s.feed = 0; }, 15);
  simulate('6. All pumps off at t=10s', 180, (s, t) => { if (t > 10) s.pumps = 0; }, 15);
}
