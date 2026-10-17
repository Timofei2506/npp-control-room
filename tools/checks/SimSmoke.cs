// Эталонные прогоны PlantSimulation: номинал, потеря ГЦН, два насоса.
// Запуск: mono sim.exe
using System; using NPP.Sim;
class T {
  static T() { try { Console.OutputEncoding = System.Text.Encoding.UTF8; } catch { } }
  static void Main(){
    // номинал 10 минут + сценарий потери ГЦН
    var sim=new PlantSimulation();
    for(int i=0;i<12000;i++) sim.Step(0.05);
    var s=sim.Snapshot();
    Console.WriteLine($"номинал 600с: P={s.power:F3} Tf={s.fuelTemp:F0} Tc={s.coolTemp:F0} Pst={s.steamPress:F2} Lvl={s.level:F1} Pel={s.genMW:F0} scram={s.scrammed}");

    sim=new PlantSimulation();
    for(int i=0;i<200;i++) sim.Step(0.05);
    sim.Apply(ControlId.Pumps, 0);
    double maxTf=0; double t1000=-1;
    for(int i=0;i<18000;i++){ sim.Step(0.05); var x=sim.Snapshot(); maxTf=Math.Max(maxTf,x.fuelTemp); if(x.fuelTemp>1000&&t1000<0)t1000=x.t; }
    s=sim.Snapshot();
    Console.WriteLine($"0 ГЦН 900с: TfMax={maxTf:F0} Tf>1000@{(t1000<0?"-":t1000.ToString("F0"))} damage={s.coreDamage} rad={s.rad:F0} alarm=0x{s.alarms:X}");

    sim=new PlantSimulation();
    for(int i=0;i<600;i++) sim.Step(0.05);
    sim.Apply(ControlId.Pumps, 5); // два насоса (биты 0 и 2)
    maxTf=0;
    for(int i=0;i<6000;i++){ sim.Step(0.05); maxTf=Math.Max(maxTf,sim.Snapshot().fuelTemp); }
    s=sim.Snapshot();
    Console.WriteLine($"2 ГЦН 300с: TfMax={maxTf:F0} Tf={s.fuelTemp:F0} Pel={s.genMW:F0} scram={s.scrammed}");
  }
}
