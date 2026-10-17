using NPP.Sim;
using UnityEngine;
using UnityEngine.Rendering;

namespace NPP.Room
{
    /// <summary>Процедурная сборка блочного щита управления.</summary>
    public static class ControlRoomBuilder
    {
        public static Transform Build()
        {
            MaterialLibrary.EnsureInit();

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.27f, 0.29f, 0.32f);
            RenderSettings.fog = false;
            RenderSettings.reflectionIntensity = 0.55f;

            var root = new GameObject("ControlRoom").transform;

            BuildShell(root);
            BuildLighting(root);
            BuildReactorConsole(root);
            BuildTurbineConsole(root);
            BuildAuxPanel(root);
            BuildAnnunciator(root);
            BuildMimic(root);
            BuildProps(root);
            return root;
        }

        // =================================================================
        // Оболочка помещения
        // =================================================================
        static void BuildShell(Transform root)
        {
            var wallLower = MaterialLibrary.Paint("wallLower", new Color(0.23f, 0.30f, 0.26f), 0.3f);
            var stripe = MaterialLibrary.Paint("stripe", new Color(0.5f, 0.42f, 0.1f), 0.35f);

            Build.Box("Floor", root, new Vector3(0f, -0.1f, 0f), new Vector3(13f, 0.2f, 11f), MaterialLibrary.Floor);
            Build.Box("Ceiling", root, new Vector3(0f, 3.62f, 0f), new Vector3(13f, 0.25f, 11f), MaterialLibrary.Ceiling);

            // стены
            Build.Box("WallBack", root, new Vector3(0f, 1.75f, 5.65f), new Vector3(13f, 3.5f, 0.3f), MaterialLibrary.Wall);
            Build.Box("WallFront", root, new Vector3(0f, 1.75f, -5.65f), new Vector3(13f, 3.5f, 0.3f), MaterialLibrary.Wall);
            Build.Box("WallLeft", root, new Vector3(-6.65f, 1.75f, 0f), new Vector3(0.3f, 3.5f, 11f), MaterialLibrary.Wall);
            Build.Box("WallRight", root, new Vector3(6.65f, 1.75f, 0f), new Vector3(0.3f, 3.5f, 11f), MaterialLibrary.Wall);

            // нижняя панель стен (тёмная), поясок
            float[][] walls =
            {
                new float[] { 0f, 5.49f, 12.9f, 0f },    // back
                new float[] { 0f, -5.49f, 12.9f, 0f },   // front
                new float[] { -6.49f, 0f, 10.9f, 1f },   // left
                new float[] { 6.49f, 0f, 10.9f, 1f }     // right
            };
            for (int i = 0; i < walls.Length; i++)
            {
                var w = walls[i];
                bool side = w[3] > 0.5f;
                Vector3 sizeLower = side ? new Vector3(0.03f, 1.15f, w[2]) : new Vector3(w[2], 1.15f, 0.03f);
                Vector3 posLower = side ? new Vector3(w[0], 0.6f, w[1]) : new Vector3(w[0], 0.6f, w[1] + 0.001f);
                Build.Box("WallLower", root, posLower, sizeLower, wallLower, false);

                Vector3 sizeStripe = side ? new Vector3(0.035f, 0.06f, w[2]) : new Vector3(w[2], 0.06f, 0.035f);
                Vector3 posStripe = side ? new Vector3(w[0], 1.2f, w[1]) : new Vector3(w[0], 1.2f, w[1]);
                Build.Box("WallStripe", root, posStripe, sizeStripe, stripe, false);

                // плинтус
                Vector3 sizeBase = side ? new Vector3(0.06f, 0.09f, w[2]) : new Vector3(w[2], 0.09f, 0.06f);
                Vector3 posBase = side ? new Vector3(w[0], 0.045f, w[1]) : new Vector3(w[0], 0.045f, w[1]);
                Build.Box("Baseboard", root, posBase, sizeBase, MaterialLibrary.ConsoleTrim, false);
            }

            // потолочные балки
            for (int i = 0; i < 3; i++)
            {
                float z = -3.6f + i * 3.6f;
                Build.Box("Beam", root, new Vector3(0f, 3.44f, z), new Vector3(13f, 0.22f, 0.22f),
                          MaterialLibrary.Concrete, false);
            }

            // вентиляционные решётки на правой стене
            for (int i = 0; i < 2; i++)
            {
                float z = 1.4f - i * 2.2f;
                var grp = Build.Group("Vent", root, new Vector3(6.46f, 2.55f, z), new Vector3(0f, -90f, 0f));
                Build.Box("Frame", grp.transform, Vector3.zero, new Vector3(1.1f, 0.55f, 0.04f),
                          MaterialLibrary.ConsoleTrim, false);
                for (int k = 0; k < 6; k++)
                    Build.Box("Slat", grp.transform, new Vector3(0f, -0.22f + k * 0.088f, 0.026f),
                              new Vector3(1.0f, 0.03f, 0.01f), MaterialLibrary.Chrome, false);
            }
        }

        // =================================================================
        // Освещение
        // =================================================================
        static void BuildLighting(Transform root)
        {
            var sunGo = new GameObject("FillLight");
            sunGo.transform.SetParent(root, false);
            sunGo.transform.localEulerAngles = new Vector3(58f, 35f, 0f);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(0.42f, 0.45f, 0.5f);
            sun.intensity = 0.5f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.75f;

            float[] xs = { -3.7f, 0f, 3.7f };
            float[] zs = { -2.6f, 1.6f };
            for (int i = 0; i < xs.Length; i++)
                for (int j = 0; j < zs.Length; j++)
                {
                    var fx = Build.Group("CeilFixture", root, new Vector3(xs[i], 3.46f, zs[j]), Vector3.zero);
                    var panel = Build.Box("Panel", fx.transform, Vector3.zero, new Vector3(1.5f, 0.07f, 0.45f),
                                          MaterialLibrary.Lamp(new Color(1f, 0.97f, 0.9f), 1.0f), false);
                    Build.Box("Housing", fx.transform, new Vector3(0f, 0.05f, 0f), new Vector3(1.62f, 0.06f, 0.56f),
                              MaterialLibrary.ConsoleTrim, false);

                    var lightGo = new GameObject("Light");
                    lightGo.transform.SetParent(fx.transform, false);
                    lightGo.transform.localPosition = new Vector3(0f, -0.28f, 0f);
                    var l = lightGo.AddComponent<Light>();
                    l.type = LightType.Point;
                    l.color = new Color(1f, 0.96f, 0.88f);
                    l.intensity = 0.95f;
                    l.range = 9f;
                    l.shadows = (j == 1 && i != 1) ? LightShadows.Soft : LightShadows.None;
                    l.renderMode = LightRenderMode.ForcePixel;
                    l.shadowStrength = 0.6f;
                }

            // аварийное освещение у стен
            var emergency = new GameObject("EmergencyRed");
            emergency.transform.SetParent(root, false);
            emergency.transform.localPosition = new Vector3(0f, 3.1f, -5.2f);
            var er = emergency.AddComponent<Light>();
            er.type = LightType.Point;
            er.color = new Color(1f, 0.15f, 0.1f);
            er.intensity = 0f;
            er.range = 10f;
            er.shadows = LightShadows.None;

            var ctrl = emergency.AddComponent<EmergencyLight>();
            ctrl.target = er;
        }

        // =================================================================
        // Пульт реактора
        // =================================================================
        static void BuildReactorConsole(Transform root)
        {
            var grp = Build.Group("ReactorConsole", root, new Vector3(-3.15f, 0f, -1.05f), new Vector3(0f, 18f, 0f));
            Transform face, deck;
            PanelFactory.MakeConsole(grp.transform, Vector3.zero, 0f, 2.9f, out face, out deck);
            AddConsoleLamp(grp.transform);

            PanelFactory.AddGauge(face, new Vector3(-1.05f, 0.20f, 0.001f), 0.30f,
                "МОЩНОСТЬ, %", Metric.ReactorPowerPct, 0f, 150f, 0.733f);
            PanelFactory.AddGauge(face, new Vector3(-0.35f, 0.20f, 0.001f), 0.30f,
                "ТОПЛИВО, °C", Metric.FuelTemp, 0f, 1200f, 0.75f);
            PanelFactory.AddGauge(face, new Vector3(0.35f, 0.20f, 0.001f), 0.30f,
                "ТЕПЛОНОСИТЕЛЬ, °C", Metric.CoolTemp, 0f, 400f, 0.85f);
            PanelFactory.AddGauge(face, new Vector3(1.05f, 0.20f, 0.001f), 0.30f,
                "РЕАКТИВНОСТЬ", Metric.RhoMilli, -3f, 3f);

            PanelFactory.AddReadout(face, new Vector3(-1.05f, -0.14f, 0.001f), new Vector2(0.55f, 0.15f),
                "POWER", Metric.ReactorPowerPct, 1, "%");
            PanelFactory.AddReadout(face, new Vector3(-0.35f, -0.14f, 0.001f), new Vector2(0.55f, 0.15f),
                "ROD", Metric.RodPosPct, 1, "%");
            PanelFactory.AddReadout(face, new Vector3(0.35f, -0.14f, 0.001f), new Vector2(0.55f, 0.15f),
                "XENON", Metric.XenonPct, 1, "%");
            PanelFactory.AddReadout(face, new Vector3(1.05f, -0.14f, 0.001f), new Vector2(0.55f, 0.15f),
                "FLOW", Metric.PumpsCount, 0, "ГЦН", s => (s.pumps / 3f * 100f).ToString("F0"));

            AddLampRow(face, new Vector3(0f, 0.40f, 0.001f), new[]
            {
                new LampDef("СЕТЬ", LampMode.ControlOn, ControlId.Breaker, Metric.ReactorPowerPct, 0f, new Color(0.2f, 1f, 0.4f), false),
                new LampDef("ГЦН", LampMode.MetricBelow, ControlId.Pumps, Metric.PumpsCount, 1.5f, new Color(1f, 0.75f, 0.15f), false),
                new LampDef("АЗ", LampMode.ControlOn, ControlId.Scram, Metric.ReactorPowerPct, 0f, new Color(1f, 0.12f, 0.08f), false),
                new LampDef("ТОПЛИВО", LampMode.MetricAbove, ControlId.Breaker, Metric.FuelTemp, 800f, new Color(1f, 0.35f, 0.1f), false)
            });

            // наклонный пульт
            PanelFactory.AddKnob(deck, new Vector3(-1.15f, 0.0f, 0.014f), 0.05f, ControlId.RodTarget,
                0f, 1f, 0.02f, "СТЕРЖНИ");
            PanelFactory.AddReadout(deck, new Vector3(-1.15f, 0.145f, 0.014f), new Vector2(0.42f, 0.1f),
                "ЗАДАНИЕ РОД", Metric.RodTargetPct, 0, "%");

            PanelFactory.AddPumpToggle(deck, new Vector3(-0.52f, 0.01f, 0.014f), 0, "ГЦН-1");
            PanelFactory.AddPumpToggle(deck, new Vector3(-0.30f, 0.01f, 0.014f), 1, "ГЦН-2");
            PanelFactory.AddPumpToggle(deck, new Vector3(-0.08f, 0.01f, 0.014f), 2, "ГЦН-3");

            PanelFactory.AddToggle(deck, new Vector3(0.26f, 0.01f, 0.014f), ControlId.Vent, "БРУ-А");

            var scram = Build.Group("AZ5Mount", deck, new Vector3(0.92f, 0.0f, 0.014f), Vector3.zero);
            scram.transform.localScale = new Vector3(0.82f, 0.82f, 0.82f);
            PanelFactory.AddScramStation(scram.transform, Vector3.zero);

            // гравировка на кромке стола
            Build.Label(grp.transform, "ПУЛЬТ УПРАВЛЕНИЯ РЕАКТОРОМ", new Vector3(0f, 0.72f, 0.49f),
                        0.045f, new Color(0.55f, 0.57f, 0.54f), TextAnchor.MiddleCenter, 2.4f);

            AddFloorMark(grp.transform, 3.2f);
            AddChair(grp.transform, new Vector3(-0.55f, 0f, 0.98f), 180f);
        }

        // =================================================================
        // Пульт турбины
        // =================================================================
        static void BuildTurbineConsole(Transform root)
        {
            var grp = Build.Group("TurbineConsole", root, new Vector3(3.15f, 0f, -1.05f), new Vector3(0f, -18f, 0f));
            Transform face, deck;
            PanelFactory.MakeConsole(grp.transform, Vector3.zero, 0f, 2.9f, out face, out deck);
            AddConsoleLamp(grp.transform);

            PanelFactory.AddGauge(face, new Vector3(-1.05f, 0.20f, 0.001f), 0.30f,
                "ОБОРОТЫ, об/мин", Metric.Rpm, 0f, 3600f, 0.92f);
            PanelFactory.AddGauge(face, new Vector3(-0.35f, 0.20f, 0.001f), 0.30f,
                "МОЩНОСТЬ, МВт", Metric.GenMW, 0f, 1200f, 0.97f);
            PanelFactory.AddGauge(face, new Vector3(0.35f, 0.20f, 0.001f), 0.30f,
                "ДАВЛЕНИЕ ПАРА, МПа", Metric.SteamPressure, 0f, 10f, 0.85f);
            PanelFactory.AddGauge(face, new Vector3(1.05f, 0.20f, 0.001f), 0.30f,
                "УРОВЕНЬ В ПГ, %", Metric.DrumLevel, 0f, 100f);

            PanelFactory.AddReadout(face, new Vector3(-1.05f, -0.14f, 0.001f), new Vector2(0.55f, 0.15f),
                "RPM", Metric.Rpm, 0, "");
            PanelFactory.AddReadout(face, new Vector3(-0.35f, -0.14f, 0.001f), new Vector2(0.55f, 0.15f),
                "GEN", Metric.GenMW, 0, "МВт");
            PanelFactory.AddReadout(face, new Vector3(0.35f, -0.14f, 0.001f), new Vector2(0.55f, 0.15f),
                "STEAM", Metric.SteamPressure, 2, "МПа");
            PanelFactory.AddReadout(face, new Vector3(1.05f, -0.14f, 0.001f), new Vector2(0.55f, 0.15f),
                "LEVEL", Metric.DrumLevel, 0, "%");

            AddLampRow(face, new Vector3(0f, 0.40f, 0.001f), new[]
            {
                new LampDef("ГЕНЕРАТОР", LampMode.ControlOn, ControlId.Breaker, Metric.ReactorPowerPct, 0f, new Color(0.2f, 1f, 0.4f), false),
                new LampDef("ПИТАНИЕ", LampMode.ControlOn, ControlId.FeedAuto, Metric.ReactorPowerPct, 0f, new Color(0.2f, 1f, 0.4f), false),
                new LampDef("ОБОРОТЫ", LampMode.MetricAbove, ControlId.Breaker, Metric.Rpm, 3100f, new Color(1f, 0.75f, 0.15f), false),
                new LampDef("ТУРБИНА", LampMode.MetricBelow, ControlId.Breaker, Metric.Rpm, 2600f, new Color(1f, 0.3f, 0.1f), true)
            });

            PanelFactory.AddKnob(deck, new Vector3(-1.15f, 0.0f, 0.014f), 0.05f, ControlId.Valve,
                0f, 1f, 0.02f, "КЛАПАН ТУРБИНЫ");
            PanelFactory.AddReadout(deck, new Vector3(-1.15f, 0.145f, 0.014f), new Vector2(0.42f, 0.1f),
                "КЛАПАН", Metric.ValvePct, 0, "%");

            PanelFactory.AddKnob(deck, new Vector3(-0.55f, 0.0f, 0.014f), 0.05f, ControlId.Feed,
                0f, 1.5f, 0.02f, "ПИТАТ. НАСОСЫ");
            PanelFactory.AddReadout(deck, new Vector3(-0.55f, 0.145f, 0.014f), new Vector2(0.42f, 0.1f),
                "ПИТАНИЕ", Metric.FeedPct, 0, "%");

            PanelFactory.AddToggle(deck, new Vector3(-0.13f, 0.01f, 0.014f), ControlId.FeedAuto, "АВТО УРОВНЯ");
            PanelFactory.AddToggle(deck, new Vector3(0.45f, 0.01f, 0.014f), ControlId.Breaker, "ГЕНЕРАТОР");

            Build.Label(grp.transform, "ПУЛЬТ УПРАВЛЕНИЯ ТУРБИНОЙ", new Vector3(0f, 0.72f, 0.49f),
                        0.045f, new Color(0.55f, 0.57f, 0.54f), TextAnchor.MiddleCenter, 2.4f);

            AddFloorMark(grp.transform, 3.2f);
            AddChair(grp.transform, new Vector3(0.5f, 0f, 0.98f), 180f);
        }

        // =================================================================
        // Панель КИП и радиационного контроля (левая стена)
        // =================================================================
        static void BuildAuxPanel(Transform root)
        {
            var grp = Build.Group("AuxPanel", root, new Vector3(-6.42f, 1.62f, 1.5f), new Vector3(0f, 90f, 0f));
            Build.Box("Back", grp.transform, Vector3.zero, new Vector3(1.6f, 1.15f, 0.08f), MaterialLibrary.ConsoleBody);
            Build.Box("Face", grp.transform, new Vector3(0f, 0f, 0.045f), new Vector3(1.52f, 1.07f, 0.012f),
                      MaterialLibrary.PanelFace, false);
            var face = Build.Group("Content", grp.transform, new Vector3(0f, 0f, 0.052f), Vector3.zero).transform;

            Build.Label(face, "ПАНЕЛЬ КИП · РАДИАЦИОННЫЙ КОНТРОЛЬ", new Vector3(0f, 0.46f, 0f), 0.05f,
                        new Color(0.85f, 0.86f, 0.8f), TextAnchor.MiddleCenter, 1.4f);

            PanelFactory.AddGauge(face, new Vector3(-0.42f, 0.13f, 0f), 0.28f,
                "РЕАКТИВНОСТЬ", Metric.RhoMilli, -3f, 3f);

            PanelFactory.AddReadout(face, new Vector3(0.34f, 0.17f, 0f), new Vector2(0.62f, 0.2f),
                "РАДИАЦИЯ, мкЗв/ч", Metric.Radiation, 2, "мкЗв/ч");
            PanelFactory.AddReadout(face, new Vector3(0.34f, -0.06f, 0f), new Vector2(0.62f, 0.13f),
                "КСЕНОН", Metric.XenonPct, 1, "%");
            PanelFactory.AddReadout(face, new Vector3(0.34f, -0.24f, 0f), new Vector2(0.62f, 0.13f),
                "РАСХОД ГЦН", Metric.PumpsCount, 0, "%", s => (s.pumps / 3f * 100f).ToString("F0"));

            PanelFactory.AddResetStation(face, new Vector3(-0.42f, -0.26f, 0f));

            // лампа радиации
            var lampRoot = Build.Group("RadLamp", face, new Vector3(-0.42f, -0.02f, 0.01f), Vector3.zero);
            var dome = Build.Sphere("Dome", lampRoot.transform, Vector3.zero, new Vector3(0.035f, 0.032f, 0.02f),
                                    MaterialLibrary.Lamp(new Color(0.06f, 0.06f, 0.06f), 0.01f), false);
            Build.Label(face, "РАДИАЦИЯ", new Vector3(-0.42f, -0.065f, 0.01f), 0.02f,
                        new Color(0.8f, 0.82f, 0.78f), TextAnchor.MiddleCenter, 0.24f);
            var ind = lampRoot.AddComponent<IndicatorLamp>();
            ind.dome = dome.GetComponent<Renderer>();
            ind.mode = LampMode.MetricAbove;
            ind.metric = Metric.Radiation;
            ind.threshold = 1f;
            ind.onColor = new Color(1f, 0.65f, 0.1f);
        }

        // =================================================================
        // Табло аварийной сигнализации
        // =================================================================
        static void BuildAnnunciator(Transform root)
        {
            var grp = Build.Group("Annunciator", root, new Vector3(0f, 2.5f, -5.44f), Vector3.zero);
            Build.Box("Back", grp.transform, new Vector3(0f, 0f, -0.055f), new Vector3(4.1f, 1.7f, 0.08f),
                      MaterialLibrary.ConsoleBody);
            Build.Box("FacePanel", grp.transform, new Vector3(0f, 0f, 0.002f), new Vector3(4.0f, 1.62f, 0.02f),
                      MaterialLibrary.PanelFace, false);
            var face = Build.Group("Content", grp.transform, new Vector3(0f, 0f, 0.016f), Vector3.zero).transform;

            Build.Label(face, "АВАРИЙНАЯ  СИГНАЛИЗАЦИЯ", new Vector3(0f, 0.71f, 0f), 0.075f,
                        new Color(0.9f, 0.75f, 0.35f), TextAnchor.MiddleCenter, 3.2f);

            float[] xs = { -1.5f, -0.5f, 0.5f, 1.5f };
            float[] ys = { 0.42f, 0.13f, -0.16f, -0.45f };
            Vector2 tile = new Vector2(0.92f, 0.24f);

            PanelFactory.AddAlarmTile(face, new Vector3(xs[0], ys[0], 0f), tile, AlarmBits.PowerHigh, "МОЩНОСТЬ >105%");
            PanelFactory.AddAlarmTile(face, new Vector3(xs[1], ys[0], 0f), tile, AlarmBits.PowerHighHigh, "МОЩНОСТЬ >115%");
            PanelFactory.AddAlarmTile(face, new Vector3(xs[2], ys[0], 0f), tile, AlarmBits.FuelHot, "ТОПЛИВО >800°C");
            PanelFactory.AddAlarmTile(face, new Vector3(xs[3], ys[0], 0f), tile, AlarmBits.FuelHotHigh, "ТОПЛИВО >1000°C");

            PanelFactory.AddAlarmTile(face, new Vector3(xs[0], ys[1], 0f), tile, AlarmBits.PressHigh, "ПАР >7.0 МПа");
            PanelFactory.AddAlarmTile(face, new Vector3(xs[1], ys[1], 0f), tile, AlarmBits.PressHighHigh, "ПАР >8.5 МПа");
            PanelFactory.AddAlarmTile(face, new Vector3(xs[2], ys[1], 0f), tile, AlarmBits.LevelLow, "УРОВЕНЬ <30%");
            PanelFactory.AddAlarmTile(face, new Vector3(xs[3], ys[1], 0f), tile, AlarmBits.LevelLowLow, "УРОВЕНЬ <15%");

            PanelFactory.AddAlarmTile(face, new Vector3(xs[0], ys[2], 0f), tile, AlarmBits.Overspeed, "ОБОРОТЫ >3100");
            PanelFactory.AddAlarmTile(face, new Vector3(xs[1], ys[2], 0f), tile, AlarmBits.FlowLow, "РАСХОД ГЦН");
            PanelFactory.AddAlarmTile(face, new Vector3(xs[2], ys[2], 0f), tile, AlarmBits.RadHigh, "РАДИАЦИЯ");
            PanelFactory.AddAlarmTile(face, new Vector3(xs[3], ys[2], 0f), tile, AlarmBits.Scram, "АЗ СРАБОТАЛА");

            PanelFactory.AddAlarmTile(face, new Vector3(xs[0], ys[3], 0f), tile, AlarmBits.GenOffline, "ГЕНЕРАТОР ОТКЛ");
            PanelFactory.AddAlarmTile(face, new Vector3(xs[1], ys[3], 0f), tile, AlarmBits.TurbineTrip, "ТУРБИНА ТРИП");
            PanelFactory.AddAlarmTile(face, new Vector3(xs[2], ys[3], 0f), tile, AlarmBits.CoreDamage, "ПОВРЕЖД. АКТ.ЗОНЫ");
            PanelFactory.AddAckButton(face, new Vector3(xs[3], ys[3] + 0.02f, 0f));

            // сирена над табло
            var siren = Build.Group("Siren", grp.transform, new Vector3(-1.55f, 1.02f, 0.02f), Vector3.zero);
            Build.Box("Base", siren.transform, Vector3.zero, new Vector3(0.22f, 0.22f, 0.06f), MaterialLibrary.ConsoleTrim, false);
            Build.Cyl("Horn", siren.transform, new Vector3(0f, 0f, 0.12f), 0.1f, 0.22f,
                      MaterialLibrary.PlasticRed, new Vector3(90f, 0f, 0f));

            AddBeacon(grp.transform);
        }

        static void AddBeacon(Transform parent)
        {
            var grp = Build.Group("Beacon", parent, new Vector3(1.55f, 1.02f, 0.06f), Vector3.zero);
            Build.Box("Base", grp.transform, Vector3.zero, new Vector3(0.2f, 0.1f, 0.2f),
                      MaterialLibrary.ConsoleTrim, false);

            var rot = Build.Group("Rotator", grp.transform, new Vector3(0f, 0.08f, 0f), Vector3.zero);
            var dome = Build.Sphere("Dome", rot.transform, new Vector3(0f, 0.04f, 0f),
                                    new Vector3(0.17f, 0.13f, 0.17f),
                                    MaterialLibrary.Lamp(new Color(0.07f, 0.05f, 0.05f), 0.01f), false);
            Build.Box("Mirror", rot.transform, new Vector3(0.06f, 0.04f, 0f),
                      new Vector3(0.02f, 0.07f, 0.09f), MaterialLibrary.Chrome, false);

            var lgo = new GameObject("BeaconLight");
            lgo.transform.SetParent(rot.transform, false);
            lgo.transform.localPosition = new Vector3(0f, 0.06f, 0.13f);
            var l = lgo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.15f, 0.1f);
            l.intensity = 0f;
            l.range = 8f;

            var b = rot.AddComponent<AlarmBeacon>();
            b.beaconLight = l;
            b.dome = dome.GetComponent<Renderer>();
        }

        // =================================================================
        // Большая мнемосхема
        // =================================================================
        static void BuildMimic(Transform root)
        {
            var grp = Build.Group("MimicStand", root, new Vector3(5.45f, 1.66f, 3.35f), Vector3.zero);
            // повернуть к центру помещения
            Vector3 dir = new Vector3(0f, 1.66f, 0f) - grp.transform.position;
            float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            grp.transform.localEulerAngles = new Vector3(0f, yaw, 0f);
            PanelFactory.AddMimicScreen(grp.transform, Vector3.zero, new Vector2(1.78f, 1.1f), 0f);
        }

        // =================================================================
        // Реквизит: дверь, часы, трубы, плакаты, огнетушитель
        // =================================================================
        static void BuildProps(Transform root)
        {
            // дверь (группа повёрнута на 180°, поэтому «в комнату» = локальный +Z)
            var door = Build.Group("Door", root, new Vector3(-2.4f, 0f, 5.44f), new Vector3(0f, 180f, 0f));
            Build.Box("Frame", door.transform, new Vector3(0f, 1.16f, 0.02f), new Vector3(1.34f, 2.32f, 0.1f),
                      MaterialLibrary.ConsoleTrim);
            Build.Box("Leaf", door.transform, new Vector3(0f, 1.13f, 0.09f), new Vector3(1.16f, 2.22f, 0.06f),
                      MaterialLibrary.DoorSteel);
            Build.Cyl("Handle", door.transform, new Vector3(0.42f, 1.05f, 0.15f), 0.02f, 0.18f,
                      MaterialLibrary.Chrome, new Vector3(0f, 0f, 90f));
            Build.Box("ExitSign", door.transform, new Vector3(0f, 2.52f, 0.0f), new Vector3(0.62f, 0.2f, 0.04f),
                      MaterialLibrary.Lamp(new Color(0.1f, 0.85f, 0.35f), 0.55f), false);
            Build.Label(door.transform, "ВЫХОД", new Vector3(0f, 2.52f, 0.03f), 0.1f,
                        new Color(0.92f, 1f, 0.92f), TextAnchor.MiddleCenter, 0.55f);

            // часы на задней стене
            var clock = Build.Group("WallClockMount", root, new Vector3(2.2f, 2.95f, 5.42f), Vector3.zero);
            PanelFactory.AddClock(clock.transform, Vector3.zero, new Vector2(0.9f, 0.26f), 180f);

            // табличка «БЛОК №4»
            var plate = Build.Group("RoomPlate", root, new Vector3(-2.4f, 3.25f, 5.44f), new Vector3(0f, 180f, 0f));
            Build.Box("Plate", plate.transform, Vector3.zero, new Vector3(1.0f, 0.22f, 0.03f),
                      MaterialLibrary.AluPlate);
            Build.Label(plate.transform, "БЛОК №4 · ВВЭР-1000", new Vector3(0f, 0f, 0.025f), 0.075f,
                        new Color(0.14f, 0.14f, 0.14f), TextAnchor.MiddleCenter, 0.92f);

            // трубы под потолком у левой стены
            for (int i = 0; i < 2; i++)
            {
                float x = -6.25f + i * 0.17f;
                float y = 3.15f - i * 0.12f;
                Build.Cyl("Pipe", root, new Vector3(x, y, 0f), 0.06f, 10.6f,
                          i == 0 ? MaterialLibrary.Chrome : MaterialLibrary.Paint("pipe", new Color(0.55f, 0.32f, 0.22f), 0.4f, 0.6f),
                          new Vector3(90f, 0f, 0f));
                for (int k = 0; k < 5; k++)
                    Build.Cyl("Flange", root, new Vector3(x, y, -4.4f + k * 2.2f), 0.085f, 0.06f,
                              MaterialLibrary.ConsoleTrim, new Vector3(90f, 0f, 0f));
            }

            // кабельный лоток
            Build.Box("CableTray", root, new Vector3(1.8f, 3.32f, 0f), new Vector3(0.42f, 0.06f, 10.4f),
                      MaterialLibrary.ConsoleTrim, false);
            for (int i = 0; i < 12; i++)
                Build.Box("Cable", root, new Vector3(1.8f + (i % 3 - 1) * 0.1f, 3.36f, -5f + i * 0.9f),
                          new Vector3(0.05f, 0.03f, 0.85f),
                          MaterialLibrary.Paint("cable" + (i % 3), i % 3 == 0 ? new Color(0.1f, 0.1f, 0.12f) : new Color(0.2f, 0.2f, 0.22f), 0.2f), false);

            // плакат с инструкцией
            var poster = Build.Group("Poster", root, new Vector3(6.46f, 1.9f, -1.6f), new Vector3(0f, -90f, 0f));
            Build.Box("Sheet", poster.transform, Vector3.zero, new Vector3(1.25f, 0.95f, 0.02f),
                      MaterialLibrary.Paint("paper", new Color(0.88f, 0.87f, 0.82f), 0.1f), false);
            string[] lines =
            {
                "ПОРЯДОК ДЕЙСТВИЙ ПРИ АВАРИИ",
                "",
                "1. Нажать АЗ-5",
                "2. Проконтролировать ввод стержней",
                "3. Отключить генератор",
                "4. Следить за уровнем в ПГ",
                "5. Доложить начальнику смены"
            };
            for (int i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrEmpty(lines[i])) continue;
                Build.Label(poster.transform, lines[i], new Vector3(0f, 0.36f - i * 0.115f, 0.014f),
                            i == 0 ? 0.062f : 0.048f,
                            i == 0 ? new Color(0.55f, 0.08f, 0.06f) : new Color(0.12f, 0.12f, 0.12f),
                            TextAnchor.MiddleCenter, 1.15f);
            }

            // огнетушитель
            var ext = Build.Group("Extinguisher", root, new Vector3(6.3f, 0f, 4.75f), Vector3.zero);
            Build.Box("Bracket", ext.transform, new Vector3(0f, 0.55f, 0.16f), new Vector3(0.1f, 0.5f, 0.08f),
                      MaterialLibrary.ConsoleTrim, false);
            Build.Cyl("Body", ext.transform, new Vector3(0f, 0.42f, 0.28f), 0.11f, 0.52f,
                      MaterialLibrary.PlasticRed);
            Build.Cyl("Top", ext.transform, new Vector3(0f, 0.72f, 0.28f), 0.05f, 0.12f,
                      MaterialLibrary.PlasticBlack);
            Build.Label(ext.transform, "ОГНЕТУШ.", new Vector3(0f, 0.42f, 0.40f), 0.028f,
                        new Color(0.95f, 0.9f, 0.8f), TextAnchor.MiddleCenter, 0.2f);
        }

        // =================================================================
        // Вспомогательное
        // =================================================================
        struct LampDef
        {
            public string caption;
            public LampMode mode;
            public ControlId control;
            public Metric metric;
            public float threshold;
            public Color color;
            public bool invert;

            public LampDef(string caption, LampMode mode, ControlId control, Metric metric, float threshold, Color color, bool invert)
            {
                this.caption = caption; this.mode = mode; this.control = control;
                this.metric = metric; this.threshold = threshold; this.color = color; this.invert = invert;
            }
        }

        static void AddLampRow(Transform face, Vector3 center, LampDef[] defs)
        {
            float spacing = 0.5f;
            float startX = center.x - (defs.Length - 1) * spacing * 0.5f;
            for (int i = 0; i < defs.Length; i++)
            {
                var d = defs[i];
                var lampRoot = Build.Group("Lamp_" + d.caption, face,
                    new Vector3(startX + i * spacing, center.y, 0.004f), Vector3.zero);
                var dome = Build.Sphere("Dome", lampRoot.transform, Vector3.zero,
                                        new Vector3(0.032f, 0.028f, 0.02f),
                                        MaterialLibrary.Lamp(new Color(0.06f, 0.06f, 0.06f), 0.01f), false);
                Build.Cyl("Ring", lampRoot.transform, new Vector3(0f, 0f, -0.004f), 0.02f, 0.01f,
                          MaterialLibrary.Chrome, new Vector3(90f, 0f, 0f));
                Build.Label(lampRoot.transform, d.caption, new Vector3(0f, -0.043f, 0.004f), 0.019f,
                            new Color(0.75f, 0.77f, 0.72f), TextAnchor.MiddleCenter, 0.34f);

                var ind = lampRoot.AddComponent<IndicatorLamp>();
                ind.dome = dome.GetComponent<Renderer>();
                ind.mode = d.mode;
                ind.control = d.control;
                ind.metric = d.metric;
                ind.threshold = d.threshold;
                ind.onColor = d.color;
                ind.invert = d.invert;
            }
        }

        static void AddConsoleLamp(Transform console)
        {
            Build.Cyl("LampRodL", console, new Vector3(-0.4f, 1.86f, 0.02f), 0.012f, 0.34f,
                      MaterialLibrary.ConsoleTrim);
            Build.Cyl("LampRodR", console, new Vector3(0.4f, 1.86f, 0.02f), 0.012f, 0.34f,
                      MaterialLibrary.ConsoleTrim);
            var hood = Build.Box("LampHood", console, new Vector3(0f, 2.02f, 0.05f), new Vector3(1.05f, 0.05f, 0.16f),
                                 MaterialLibrary.ConsoleTrim, false);
            hood.transform.localEulerAngles = new Vector3(12f, 0f, 0f);
            Build.Box("LampStrip", console, new Vector3(0f, 2.0f, 0.1f), new Vector3(0.95f, 0.02f, 0.1f),
                      MaterialLibrary.Lamp(new Color(1f, 0.85f, 0.6f), 0.9f), false);

            var lightGo = new GameObject("ConsoleLight");
            lightGo.transform.SetParent(console, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.93f, 0.35f);
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.86f, 0.62f);
            l.intensity = 0.55f;
            l.range = 2.6f;
            l.shadows = LightShadows.None;
        }

        static void AddFloorMark(Transform console, float width)
        {
            Build.Box("FloorMark", console, new Vector3(0f, 0.006f, 0.72f), new Vector3(width * 0.85f, 0.004f, 0.5f),
                      MaterialLibrary.Paint("mark", new Color(0.62f, 0.52f, 0.12f), 0.2f), false);
        }

        static void AddChair(Transform console, Vector3 pos, float yaw)
        {
            var chair = Build.Group("Chair", console, pos, new Vector3(0f, yaw, 0f));
            Build.Cyl("Column", chair.transform, new Vector3(0f, 0.22f, 0f), 0.035f, 0.44f, MaterialLibrary.Chrome);
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f * Mathf.Deg2Rad;
                var leg = Build.Box("Leg", chair.transform,
                    new Vector3(Mathf.Sin(a) * 0.17f, 0.03f, Mathf.Cos(a) * 0.17f),
                    new Vector3(0.055f, 0.03f, 0.32f), MaterialLibrary.ConsoleTrim, false);
                leg.transform.localEulerAngles = new Vector3(0f, -i * 72f, 0f);
            }
            Build.Box("Seat", chair.transform, new Vector3(0f, 0.47f, 0f), new Vector3(0.46f, 0.08f, 0.44f),
                      MaterialLibrary.RubberBlack);
            var back = Build.Box("Back", chair.transform, new Vector3(0f, 0.72f, 0.21f), new Vector3(0.44f, 0.42f, 0.06f),
                                 MaterialLibrary.RubberBlack);
            back.transform.localEulerAngles = new Vector3(-8f, 0f, 0f);
            Build.Sphere("BackTop", chair.transform, new Vector3(0f, 0.93f, 0.24f),
                         new Vector3(0.44f, 0.06f, 0.06f), MaterialLibrary.RubberBlack, false);
        }
    }

    /// <summary>Красное аварийное освещение при авариях.</summary>
    public class EmergencyLight : MonoBehaviour
    {
        public Light target;
        float _t;

        void Update()
        {
            if (target == null || NPP.Core.GameManager.Instance == null) return;
            bool danger = (NPP.Core.GameManager.Instance.Latest.alarms &
                          ((int)NPP.Sim.AlarmBits.Scram | (int)NPP.Sim.AlarmBits.CoreDamage)) != 0;
            _t += Time.deltaTime;
            target.intensity = danger ? 0.55f + 0.35f * Mathf.Sin(_t * 3.2f) : 0f;
        }
    }
}
