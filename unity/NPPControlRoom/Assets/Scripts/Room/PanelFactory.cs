using System;
using NPP.Sim;
using UnityEngine;

namespace NPP.Room
{
    /// <summary>Сборка элементов пультов: приборы, тумблеры, верньеры, АЗ-5, табло.</summary>
    public static class PanelFactory
    {
        // ------------------------------------------------------------------
        // Корпус пульта: возвращает плоскость приборной панели и наклонный пульт
        // ------------------------------------------------------------------
        public static void MakeConsole(Transform parent, Vector3 pos, float yaw, float width,
                                       out Transform face, out Transform deck)
        {
            MaterialLibrary.EnsureInit();
            var root = Bld.Group("Console", parent, pos, new Vector3(0f, yaw, 0f));

            Bld.Box("Base", root.transform, new Vector3(0f, 0.36f, 0.02f),
                      new Vector3(width - 0.08f, 0.72f, 0.72f), MaterialLibrary.ConsoleBody);
            Bld.Box("Kick", root.transform, new Vector3(0f, 0.07f, 0.39f),
                      new Vector3(width - 0.14f, 0.14f, 0.02f), MaterialLibrary.ConsoleTrim);
            Bld.Box("DeskTop", root.transform, new Vector3(0f, 0.74f, 0.06f),
                      new Vector3(width + 0.06f, 0.05f, 0.84f), MaterialLibrary.DeskTop);
            Bld.Box("Back", root.transform, new Vector3(0f, 1.24f, -0.12f),
                      new Vector3(width, 0.95f, 0.07f), MaterialLibrary.ConsoleBody);
            Bld.Box("BackPlate", root.transform, new Vector3(0f, 1.24f, -0.077f),
                      new Vector3(width - 0.05f, 0.9f, 0.012f), MaterialLibrary.PanelFace);

            // наклонная панель тумблеров
            var plate = Bld.Box("DeckPlate", root.transform, new Vector3(0f, 0.79f, 0.16f),
                                  new Vector3(width - 0.03f, 0.022f, 0.42f), MaterialLibrary.PanelFace);
            plate.transform.localEulerAngles = new Vector3(15f, 0f, 0f);

            // световая полоска под кромкой стола
            var strip = Bld.Box("Strip", root.transform, new Vector3(0f, 0.715f, 0.475f),
                                  new Vector3(width - 0.2f, 0.012f, 0.012f), MaterialLibrary.ConsoleTrim);
            strip.GetComponent<MeshRenderer>().sharedMaterial =
                MaterialLibrary.Lamp(new Color(1f, 0.78f, 0.45f), 0.7f);

            face = Bld.Group("Face", root.transform, new Vector3(0f, 1.24f, -0.068f), Vector3.zero).transform;
            deck = Bld.Group("Deck", root.transform, new Vector3(0f, 0.806f, 0.164f), new Vector3(15f, 0f, 0f)).transform;
        }

        // ------------------------------------------------------------------
        // Стрелочный прибор
        // ------------------------------------------------------------------
        public static AnalogGauge AddGauge(Transform parent, Vector3 pos, float diameter,
                                           string caption, Metric metric, float min, float max,
                                           float redFrom = -1f)
        {
            var root = Bld.Group("Gauge_" + caption, parent, pos, Vector3.zero);

            var pc = new PixelCanvas(256, 256, new Color32(210, 207, 192, 255));
            DrawGaugeFace(pc, redFrom);
            var face = Bld.Quad("Face", root.transform, Vector3.zero, new Vector2(diameter, diameter),
                                  MaterialLibrary.ScreenMat(pc.Texture, 0.98f));

            // стрелка
            var needlePivot = Bld.Group("NeedlePivot", root.transform, new Vector3(0f, 0f, 0.006f), Vector3.zero);
            Bld.Box("Needle", needlePivot.transform, new Vector3(0f, diameter * 0.17f, 0f),
                      new Vector3(0.006f, diameter * 0.34f, 0.0025f), MaterialLibrary.PlasticBlack, false);
            Bld.Box("NeedleTip", needlePivot.transform, new Vector3(0f, diameter * 0.315f, 0.001f),
                      new Vector3(0.012f, diameter * 0.05f, 0.003f), MaterialLibrary.PlasticRed, false);

            Bld.Cyl("Cap", root.transform, new Vector3(0f, 0f, 0.009f), 0.013f, 0.008f,
                      MaterialLibrary.PlasticBlack, new Vector3(90f, 0f, 0f));

            Bld.Label(root.transform, caption, new Vector3(0f, -diameter * 0.5f - 0.033f, 0.002f),
                        0.03f, new Color(0.82f, 0.84f, 0.8f), TextAnchor.MiddleCenter, diameter + 0.12f);

            var g = root.AddComponent<AnalogGauge>();
            g.metric = metric;
            g.minValue = min;
            g.maxValue = max;
            g.needle = needlePivot.transform;
            return g;
        }

        static void DrawGaugeFace(PixelCanvas pc, float redFrom)
        {
            int s = pc.W;
            float cx = s / 2f, cy = s / 2f;
            var rim = new Color32(58, 58, 54, 255);
            var tick = new Color32(28, 28, 26, 255);
            var red = new Color32(186, 34, 24, 255);

            pc.FillCircle(cx, cy, s * 0.46f, new Color32(70, 70, 66, 255));
            pc.FillCircle(cx, cy, s * 0.43f, new Color32(210, 207, 192, 255));
            pc.Circle(cx, cy, s * 0.43f, rim, 3);

            const float a0 = 140f, a1 = -140f;
            if (redFrom >= 0f)
            {
                float startA = Mathf.Lerp(a0, a1, redFrom);
                pc.Arc(cx, cy, s * 0.355f, (90f + startA) * Mathf.Deg2Rad, (90f + a1) * Mathf.Deg2Rad, red, 9);
            }

            for (int i = 0; i <= 50; i++)
            {
                float f = i / 50f;
                float a = Mathf.Lerp(a0, a1, f);
                float th = (90f + a) * Mathf.Deg2Rad;
                bool major = i % 5 == 0;
                float r0 = s * (major ? 0.30f : 0.325f);
                float r1 = s * 0.385f;
                int x0 = Mathf.RoundToInt(cx + Mathf.Cos(th) * r0);
                int y0 = Mathf.RoundToInt(cy + Mathf.Sin(th) * r0);
                int x1 = Mathf.RoundToInt(cx + Mathf.Cos(th) * r1);
                int y1 = Mathf.RoundToInt(cy + Mathf.Sin(th) * r1);
                pc.Line(x0, y0, x1, y1, tick, major ? 4 : 2);
            }

            // деления «0», средняя зона
            pc.Circle(cx, cy, s * 0.40f, new Color32(150, 148, 138, 255), 1);
            // мелкий блик
            pc.Arc(cx, cy, s * 0.46f, 0.6f, 2.2f, new Color32(240, 240, 235, 90), 2);
            pc.Apply();
        }

        // ------------------------------------------------------------------
        // Цифровое табло
        // ------------------------------------------------------------------
        public static DigitalReadout AddReadout(Transform parent, Vector3 pos, Vector2 size, string label,
                                                Metric metric, int decimals, string unit,
                                                Func<PlantSnapshot, string> provider = null,
                                                Color32? fgOverride = null)
        {
            var root = Bld.Group("Readout_" + label, parent, pos, Vector3.zero);
            Bld.Box("Bezel", root.transform, new Vector3(0f, 0f, 0.004f),
                      new Vector3(size.x + 0.024f, size.y + 0.024f, 0.012f), MaterialLibrary.ConsoleTrim, false);
            var scr = Bld.Quad("Screen", root.transform, new Vector3(0f, 0f, 0.012f), size, null);

            var dr = scr.AddComponent<DigitalReadout>();
            dr.metric = metric;
            dr.decimals = decimals;
            dr.unit = unit;
            dr.label = label;
            dr.textProvider = provider;
            if (fgOverride.HasValue) dr.fg = fgOverride.Value;
            dr.Build();
            return dr;
        }

        // ------------------------------------------------------------------
        // Тумблер с лампой
        // ------------------------------------------------------------------
        public static void AddToggle(Transform parent, Vector3 pos, ControlId control, string caption,
                                     float width = 0.09f)
        {
            var root = Bld.Group("Toggle_" + control, parent, pos, Vector3.zero);
            Bld.Box("Plate", root.transform, new Vector3(0f, -0.005f, 0f),
                      new Vector3(width, 0.09f, 0.014f), MaterialLibrary.AluPlate);

            var pivot = Bld.Group("Pivot", root.transform, new Vector3(0f, 0.004f, 0.008f), Vector3.zero);
            Bld.Box("Lever", pivot.transform, new Vector3(0f, 0.031f, 0.006f),
                      new Vector3(0.011f, 0.058f, 0.011f), MaterialLibrary.Chrome, false);
            Bld.Sphere("Ball", pivot.transform, new Vector3(0f, 0.062f, 0.006f),
                         new Vector3(0.026f, 0.026f, 0.026f), MaterialLibrary.PlasticBlack, false);

            Bld.Label(root.transform, caption, new Vector3(0f, -0.062f, 0.004f), 0.024f,
                        new Color(0.8f, 0.82f, 0.78f), TextAnchor.MiddleCenter, width + 0.06f);

            // лампа состояния
            var lampRoot = Bld.Group("Lamp", root.transform, new Vector3(0f, 0.078f, 0.006f), Vector3.zero);
            var dome = Bld.Sphere("Dome", lampRoot.transform, Vector3.zero, new Vector3(0.022f, 0.02f, 0.014f),
                                     MaterialLibrary.Lamp(new Color(0.05f, 0.06f, 0.06f), 0.01f), false);
            var ring = Bld.Cyl("Ring", lampRoot.transform, new Vector3(0f, 0f, -0.002f), 0.014f, 0.008f,
                                 MaterialLibrary.Chrome, new Vector3(90f, 0f, 0f));

            var ind = lampRoot.AddComponent<IndicatorLamp>();
            ind.dome = dome.GetComponent<Renderer>();
            ind.mode = LampMode.ControlOn;
            ind.control = control;
            ind.onColor = new Color(0.25f, 1f, 0.4f);

            var t = root.AddComponent<ToggleLever>();
            t.command = control;
            t.lever = pivot.transform;
            t.offAngleX = 26f;
            t.onAngleX = -26f;
        }

        // ------------------------------------------------------------------
        // Поворотная ручка
        // ------------------------------------------------------------------
        public static RotaryKnob AddKnob(Transform parent, Vector3 pos, float radius, ControlId control,
                                         float min, float max, float step, string caption)
        {
            var root = Bld.Group("Knob_" + control, parent, pos, Vector3.zero);
            Bld.Box("Plate", root.transform, new Vector3(0f, 0f, 0.001f),
                      new Vector3(radius * 3.2f, radius * 3.2f, 0.01f), MaterialLibrary.PanelFace);
            Bld.Cyl("Base", root.transform, new Vector3(0f, 0f, 0.005f), radius + 0.012f, 0.012f,
                      MaterialLibrary.AluPlate, new Vector3(90f, 0f, 0f));
            var kp = Bld.Group("KnobPivot", root.transform, new Vector3(0f, 0f, 0.012f), Vector3.zero);
            Bld.Cyl("Knob", kp.transform, Vector3.zero, radius, 0.032f, MaterialLibrary.PlasticBlack,
                      new Vector3(90f, 0f, 0f));
            Bld.Box("Pointer", kp.transform, new Vector3(0f, radius * 0.52f, 0.017f),
                      new Vector3(0.007f, radius * 0.55f, 0.004f), MaterialLibrary.PlasticYellow, false);

            // риски вокруг ручки
            for (int i = 0; i <= 10; i++)
            {
                float ang = Mathf.Lerp(-135f, 135f, i / 10f);
                float rad = ang * Mathf.Deg2Rad;
                float rr = radius + 0.022f;
                var tickGo = Bld.Box("Tick", root.transform,
                    new Vector3(Mathf.Sin(rad) * rr, Mathf.Cos(rad) * rr, 0.006f),
                    new Vector3(0.003f, 0.012f, 0.002f), MaterialLibrary.PlasticBlack, false);
                tickGo.transform.localEulerAngles = new Vector3(0f, 0f, -ang);
            }

            Bld.Label(root.transform, caption, new Vector3(0f, -radius - 0.045f, 0.004f), 0.024f,
                        new Color(0.8f, 0.82f, 0.78f), TextAnchor.MiddleCenter, radius * 3f);

            var k = root.AddComponent<RotaryKnob>();
            k.command = control;
            k.min = min;
            k.max = max;
            k.step = step;
            k.knob = kp.transform;
            k.caption = caption;
            return k;
        }

        // ------------------------------------------------------------------
        // Кнопка
        // ------------------------------------------------------------------
        public static void AddPushButton(Transform parent, Vector3 pos, ControlId command, float value,
                                         string caption, Color color, float radius = 0.034f)
        {
            var root = Bld.Group("Btn_" + caption, parent, pos, Vector3.zero);
            Bld.Box("Plate", root.transform, new Vector3(0f, 0f, 0.002f),
                      new Vector3(radius * 3.1f, radius * 3.1f, 0.012f), MaterialLibrary.PanelFace);
            Bld.Cyl("Ring", root.transform, new Vector3(0f, 0f, 0.008f), radius + 0.014f, 0.018f,
                      MaterialLibrary.Chrome, new Vector3(90f, 0f, 0f));
            var capGroup = Bld.Group("Cap", root.transform, new Vector3(0f, 0f, 0.016f), Vector3.zero);
            var capMat = MaterialLibrary.Lamp(color, 0.25f);
            Bld.Cyl("CapMesh", capGroup.transform, Vector3.zero, radius, 0.024f, capMat,
                      new Vector3(90f, 0f, 0f));

            Bld.Label(root.transform, caption, new Vector3(0f, -radius - 0.033f, 0.004f), 0.022f,
                        new Color(0.8f, 0.82f, 0.78f), TextAnchor.MiddleCenter, radius * 4f);

            var b = root.AddComponent<PushButton>();
            b.command = command;
            b.value = value;
            b.cap = capGroup.transform;
            b.caption = caption;
        }

        // ------------------------------------------------------------------
        // АЗ-5: грибовидная кнопка с откидной крышкой
        // ------------------------------------------------------------------
        public static void AddScramStation(Transform parent, Vector3 pos)
        {
            var root = Bld.Group("AZ5", parent, pos, Vector3.zero);

            Bld.Box("Plate", root.transform, new Vector3(0f, 0f, 0f), new Vector3(0.34f, 0.34f, 0.02f),
                      MaterialLibrary.ConsoleTrim);
            Bld.Box("Sign", root.transform, new Vector3(0f, 0.20f, 0.006f), new Vector3(0.3f, 0.06f, 0.006f),
                      MaterialLibrary.PlasticYellow, false);
            Bld.Label(root.transform, "АЗ-5", new Vector3(0f, 0.20f, 0.012f), 0.04f, new Color(0.1f, 0.1f, 0.1f));

            // грибовидная кнопка
            Bld.Cyl("Guard", root.transform, new Vector3(0f, -0.01f, 0.016f), 0.075f, 0.03f,
                      MaterialLibrary.PlasticBlack, new Vector3(90f, 0f, 0f));
            var cap = Bld.Group("CapGroup", root.transform, new Vector3(0f, -0.01f, 0.034f), Vector3.zero);
            Bld.Cyl("Mushroom", cap.transform, Vector3.zero, 0.058f, 0.05f, MaterialLibrary.PlasticRed,
                      new Vector3(90f, 0f, 0f));
            Bld.Cyl("MushroomTop", cap.transform, new Vector3(0f, 0f, 0.014f), 0.045f, 0.02f,
                      MaterialLibrary.Lamp(new Color(0.7f, 0.05f, 0.04f), 0.35f), new Vector3(90f, 0f, 0f));

            // петля и крышка
            var hinge = Bld.Group("Hinge", root.transform, new Vector3(0f, 0.155f, 0.03f), Vector3.zero);
            var cover = Bld.Box("Cover", hinge.transform, new Vector3(0f, -0.155f, 0.004f),
                                  new Vector3(0.29f, 0.30f, 0.012f), MaterialLibrary.GlassRed);
            var coverLabel = Bld.Label(hinge.transform, "АВАРИЙНАЯ ЗАЩИТА",
                                         new Vector3(0f, -0.155f, 0.012f), 0.021f,
                                         new Color(1f, 0.9f, 0.85f), TextAnchor.MiddleCenter, 0.26f);
            var coverLabelMr = coverLabel.GetComponent<MeshRenderer>();
            coverLabelMr.sharedMaterial = Bld.UiFont.material;

            var fc = hinge.AddComponent<FlipCover>();
            fc.pivot = hinge.transform;
            fc.openAngle = 105f;

            var sb = root.AddComponent<ScramButton>();
            sb.cover = fc;
            sb.cap = cap.transform;

            var lamp = Bld.Sphere("Lamp", root.transform, new Vector3(0.12f, 0.12f, 0.012f),
                                    new Vector3(0.022f, 0.02f, 0.016f),
                                    MaterialLibrary.Lamp(new Color(0.06f, 0.06f, 0.06f), 0.01f), false);
            var ind = root.AddComponent<IndicatorLamp>();
            ind.dome = lamp.GetComponent<Renderer>();
            ind.mode = LampMode.ControlOn;
            ind.control = ControlId.Scram;
            ind.onColor = new Color(1f, 0.12f, 0.08f);
        }

        // ------------------------------------------------------------------
        // Табло аварийной сигнализации
        // ------------------------------------------------------------------
        public static void AddAlarmTile(Transform parent, Vector3 pos, Vector2 size, AlarmBits bit, string caption)
        {
            var root = Bld.Group("Alarm_" + bit, parent, pos, Vector3.zero);
            var plate = Bld.Box("Plate", root.transform, Vector3.zero,
                                  new Vector3(size.x, size.y, 0.035f),
                                  MaterialLibrary.Lamp(new Color(0.05f, 0.055f, 0.055f), 0.01f));
            Bld.Label(root.transform, caption, new Vector3(0f, 0f, 0.022f), 0.05f,
                        new Color(0.93f, 0.9f, 0.85f), TextAnchor.MiddleCenter, size.x - 0.04f);

            var tile = root.AddComponent<AlarmTile>();
            tile.bit = bit;
            tile.caption = caption;
            tile.face = plate.GetComponent<Renderer>();
        }

        // ------------------------------------------------------------------
        // Кнопка квитирования
        // ------------------------------------------------------------------
        public static void AddAckButton(Transform parent, Vector3 pos)
        {
            var root = Bld.Group("ACK", parent, pos, Vector3.zero);
            Bld.Box("Plate", root.transform, new Vector3(0f, 0f, 0.002f),
                      new Vector3(0.155f, 0.155f, 0.012f), MaterialLibrary.PanelFace);
            Bld.Cyl("Ring", root.transform, new Vector3(0f, 0f, 0.008f), 0.05f, 0.02f,
                      MaterialLibrary.Chrome, new Vector3(90f, 0f, 0f));
            var cap = Bld.Group("Cap", root.transform, new Vector3(0f, 0f, 0.018f), Vector3.zero);
            Bld.Cyl("CapMesh", cap.transform, Vector3.zero, 0.038f, 0.026f,
                      MaterialLibrary.Lamp(new Color(0.9f, 0.7f, 0.1f), 0.3f), new Vector3(90f, 0f, 0f));
            Bld.Label(root.transform, "КВИТ", new Vector3(0f, -0.075f, 0.004f), 0.024f,
                        new Color(0.85f, 0.85f, 0.8f), TextAnchor.MiddleCenter, 0.18f);
            var b = root.AddComponent<AckButton>();
            b.cap = cap.transform;
        }

        // ------------------------------------------------------------------
        // Сброс модели под защитной крышкой
        // ------------------------------------------------------------------
        public static void AddResetStation(Transform parent, Vector3 pos)
        {
            var root = Bld.Group("RESET", parent, pos, Vector3.zero);
            Bld.Box("Plate", root.transform, Vector3.zero, new Vector3(0.22f, 0.24f, 0.02f),
                      MaterialLibrary.ConsoleTrim);
            Bld.Label(root.transform, "СБРОС МОДЕЛИ", new Vector3(0f, 0.09f, 0.012f), 0.018f,
                        new Color(0.85f, 0.85f, 0.8f), TextAnchor.MiddleCenter, 0.2f);

            var hinge = Bld.Group("Hinge", root.transform, new Vector3(0f, 0.06f, 0.02f), Vector3.zero);
            Bld.Box("Cover", hinge.transform, new Vector3(0f, -0.06f, 0.004f), new Vector3(0.16f, 0.13f, 0.01f),
                      MaterialLibrary.Glass);
            var fc = hinge.AddComponent<FlipCover>();
            fc.pivot = hinge.transform;
            fc.openAngle = 100f;

            var btnRoot = Bld.Group("Button", root.transform, new Vector3(0f, -0.03f, 0f), Vector3.zero);
            Bld.Box("BtnPlate", btnRoot.transform, new Vector3(0f, 0f, 0.002f),
                      new Vector3(0.14f, 0.14f, 0.012f), MaterialLibrary.PanelFace);
            var capGroup = Bld.Group("Cap", btnRoot.transform, new Vector3(0f, 0f, 0.016f), Vector3.zero);
            Bld.Cyl("Ring", btnRoot.transform, new Vector3(0f, 0f, 0.008f), 0.036f, 0.016f,
                      MaterialLibrary.Chrome, new Vector3(90f, 0f, 0f));
            Bld.Cyl("CapMesh", capGroup.transform, Vector3.zero, 0.028f, 0.024f,
                      MaterialLibrary.Lamp(new Color(0.15f, 0.4f, 0.9f), 0.3f), new Vector3(90f, 0f, 0f));

            var pb = btnRoot.AddComponent<PushButton>();
            pb.command = ControlId.Reset;
            pb.value = 1f;
            pb.cap = capGroup.transform;
            pb.caption = "СБРОС";
        }

        // ------------------------------------------------------------------
        // Мнемосхема
        // ------------------------------------------------------------------
        public static void AddMimicScreen(Transform parent, Vector3 pos, Vector2 size, float yaw)
        {
            var root = Bld.Group("MimicScreen", parent, pos, new Vector3(0f, yaw, 0f));
            Bld.Box("Frame", root.transform, new Vector3(0f, 0f, -0.03f),
                      new Vector3(size.x + 0.12f, size.y + 0.12f, 0.06f), MaterialLibrary.ConsoleTrim);
            var scr = Bld.Quad("Screen", root.transform, new Vector3(0f, 0f, 0.001f), size, null);
            var ms = scr.AddComponent<MimicScreen>();
            ms.Build();

            // стойка до пола (pos.y — высота центра экрана над полом)
            float standCenter = -pos.y + 0.45f;
            Bld.Box("Stand", root.transform, new Vector3(0f, standCenter, -0.05f),
                      new Vector3(0.35f, 0.9f, 0.12f), MaterialLibrary.ConsoleBody);
            Bld.Box("Foot", root.transform, new Vector3(0f, -pos.y + 0.02f, 0.05f),
                      new Vector3(0.7f, 0.04f, 0.55f), MaterialLibrary.ConsoleTrim);
        }

        // ------------------------------------------------------------------
        // Часы
        // ------------------------------------------------------------------
        public static void AddClock(Transform parent, Vector3 pos, Vector2 size, float yaw)
        {
            var root = Bld.Group("Clock", parent, pos, new Vector3(0f, yaw, 0f));
            Bld.Box("Frame", root.transform, new Vector3(0f, 0f, -0.02f),
                      new Vector3(size.x + 0.06f, size.y + 0.06f, 0.05f), MaterialLibrary.ConsoleTrim);
            var scr = Bld.Quad("Screen", root.transform, new Vector3(0f, 0f, 0.006f), size, null);
            var cd = scr.AddComponent<ClockDisplay>();
            cd.texW = 224;
            cd.texH = 64;
            cd.Build();
        }

        // ------------------------------------------------------------------
        // Выключатель ГЦН (независимый)
        // ------------------------------------------------------------------
        public static void AddPumpToggle(Transform parent, Vector3 pos, int bitIndex, string caption, float width = 0.09f)
        {
            var root = Bld.Group("Pump_" + bitIndex, parent, pos, Vector3.zero);
            Bld.Box("Plate", root.transform, new Vector3(0f, -0.005f, 0f),
                      new Vector3(width, 0.09f, 0.014f), MaterialLibrary.AluPlate);

            var pivot = Bld.Group("Pivot", root.transform, new Vector3(0f, 0.004f, 0.008f), Vector3.zero);
            Bld.Box("Lever", pivot.transform, new Vector3(0f, 0.031f, 0.006f),
                      new Vector3(0.011f, 0.058f, 0.011f), MaterialLibrary.Chrome, false);
            Bld.Sphere("Ball", pivot.transform, new Vector3(0f, 0.062f, 0.006f),
                         new Vector3(0.026f, 0.026f, 0.026f), MaterialLibrary.PlasticBlack, false);

            Bld.Label(root.transform, caption, new Vector3(0f, -0.062f, 0.004f), 0.022f,
                        new Color(0.8f, 0.82f, 0.78f), TextAnchor.MiddleCenter, width + 0.08f);

            var lampRoot = Bld.Group("Lamp", root.transform, new Vector3(0f, 0.078f, 0.006f), Vector3.zero);
            var dome = Bld.Sphere("Dome", lampRoot.transform, Vector3.zero, new Vector3(0.022f, 0.02f, 0.014f),
                                     MaterialLibrary.Lamp(new Color(0.05f, 0.06f, 0.06f), 0.01f), false);
            Bld.Cyl("Ring", lampRoot.transform, new Vector3(0f, 0f, -0.002f), 0.014f, 0.008f,
                      MaterialLibrary.Chrome, new Vector3(90f, 0f, 0f));

            var ind = lampRoot.AddComponent<IndicatorLamp>();
            ind.dome = dome.GetComponent<Renderer>();
            ind.mode = LampMode.ControlOn;
            ind.control = ControlId.Pumps;
            ind.maskBit = bitIndex;
            ind.onColor = new Color(0.25f, 1f, 0.4f);

            var ps = root.AddComponent<PumpSwitch>();
            ps.bitIndex = bitIndex;
            ps.lever = pivot.transform;
            ps.caption = caption;
        }
    }
}
