// v2: устойчивый ПГ-контур, калибровка под номинал 3000 МВт
const P = {
  B: 0.0065, LAMBDA: 2.0e-5, LAM: 0.0784,
  RODW: 0.035, RODREF: 0.62, RODSPD: 0.04, SCRAMSPD: 0.40,
  AF: 1.5e-5, AC: 2.5e-5, AXE: 0.004, XET: 600,
  RATED: 3000, HB: 0.2, HFLOW: 9.477, CF: 60, CC: 250,
  KCS: 100, TSGA: 170, TSGB: 15, CSG: 400,
  KTURB: 555.56, KFEED: 1.0, Q0: 2820, KST: 120, CFR: 0.024, J: 60,
  PMAX: 3.0, CORE_DMG_P: 1.5,
};

function fresh() {
  return {
    t:0, P:1, C:P.B/(P.LAMBDA*P.LAM), Qd:0.07,
    Tf:600, Tc:290, Pst:6.0, Lvl:50, W:3000, Xe:1, I:1, Pel:0,
    rod:P.RODREF, rodTarget:P.RODREF, pumps:3, pumpsMask:7,
    valve:3000/(P.KTURB*6.0), feed:1, feedAuto:true, breaker:true, vent:false,
    scrammed:false, turbineTripped:false, coreDamage:false, rad:0.1,
    alarms:0, rho:0, scramReason:''
  };
}

const clamp=(v,a,b)=>v<a?a:(v>b?b:v); const c01=v=>clamp(v,0,1);

function scram(s, why){ if(s.scrammed) return; s.scrammed=true; s.scramReason=why; s.turbineTripped=true; s.breaker=false; }

function step(s, dt){
  if(dt<=0) return; if(dt>0.25) dt=0.25;
  s.t += dt;
  const pumpFrac = s.pumps/3;

  if(s.feedAuto){
    const steamFlow = (P.KTURB*s.Pst*(s.turbineTripped?0:s.valve))/P.Q0;
    s.feed = clamp(steamFlow + 0.02*(50-s.Lvl), 0, 1.5);
  }

  if(s.scrammed){ s.rodTarget=1; s.rod=Math.min(1,s.rod+P.SCRAMSPD*dt); }
  else {
    const d=s.rodTarget-s.rod, mv=P.RODSPD*dt;
    s.rod += Math.abs(d)<=mv?d:Math.sign(d)*mv;
  }

  const rhoRod=P.RODW*(P.RODREF-s.rod);
  const rhoF=-P.AF*(s.Tf-600);
  const rhoC=-P.AC*(s.Tc-290);
  const rhoXe=-P.AXE*(s.Xe-1);
  const rho=rhoRod+rhoF+rhoC+rhoXe; s.rho=rho;

  // кинетика — экспоненциальный шаг по P, C (semi-implicit, устойчиво)
  const n=10, h=dt/n;
  for(let i=0;i<n;i++){
    // неявный шаг по мощности: устойчив при любой реактивности
    const a=(P.B-rho)/P.LAMBDA;
    s.P=(s.P + P.LAM*s.C*h)/(1+a*h);
    s.C+=((P.B/P.LAMBDA)*s.P - P.LAM*s.C)*h;
    if(s.P<1e-6) s.P=1e-6;
    if(s.P>P.PMAX) s.P=P.PMAX;
    if(s.P>1.15 && !s.scrammed) scram(s,'МОЩНОСТЬ > 115%');
    if(s.P>P.CORE_DMG_P) s.coreDamage=true;
  }

  s.Qd += (0.07*s.P - s.Qd)*(dt/1200);
  const Qh=P.RATED*(0.93*s.P+s.Qd);
  const hfc=P.HB+P.HFLOW*pumpFrac;
  const Qfc=hfc*(s.Tf-s.Tc);
  s.Tf += (Qh-Qfc)/P.CF*dt;

  const Tsg=P.TSGA+P.TSGB*s.Pst;
  const kcs=P.KCS*Math.max(0.06,pumpFrac);
  const Qcs=kcs*(s.Tc-Tsg);

  // теплоноситель: инерция + полунеявный шаг
  s.Tc += (Qfc-Qcs)/P.CC*dt;
  if(s.Tc<20) s.Tc=20;

  // ПГ: давление = баланс подведённого и отведённого тепла (без питательной воды)
  const valveEff=s.turbineTripped?0:s.valve;
  const Qturb=P.KTURB*s.Pst*valveEff;
  let Qrel=0;
  if(s.vent) Qrel+=300*Math.max(0,s.Pst-5.5);
  if(s.Pst>7.8) Qrel+=800*(s.Pst-7.8);
  s.Pst += (Qcs-Qturb-Qrel)/P.CSG*dt;
  s.Pst = clamp(s.Pst,0.1,9.5);

  // уровень: массовый баланс
  s.Lvl += 1.2*(s.feed-Qturb/P.Q0)*dt;
  s.Lvl = clamp(s.Lvl,0,100);

  if(s.breaker){ s.W += (3000-s.W)*Math.min(1,2*dt); s.Pel=0.33*Qturb; }
  else {
    const Tst=P.KST*s.Pst*valveEff, Tfr=P.CFR*s.W;
    s.W=Math.max(0,s.W+(Tst-Tfr)/P.J*dt); s.Pel=0;
  }
  if(s.W>3150 && !s.turbineTripped){ s.turbineTripped=true; s.breaker=false; }

  s.I += (s.P-s.I)*dt/P.XET;
  s.Xe += (0.9*s.I+0.1*s.P - s.Xe*(0.1+0.9*s.P))*dt/P.XET;

  // защиты
  if(s.Tf>900){ if(!s.scrammed) scram(s,'ТОПЛИВО > 900 °C'); s.coreDamage = s.coreDamage || s.Tf>1000; }
  if(s.Pst>8.5 && !s.scrammed) scram(s,'ДАВЛЕНИЕ > 8.5 МПа');
  if(s.Lvl<15 && !s.turbineTripped){ s.turbineTripped=true; s.breaker=false; }
  if(s.Pst<2.5 && !s.turbineTripped){ s.turbineTripped=true; s.breaker=false; }
  if(s.pumps<2 && !s.scrammed) scram(s,'ГЦН < 2');

  let al=0;
  const set=(bit,cond)=>{ if(cond) al|=bit; };
  set(1<<0,s.P>1.05); set(1<<1,s.P>1.15);
  set(1<<2,s.Tf>800); set(1<<3,s.Tf>1000);
  set(1<<4,s.Pst>7.0); set(1<<5,s.Pst>8.5);
  set(1<<6,s.Lvl<30); set(1<<7,s.Lvl<15);
  set(1<<8,s.W>3100); set(1<<9,s.pumps<2);
  set(1<<10,s.rad>5); set(1<<11,s.scrammed);
  set(1<<12,!s.breaker); set(1<<13,s.turbineTripped); set(1<<14,s.coreDamage);
  s.alarms=al;
  if(s.coreDamage) s.rad += 0.5*dt;
  else s.rad = Math.max(0.1, s.rad-0.05*dt);
}

// ---------------------------------------------------------------------------
// CLI: node physics.js [сценарий]
// ---------------------------------------------------------------------------
function simulate(name, fn, dur, dt){
  dt=dt||0.05; const s=fresh(); let trip=-1;
  for(let t=0;t<dur;t+=dt){ fn(s,t); step(s,dt); if(s.scrammed&&trip<0) trip=s.t; }
  console.log(name.padEnd(12),
    'P='+(s.P*100).toFixed(1)+'%',
    'Tf='+s.Tf.toFixed(0), 'Tc='+s.Tc.toFixed(0),
    'Pst='+s.Pst.toFixed(2), 'Lvl='+s.Lvl.toFixed(0),
    'W='+s.W.toFixed(0), 'Pel='+s.Pel.toFixed(0),
    'Xe='+s.Xe.toFixed(2),
    (s.scrammed?'SCRAM@'+trip.toFixed(0)+' "'+s.scramReason+'"':''),
    (s.coreDamage?'CORE-DAMAGE':''));
  return s;
}

if(require.main===module){
  console.log('=== NPP PlantSimulation — эталонные сценарии (dt=0.05 c) ===');
  simulate('номинал', ()=>{}, 600);
  simulate('стержни 20%', (s,t)=>{ if(t>=30&&t<30.05) s.rodTarget=0.20; }, 300);
  simulate('SCRAM', (s,t)=>{ if(t>=30&&t<30.05&&!s.scrammed){ s.scrammed=true; s.turbineTripped=true; s.breaker=false; } }, 300);
  simulate('клапан 100%', (s,t)=>{ if(t>=30&&t<30.05) s.valve=1.0; }, 300);
  simulate('ГЦН 0', (s,t)=>{ if(t>=30&&t<30.05) s.pumps=0; }, 300);
  simulate('питание 0', (s,t)=>{ if(t>=30&&t<30.05){ s.feedAuto=false; s.feed=0; } }, 300);
}

module.exports={P,fresh,step,simulate};
