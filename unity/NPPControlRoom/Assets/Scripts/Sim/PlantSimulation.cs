using System;

namespace NPP.Sim
{
    /// <summary>Идентификаторы команд оператора. Используются и локально, и по сети.</summary>
    public enum ControlId
    {
        None = 0,
        RodTarget = 1,   // 0..1 — заданная глубина погружения стержней (1 = полностью введены)
        Pumps = 2,       // 0..3 — число работающих ГЦН
        Valve = 3,       // 0..1 — стопорно-регулирующий клапан турбины
        Feed = 4,        // 0..1.5 — питательные насосы (ручной режим)
        FeedAuto = 5,    // 0/1 — авторегулятор уровня в парогенераторе
        Breaker = 6,     // 0/1 — генераторный выключатель
        Vent = 7,        // 0/1 — сброс пара в атмосферу (БРУ-А)
        Scram = 8,       // 1 — аварийная защита (АЗ-5)
        Reset = 9        // 1 — сброс модели (после аварии)
    }

    /// <summary>Биты аварийной сигнализации, приходят в снапшоте одним int.</summary>
    [Flags]
    public enum AlarmBits
    {
        None = 0,
        PowerHigh = 1 << 0,
        PowerHighHigh = 1 << 1,
        FuelHot = 1 << 2,
        FuelHotHigh = 1 << 3,
        PressHigh = 1 << 4,
        PressHighHigh = 1 << 5,
        LevelLow = 1 << 6,
        LevelLowLow = 1 << 7,
        Overspeed = 1 << 8,
        FlowLow = 1 << 9,
        RadHigh = 1 << 10,
        Scram = 1 << 11,
        GenOffline = 1 << 12,
        TurbineTrip = 1 << 13,
        CoreDamage = 1 << 14
    }

    /// <summary>Плоский снимок состояния блока — то, что видит оператор и что уходит по сети.</summary>
    [Serializable]
    public struct PlantSnapshot
    {
        public float power;        // доля номинала, 1.0 = 100%
        public float rod;          // фактическое положение, 1 = введены
        public float rodTarget;
        public int pumps;        // число работающих ГЦН
        public int pumpsMask;    // независимые выключатели ГЦН (биты 0..2)
        public float valve;
        public float feed;
        public bool feedAuto;
        public bool breaker;
        public bool vent;
        public float fuelTemp;     // °C
        public float coolTemp;     // °C
        public float steamPress;   // МПа
        public float level;        // %
        public float rpm;
        public float genMW;
        public float xenon;        // 1.0 = равновесие
        public float rho;          // реактивность, доли
        public float rad;          // мкЗв/ч (учебные единицы)
        public int alarms;
        public bool scrammed;
        public bool turbineTripped;
        public bool coreDamage;
        public double t;
    }

    /// <summary>Метрики для приборов: единая точка доступа к значениям снапшота.</summary>
    public enum Metric
    {
        ReactorPowerPct,
        RodPosPct,
        RodTargetPct,
        FuelTemp,
        CoolTemp,
        SteamPressure,
        DrumLevel,
        Rpm,
        GenMW,
        XenonPct,
        RhoMilli,
        Radiation,
        FeedPct,
        ValvePct,
        PumpsCount
    }

    public static class Metrics
    {
        public static float Get(PlantSnapshot s, Metric m)
        {
            switch (m)
            {
                case Metric.ReactorPowerPct: return s.power * 100f;
                case Metric.RodPosPct: return s.rod * 100f;
                case Metric.RodTargetPct: return s.rodTarget * 100f;
                case Metric.FuelTemp: return s.fuelTemp;
                case Metric.CoolTemp: return s.coolTemp;
                case Metric.SteamPressure: return s.steamPress;
                case Metric.DrumLevel: return s.level;
                case Metric.Rpm: return s.rpm;
                case Metric.GenMW: return s.genMW;
                case Metric.XenonPct: return s.xenon * 100f;
                case Metric.RhoMilli: return s.rho * 1000f;
                case Metric.Radiation: return s.rad;
                case Metric.FeedPct: return s.feed * 100f;
                case Metric.ValvePct: return s.valve * 100f;
                case Metric.PumpsCount: return s.pumps;
                default: return 0f;
            }
        }
    }

    /// <summary>
    /// Упрощённая, но физически связная модель энергоблока ВВЭР-1000:
    /// точечная кинетика с запаздывающими нейтронами, температуры топлива/теплоносителя,
    /// парогенератор, турбина, ксеноновое отравление, защиты.
    /// Модель откалибрована скриптом tools/sim-prototype/physics.js (см. отчёты в docs/).
    /// НЕ является инструментом для реальных расчётов.
    /// </summary>
    public class PlantSimulation
    {
        // ---- константы ----
        public const double Beta = 0.0065;
        public const double Lambda = 2.0e-5;
        public const double Lam = 0.0784;

        public const double RodWorth = 0.035;
        public const double RodRef = 0.62;
        public const double RodSpeed = 0.04;
        public const double ScramSpeed = 0.40;

        public const double AFuel = 1.5e-5;
        public const double ACool = 2.5e-5;
        public const double AXe = 0.004;

        public const double DecayTau = 12.0;
        public const double DecayFrac = 0.07;

        public const double RatedMW = 3000.0;
        public const double HBase = 2.0;
        public const double HFlow = 7.677;
        public const double Cf = 60.0;

        public const double Kcs = 100.0;
        public const double Cc = 250.0;
        public const double TsgA = 170.0;
        public const double TsgB = 15.0;

        public const double Csg = 40.0;
        public const double KTurb = 550.0;
        public const double KFeed = 180.0;
        public const double Q0 = 2820.0;

        public const double KSt = 120.0;
        public const double CFr = 0.024;
        public const double J = 6.0;

        public const double XeT = 600.0;

        public const double PMax = 3.0;
        public const double CoreDamageP = 1.5;

        // ---- состояние ----
        public double t;
        public double P;         // относительная мощность
        public double C;         // предшественники запаздывающих нейтронов (норм.)
        public double Qd;        // остаточное тепловыделение (норм.)
        public double Tf;        // температура топлива, °C
        public double Tc;        // температура теплоносителя, °C
        public double Pst;       // давление пара, МПа
        public double Lvl;       // уровень в ПГ, %
        public double W;         // обороты турбины (номинал 3000)
        public double Xe;        // ксенон (1 = равновесие)
        public double I;         // йод
        public double pel;       // выработка, МВт
        public double rad;

        public double rod;
        public double rodTarget;
        public int pumps;        // число работающих ГЦН
        public int pumpsMask;    // независимые выключатели ГЦН (биты 0..2)
        public double valve;
        public double feed;
        public bool feedAuto;
        public bool breaker;
        public bool vent;

        public bool scrammed;
        public bool turbineTripped;
        public bool coreDamage;
        public string scramReason = "";
        public int alarms;

        public PlantSimulation() { Reset(); }

        public void Reset()
        {
            t = 0;
            P = 1.0;
            C = Beta / (Lambda * Lam);
            Qd = DecayFrac;
            Tf = 600; Tc = 290; Pst = 6.0; Lvl = 50; W = 3000; Xe = 1; I = 1; pel = 0;
            rodTarget = RodRef; rod = RodRef;
            pumpsMask = 7; pumps = 3; valve = 0.85; feed = 1.0; feedAuto = true;
            breaker = true; vent = false;
            scrammed = false; turbineTripped = false; coreDamage = false;
            scramReason = "";
            alarms = 0;
            rad = 0.1;
        }

        public void Apply(ControlId id, float v)
        {
            switch (id)
            {
                case ControlId.RodTarget: rodTarget = Clamp01(v); if (scrammed) scrammed = true; break;
                case ControlId.Pumps: pumpsMask = (int)Math.Round(Clamp(v, 0f, 7f)); break;
                case ControlId.Valve: valve = Clamp01(v); break;
                case ControlId.Feed: feed = Clamp(v, 0f, 1.5f); feedAuto = false; break;
                case ControlId.FeedAuto: feedAuto = v > 0.5f; break;
                case ControlId.Breaker: breaker = v > 0.5f; break;
                case ControlId.Vent: vent = v > 0.5f; break;
                case ControlId.Scram:
                    if (!scrammed) { Scram("АЗ-5 (оператор)"); }
                    break;
                case ControlId.Reset:
                    Reset();
                    break;
            }
        }

        public void Scram(string reason)
        {
            if (scrammed) return;
            scrammed = true;
            scramReason = reason;
        }

        public void Step(double dt)
        {
            if (dt <= 0) return;
            if (dt > 0.25) dt = 0.25;

            int pumpCount = CountPumps(pumpsMask);
            pumps = pumpCount;
            double pumpFrac = pumpCount / 3.0;

            // ---- авторегулятор питания ----
            if (feedAuto)
            {
                double demand = (KTurb * Pst * (turbineTripped ? 0.0 : valve)) / Q0;
                double trim = 0.014 * (50.0 - Lvl);
                feed = Clamp(demand * 1.06 + trim, 0.0, 1.6);
            }

            // ---- привод стержней ----
            if (scrammed)
            {
                rodTarget = 1.0;
                rod = Math.Min(1.0, rod + ScramSpeed * dt);
            }
            else
            {
                double d = rodTarget - rod;
                double mv = RodSpeed * dt;
                rod += Math.Abs(d) <= mv ? d : Math.Sign(d) * mv;
            }

            // ---- реактивность ----
            double rhoRod = RodWorth * (RodRef - rod);
            double rhoF = -AFuel * (Tf - 600.0);
            double rhoC = -ACool * (Tc - 290.0);
            double rhoXe = -AXe * (Xe - 1.0);
            double rho = rhoRod + rhoF + rhoC + rhoXe;

            // ---- кинетика (подшаги, защиты внутри) ----
            int n = 10;
            double h = dt / n;
            for (int i = 0; i < n; i++)
            {
                double dP = ((rho - Beta) / Lambda) * P + Lam * C;
                double dC = (Beta / Lambda) * P - Lam * C;
                P += dP * h;
                C += dC * h;
                if (P < 1e-6) P = 1e-6;
                if (P > PMax) P = PMax;
                if (P > 1.15 && !scrammed) Scram("МОЩНОСТЬ > 115%");
                if (P > CoreDamageP) coreDamage = true;
            }

            // ---- остаточное тепловыделение ----
            Qd += (DecayFrac * P - Qd) * (dt / DecayTau);

            // ---- теплофизика ----
            double Qh = RatedMW * (0.93 * P + Qd);
            double hfc = HBase + HFlow * pumpFrac;
            double Qfc = hfc * (Tf - Tc);
            Tf += (Qh - Qfc) / Cf * dt;

            double Tsg = TsgA + TsgB * Pst;
            double kcs = Kcs * Math.Max(0.06, pumpFrac);
            double Qcs = kcs * (Tc - Tsg);
            Tc += (Qfc - Qcs) / Cc * dt;

            // ---- парогенератор ----
            double valveEff = turbineTripped ? 0.0 : valve;
            double Qturb = KTurb * Pst * valveEff;
            double Qfeed = KFeed * feed * (Pst / 6.0);
            double Qrel = 0.0;
            if (vent) Qrel += 300.0 * Math.Max(0.0, Pst - 5.5);
            if (Pst > 7.8) Qrel += 800.0 * (Pst - 7.8);
            Pst += (Qcs - Qturb - Qfeed - Qrel) / Csg * dt;
            Pst = Clamp(Pst, 0.1, 9.5);
            Lvl += 1.2 * (feed - Qturb / Q0) * dt;
            Lvl = Clamp(Lvl, 0.0, 100.0);

            // ---- турбина / генератор ----
            if (breaker)
            {
                W += (3000.0 - W) * Math.Min(1.0, 2.0 * dt); // синхронизация с сетью
                pel = 0.33 * Qturb;
            }
            else
            {
                double Tst = KSt * Pst * valveEff;
                double Tfr = CFr * W;
                W = Math.Max(0.0, W + (Tst - Tfr) / J * dt);
                pel = 0.0;
            }
            if (W > 3150 && !turbineTripped) { turbineTripped = true; breaker = false; }

            // ---- ксенон (ускоренно) ----
            I += (P - I) * dt / XeT;
            Xe += (0.9 * I + 0.1 * P - Xe * (0.1 + 0.9 * P)) * dt / XeT;

            // ---- радиация (учебные единицы) ----
            rad = 0.1 + 20.0 * Math.Max(0, P - 1.2) + 10.0 * Math.Max(0, (Tf - 900) / 100.0)
                + (scrammed ? 2.0 : 0.0) + (coreDamage ? 500.0 : 0.0);

            // ---- сигнализация ----
            int a = 0;
            if (P > 1.05) a |= (int)AlarmBits.PowerHigh;
            if (P > 1.15) a |= (int)AlarmBits.PowerHighHigh;
            if (Tf > 800) a |= (int)AlarmBits.FuelHot;
            if (Tf > 1000) a |= (int)AlarmBits.FuelHotHigh;
            if (Pst > 7.0) a |= (int)AlarmBits.PressHigh;
            if (Pst > 8.5) a |= (int)AlarmBits.PressHighHigh;
            if (Lvl < 30) a |= (int)AlarmBits.LevelLow;
            if (Lvl < 15) a |= (int)AlarmBits.LevelLowLow;
            if (W > 3100) a |= (int)AlarmBits.Overspeed;
            if (pumpCount < 2) a |= (int)AlarmBits.FlowLow;
            if (rad > 1.0) a |= (int)AlarmBits.RadHigh;
            if (scrammed) a |= (int)AlarmBits.Scram;
            if (!breaker) a |= (int)AlarmBits.GenOffline;
            if (turbineTripped) a |= (int)AlarmBits.TurbineTrip;
            if (coreDamage) a |= (int)AlarmBits.CoreDamage;
            alarms = a;

            // ---- автоматические защиты ----
            if (Tf > 1000) Scram("ТЕМПЕРАТУРА ТОПЛИВА > 1000°C");
            if (Pst > 8.5) Scram("ДАВЛЕНИЕ ПАРА > 8.5 МПа");
            if (pumpCount < 2) Scram("РАСХОД ТЕПЛОНОСИТЕЛЯ НИЗКИЙ");
            if (Lvl < 15 && !turbineTripped) { turbineTripped = true; breaker = false; }
            if (scrammed) turbineTripped = true;

            t += dt;
        }

        public PlantSnapshot Snapshot()
        {
            PlantSnapshot s;
            s.power = (float)P;
            s.rod = (float)rod;
            s.rodTarget = (float)rodTarget;
            s.pumps = PumpsCount();
            s.pumpsMask = pumpsMask;
            s.valve = (float)valve;
            s.feed = (float)feed;
            s.feedAuto = feedAuto;
            s.breaker = breaker;
            s.vent = vent;
            s.fuelTemp = (float)Tf;
            s.coolTemp = (float)Tc;
            s.steamPress = (float)Pst;
            s.level = (float)Lvl;
            s.rpm = (float)W;
            s.genMW = (float)pel;
            s.xenon = (float)Xe;
            s.rho = (float)(RodWorth * (RodRef - rod) - AFuel * (Tf - 600.0) - ACool * (Tc - 290.0) - AXe * (Xe - 1.0));
            s.rad = (float)rad;
            s.alarms = alarms;
            s.scrammed = scrammed;
            s.turbineTripped = turbineTripped;
            s.coreDamage = coreDamage;
            s.t = t;
            return s;
        }

        public static int CountPumps(int mask)
        {
            int c = 0;
            if ((mask & 1) != 0) c++;
            if ((mask & 2) != 0) c++;
            if ((mask & 4) != 0) c++;
            return c;
        }

        public int PumpsCount() { return CountPumps(pumpsMask); }

        static double Clamp(double v, double a, double b) => v < a ? a : (v > b ? b : v);
        static float Clamp(float v, float a, float b) => v < a ? a : (v > b ? b : v);
        static double Clamp01(double v) => Clamp(v, 0.0, 1.0);
    }
}
