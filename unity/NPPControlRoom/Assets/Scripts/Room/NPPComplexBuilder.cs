using NPP.Core;
using NPP.Sim;
using UnityEngine;

namespace NPP.Room
{
    /// <summary>
    /// Всё здание блока: блочный щит (командный центр) → коридор → реакторный зал →
    /// машинный зал. Строится процедурно, как и сам щит; внешних моделей нет.
    ///
    /// Планировка (вид сверху, +z — «в глубину» блока):
    ///
    ///   z=30  ┌───────────────────────────┬───────────────────────────┐
    ///         │                           │                           │
    ///         │     РЕАКТОРНЫЙ ЗАЛ        │       МАШИННЫЙ ЗАЛ        │
    ///         │      x ∈ [-9, 9]          │       x ∈ [9, 27]         │
    ///   z=12  └─────────┬─────────────────┴───────────────────────────┘
    ///                   │ коридор x ∈ [-4.6, -1.4]
    ///   z=5.65 ┌────────┴────────┐
    ///         │  БЛОЧНЫЙ ЩИТ    │  (13 × 11)
    ///   z=-5.5 └─────────────────┘
    /// </summary>
    public static class NPPComplexBuilder
    {
        // ---- размеры ----
        const float HallZ0 = 12f;      // начало залов
        const float HallZ1 = 30f;      // дальняя стена
        const float ReactorHalfX = 9f;
        const float TurbineX1 = 27f;
        const float HallHeight = 13f;

        const float CorridorX0 = -4.6f;
        const float CorridorX1 = -1.4f;
        const float CorridorH = 3.0f;

        public static Transform Build()
        {
            MaterialLibrary.EnsureInit();

            var root = new GameObject("NPP_Building").transform;

            // Командный центр (он же блочный щит) — как раньше.
            var control = ControlRoomBuilder.Build();
            control.SetParent(root, false);

            BuildCorridor(root);
            BuildReactorHall(root);
            BuildTurbineHall(root);

            return root;
        }

        // =================================================================
        // Вспомогательное
        // =================================================================
        static void Slab(Transform p, string name, float x0, float x1, float y, float z0, float z1, Material m)
        {
            Bld.Box(name, p,
                new Vector3((x0 + x1) * 0.5f, y, (z0 + z1) * 0.5f),
                new Vector3(x1 - x0, 0.2f, z1 - z0), m);
        }

        /// <summary>Стена вдоль X (постоянный z).</summary>
        static void WallX(Transform p, string name, float z, float x0, float x1, float y0, float y1,
                          float thickness = 0.3f, Material m = null)
        {
            Bld.Box(name, p,
                new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, z),
                new Vector3(x1 - x0, y1 - y0, thickness), m ?? MaterialLibrary.Wall);
        }

        /// <summary>Стена вдоль Z (постоянный x).</summary>
        static void WallZ(Transform p, string name, float x, float z0, float z1, float y0, float y1,
                          float thickness = 0.3f, Material m = null)
        {
            Bld.Box(name, p,
                new Vector3(x, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f),
                new Vector3(thickness, y1 - y0, z1 - z0), m ?? MaterialLibrary.Wall);
        }

        static void Sign(Transform p, string text, Vector3 pos, float yaw, float height = 0.16f)
        {
            var g = Bld.Group("Sign_" + text, p, pos, new Vector3(0f, yaw, 0f));
            Bld.Box("Plate", g.transform, Vector3.zero, new Vector3(1.5f, height * 2.4f, 0.03f),
                      MaterialLibrary.Paint("signplate", new Color(0.09f, 0.14f, 0.09f), 0.3f, 0.2f), false);
            Bld.Label(g.transform, text, new Vector3(0f, 0f, 0.02f), height,
                        new Color(0.85f, 0.92f, 0.7f), TextAnchor.MiddleCenter, 1.4f);
        }

        static void CeilingLamp(Transform p, Vector3 pos)
        {
            Bld.Box("LampHousing", p, pos + new Vector3(0f, 0.12f, 0f), new Vector3(0.7f, 0.12f, 0.24f),
                      MaterialLibrary.ConsoleTrim, false);
            Bld.Box("LampPanel", p, pos - new Vector3(0f, 0.02f, 0f), new Vector3(0.62f, 0.03f, 0.18f),
                      MaterialLibrary.Lamp(new Color(1f, 0.96f, 0.86f), 1.15f), false);
            var lgo = new GameObject("HallLight");
            lgo.transform.SetParent(p, false);
            lgo.transform.localPosition = pos + new Vector3(0f, -0.12f, 0f);
            var l = lgo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.95f, 0.85f);
            l.intensity = 1.15f;
            l.range = 11f;
            l.shadows = LightShadows.None;      // в залах много ламп: тени только у щита
            l.renderMode = LightRenderMode.ForceVertex;
        }

        static void Railing(Transform p, string name, Vector3 a, Vector3 b, float height = 1.05f)
        {
            var g = Bld.Group(name, p, Vector3.zero, Vector3.zero);
            Vector3 d = b - a;
            float len = d.magnitude;
            Vector3 mid = (a + b) * 0.5f;

            // верхняя и средняя перекладины
            for (int i = 0; i < 2; i++)
            {
                float h = height - i * 0.5f;
                var bar = Bld.Cyl("Rail", g.transform, mid + Vector3.up * h, 0.025f, len,
                                    MaterialLibrary.Paint("rail", new Color(0.75f, 0.55f, 0.12f), 0.5f, 0.4f),
                                    new Vector3(90f, 0f, 0f), true);
                bar.transform.rotation = Quaternion.LookRotation(d.normalized, Vector3.up)
                                       * Quaternion.Euler(90f, 0f, 0f);
            }

            // стойки
            int posts = Mathf.Max(2, Mathf.RoundToInt(len / 1.6f) + 1);
            for (int i = 0; i < posts; i++)
            {
                float t = posts == 1 ? 0f : i / (float)(posts - 1);
                Vector3 pos = Vector3.Lerp(a, b, t) + Vector3.up * (height * 0.5f);
                Bld.Box("Post", g.transform, pos, new Vector3(0.05f, height, 0.05f),
                          MaterialLibrary.Paint("rail", new Color(0.75f, 0.55f, 0.12f), 0.5f, 0.4f), false);
            }
        }

        /// <summary>Позиционный низкий гул (турбина/насосы): слышно по мере приближения.</summary>
        static void AddHallHum(Transform p, Vector3 pos, float volume)
        {
            var go = new GameObject("Hum");
            go.transform.SetParent(p, false);
            go.transform.localPosition = pos;

            var src = go.AddComponent<AudioSource>();
            src.clip = NPP.Audio.SoundBank.HallHumClip ?? NPP.Audio.SoundBank.HumClip;
            if (src.clip == null) { Object.Destroy(go); return; }
            src.loop = true;
            src.spatialBlend = 1f;
            src.volume = volume;
            src.playOnAwake = false;
            src.minDistance = 2.5f;
            src.maxDistance = 22f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.Play();
        }

        // =================================================================
        // Коридор: щит → залы
        // =================================================================
        static void BuildCorridor(Transform root)
        {
            var g = Bld.Group("Corridor", root, Vector3.zero, Vector3.zero).transform;
            const float z0 = 5.5f, z1 = HallZ0;
            float cx = (CorridorX0 + CorridorX1) * 0.5f;

            Slab(g, "Floor", CorridorX0, CorridorX1, -0.1f, z0, z1, MaterialLibrary.Floor);
            Bld.Box("Ceiling", g, new Vector3(cx, CorridorH + 0.1f, (z0 + z1) * 0.5f),
                      new Vector3(CorridorX1 - CorridorX0, 0.2f, z1 - z0), MaterialLibrary.Ceiling);

            WallZ(g, "WallL", CorridorX0, z0, z1, 0f, CorridorH, 0.2f);
            WallZ(g, "WallR", CorridorX1, z0, z1, 0f, CorridorH, 0.2f);

            // лампы по потолку
            for (int i = 0; i < 3; i++)
                CeilingLamp(g, new Vector3(cx, CorridorH - 0.05f, 6.6f + i * 2.2f));

            // таблички и указатели
            Sign(g, "РЕАКТОРНЫЙ ЗАЛ", new Vector3(cx, 2.55f, z1 - 0.02f), 180f, 0.13f);
            Sign(g, "К БЛОЧНОМУ ЩИТУ", new Vector3(cx, 2.55f, z0 + 0.02f), 0f, 0.11f);

            // трубы вдоль стены
            for (int i = 0; i < 2; i++)
                Bld.Cyl("Pipe", g, new Vector3(CorridorX0 + 0.18f, 2.25f - i * 0.35f, (z0 + z1) * 0.5f),
                          0.07f, z1 - z0, MaterialLibrary.Chrome, new Vector3(90f, 0f, 0f), true);

            // огнетушитель и щиток — чтобы коридор не был пустым
            Bld.Box("FireBox", g, new Vector3(CorridorX1 - 0.13f, 1.35f, 7.2f), new Vector3(0.12f, 0.55f, 0.4f),
                      MaterialLibrary.PlasticRed, false);
            Bld.Box("Panel", g, new Vector3(CorridorX1 - 0.12f, 1.5f, 9.4f), new Vector3(0.1f, 0.8f, 0.6f),
                      MaterialLibrary.AluPlate, false);
        }

        // =================================================================
        // Реакторный зал
        // =================================================================
        static void BuildReactorHall(Transform root)
        {
            var g = Bld.Group("ReactorHall", root, Vector3.zero, Vector3.zero).transform;
            float zc = (HallZ0 + HallZ1) * 0.5f;
            float depth = HallZ1 - HallZ0;

            Slab(g, "Floor", -ReactorHalfX, ReactorHalfX, -0.1f, HallZ0, HallZ1, MaterialLibrary.Concrete);
            Bld.Box("Ceiling", g, new Vector3(0f, HallHeight, zc), new Vector3(ReactorHalfX * 2f, 0.3f, depth),
                      MaterialLibrary.Concrete);

            // передняя стена зала — с проёмом под коридор
            float cz0 = CorridorX0, cz1 = CorridorX1;
            WallX(g, "WallFrontL", HallZ0, -ReactorHalfX, cz0, 0f, HallHeight);
            WallX(g, "WallFrontR", HallZ0, cz1, ReactorHalfX, 0f, HallHeight);
            WallX(g, "WallFrontTop", HallZ0, cz0, cz1, CorridorH, HallHeight);

            WallX(g, "WallBack", HallZ1, -ReactorHalfX, ReactorHalfX, 0f, HallHeight);
            WallZ(g, "WallLeft", -ReactorHalfX, HallZ0, HallZ1, 0f, HallHeight);

            // правая стена — общая с машинным залом, с проёмом
            const float doorZ0 = 20f, doorZ1 = 22.6f, doorH = 3.6f;
            WallZ(g, "WallRightA", ReactorHalfX, HallZ0, doorZ0, 0f, HallHeight);
            WallZ(g, "WallRightB", ReactorHalfX, doorZ1, HallZ1, 0f, HallHeight);
            WallZ(g, "WallRightTop", ReactorHalfX, doorZ0, doorZ1, doorH, HallHeight);
            Sign(g, "МАШИННЫЙ ЗАЛ →", new Vector3(ReactorHalfX - 0.02f, doorH + 0.45f, (doorZ0 + doorZ1) * 0.5f),
                 -90f, 0.12f);
            Sign(g, "БЛОЧНЫЙ ЩИТ · КОРИДОР", new Vector3((cz0 + cz1) * 0.5f, CorridorH + 0.42f, HallZ0 + 0.18f),
                 180f, 0.12f);

            // створка между залами: уезжает вдоль стены
            var hd = Bld.Group("HallDoor", g, new Vector3(ReactorHalfX, 0f, (doorZ0 + doorZ1) * 0.5f),
                                 new Vector3(0f, 90f, 0f)).transform;
            var hleaf = Bld.Box("Leaf", hd, new Vector3(0f, doorH * 0.5f, 0f),
                                  new Vector3(doorZ1 - doorZ0 - 0.06f, doorH - 0.08f, 0.08f),
                                  MaterialLibrary.Paint("halldoor", new Color(0.33f, 0.36f, 0.33f), 0.5f, 0.45f));
            var hslide = hleaf.AddComponent<SlidingDoor>();
            hslide.openOffset = new Vector3(doorZ1 - doorZ0, 0f, 0f);
            hslide.openDistance = 3.2f;
            hslide.speed = 3.2f;

            // пол-площадка: приямок вокруг реактора
            BuildReactor(g, new Vector3(0f, 0f, 21f));

            // парогенераторы по углам
            Vector3[] sgPos =
            {
                new Vector3(-5.4f, 0f, 16.2f),
                new Vector3(5.4f, 0f, 16.2f),
                new Vector3(-5.4f, 0f, 25.8f),
                new Vector3(5.4f, 0f, 25.8f)
            };
            for (int i = 0; i < sgPos.Length; i++)
                BuildSteamGenerator(g, sgPos[i], i);

            // главные циркуляционные насосы
            for (int i = 0; i < 2; i++)
                BuildPump(g, new Vector3(-3.2f + i * 6.4f, 0f, 26.6f), i);

            // мостовой кран
            BuildCrane(g, zc);

            // освещение
            for (int i = 0; i < 3; i++)
            {
                CeilingLamp(g, new Vector3(-5f, HallHeight - 0.2f, 15.5f + i * 6f));
                CeilingLamp(g, new Vector3(5f, HallHeight - 0.2f, 15.5f + i * 6f));
            }

            Sign(g, "РЕАКТОРНЫЙ ЗАЛ · БЛОК №4", new Vector3(0f, HallHeight - 1.2f, HallZ0 + 0.18f), 180f, 0.22f);

            AddHallHum(g, new Vector3(-3f, 2.2f, 18f), 0.7f);
            AddHallHum(g, new Vector3(4f, 2.2f, 24f), 0.55f);

            // ограждение вокруг реактора
            float r = 3.6f;
            Railing(g, "RailN", new Vector3(-r, 0f, 21f + r), new Vector3(r, 0f, 21f + r));
            Railing(g, "RailS", new Vector3(-r, 0f, 21f - r), new Vector3(r, 0f, 21f - r));
            Railing(g, "RailW", new Vector3(-r, 0f, 21f - r), new Vector3(-r, 0f, 21f + r));
            Railing(g, "RailE", new Vector3(r, 0f, 21f - r), new Vector3(r, 0f, 21f + r));

            // Информационная стойка у входа: мнемосхема блока и температуры.
            var info = Bld.Group("HallInfo", g, new Vector3(-1.1f, 0f, 13.4f), new Vector3(0f, 180f, 0f)).transform;
            Bld.Box("Post", info, new Vector3(0f, 1.05f, 0f), new Vector3(2.3f, 2.1f, 0.18f), MaterialLibrary.ConsoleBody);
            PanelFactory.AddMimicScreen(info, new Vector3(0f, 1.55f, 0.1f), new Vector2(1.5f, 0.5f), 0f);
            PanelFactory.AddReadout(info, new Vector3(-0.62f, 0.72f, 0.11f), new Vector2(0.52f, 0.18f),
                                    "ТОПЛИВО", Metric.FuelTemp, 0, " °C");
            PanelFactory.AddReadout(info, new Vector3(0f, 0.72f, 0.11f), new Vector2(0.52f, 0.18f),
                                    "ТЕПЛОНОСИТ.", Metric.CoolTemp, 0, " °C");
            PanelFactory.AddReadout(info, new Vector3(0.62f, 0.72f, 0.11f), new Vector2(0.52f, 0.18f),
                                    "ДАВЛЕНИЕ", Metric.SteamPressure, 2, " МПа");

            // щиты и шкафы вдоль левой стены
            for (int i = 0; i < 4; i++)
            {
                var cab = Bld.Box("Cabinet", g, new Vector3(-ReactorHalfX + 0.55f, 0.95f, 14.5f + i * 1.4f),
                                    new Vector3(0.7f, 1.9f, 1.2f), MaterialLibrary.Paint("cab", new Color(0.28f, 0.33f, 0.3f), 0.4f, 0.3f));
                Bld.Box("CabDoor", cab.transform, new Vector3(0.36f, 0f, 0f), new Vector3(0.02f, 1.7f, 1.05f),
                          MaterialLibrary.Paint("cabdoor", new Color(0.33f, 0.38f, 0.34f), 0.45f, 0.3f), false);
            }
        }

        static void BuildReactor(Transform p, Vector3 at)
        {
            var g = Bld.Group("Reactor", p, at, Vector3.zero).transform;

            // шахта
            Bld.Cyl("Pit", g, new Vector3(0f, -0.2f, 0f), 3.0f, 0.3f, MaterialLibrary.Concrete);

            // корпус реактора
            Bld.Cyl("Vessel", g, new Vector3(0f, 3.4f, 0f), 2.05f, 6.6f,
                      MaterialLibrary.Paint("vessel", new Color(0.42f, 0.46f, 0.44f), 0.6f, 0.45f), default(Vector3), true);
            Bld.Cyl("VesselTop", g, new Vector3(0f, 6.85f, 0f), 1.75f, 0.5f,
                      MaterialLibrary.Paint("vessel2", new Color(0.36f, 0.40f, 0.38f), 0.7f, 0.5f));
            Bld.Cyl("Flange", g, new Vector3(0f, 6.5f, 0f), 2.15f, 0.22f, MaterialLibrary.ConsoleTrim);

            // приводы СУЗ сверху
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                Bld.Cyl("Drive", g, new Vector3(Mathf.Cos(a) * 1.15f, 7.35f, Mathf.Sin(a) * 1.15f),
                          0.16f, 0.75f, MaterialLibrary.Chrome);
            }

            // главные циркуляционные трубопроводы (ГЦТ)
            for (int i = 0; i < 4; i++)
            {
                float a = (i / 4f) * Mathf.PI * 2f + Mathf.PI / 4f;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Bld.Cyl("GT", g, dir * 3.1f + Vector3.up * 2.4f, 0.26f, 2.6f,
                          MaterialLibrary.Paint("gt", new Color(0.55f, 0.5f, 0.42f), 0.5f, 0.5f),
                          new Vector3(90f, 0f, 0f), true);
            }

            // биологическая защита — кольцо бетона
            for (int i = 0; i < 12; i++)
            {
                float a = i / 12f * Mathf.PI * 2f;
                Bld.Box("BioShield", g, new Vector3(Mathf.Cos(a) * 2.75f, 0.55f, Mathf.Sin(a) * 2.75f),
                          new Vector3(0.55f, 1.1f, 0.55f), MaterialLibrary.Concrete, true);
            }
        }

        static void BuildSteamGenerator(Transform p, Vector3 at, int idx)
        {
            var g = Bld.Group("SG" + (idx + 1), p, at, Vector3.zero).transform;

            Bld.Cyl("Body", g, new Vector3(0f, 3.0f, 0f), 1.15f, 6.0f,
                      MaterialLibrary.Paint("sg", new Color(0.46f, 0.48f, 0.45f), 0.55f, 0.42f), default(Vector3), true);
            Bld.Cyl("Top", g, new Vector3(0f, 6.1f, 0f), 1.0f, 0.35f, MaterialLibrary.ConsoleTrim);
            Bld.Cyl("Base", g, new Vector3(0f, 0.15f, 0f), 1.35f, 0.3f, MaterialLibrary.Concrete);

            // паропровод к машинному залу
            Bld.Cyl("SteamPipe", g, new Vector3(1.05f, 6.2f, 0f), 0.3f, 1.9f,
                      MaterialLibrary.Paint("hpsteam", new Color(0.62f, 0.58f, 0.52f), 0.5f, 0.5f),
                      new Vector3(0f, 0f, 90f), true);

            Bld.Label(g.transform, "ПГ-" + (idx + 1), new Vector3(0f, 3.4f, -1.18f), 0.2f,
                        new Color(0.9f, 0.9f, 0.86f), TextAnchor.MiddleCenter, 0.9f);
        }

        static void BuildPump(Transform p, Vector3 at, int idx)
        {
            var g = Bld.Group("MCP" + (idx + 1), p, at, Vector3.zero).transform;
            Bld.Box("Base", g, new Vector3(0f, 0.15f, 0f), new Vector3(1.6f, 0.3f, 1.6f), MaterialLibrary.Concrete);
            Bld.Cyl("Motor", g, new Vector3(0f, 1.2f, 0f), 0.55f, 1.8f,
                      MaterialLibrary.Paint("motor", new Color(0.22f, 0.32f, 0.45f), 0.5f, 0.4f), default(Vector3), true);
            Bld.Cyl("Casing", g, new Vector3(0f, 0.45f, 0.55f), 0.42f, 0.5f,
                      MaterialLibrary.Chrome, new Vector3(90f, 0f, 0f), true);
            Bld.Label(g.transform, "ГЦН-" + (idx + 1), new Vector3(0f, 2.25f, 0f), 0.16f,
                        new Color(0.9f, 0.9f, 0.86f), TextAnchor.MiddleCenter, 0.8f);
        }

        static void BuildCrane(Transform p, float zc)
        {
            var g = Bld.Group("Crane", p, new Vector3(0f, 11.4f, zc), Vector3.zero).transform;

            // мост
            Bld.Box("Bridge", g, Vector3.zero, new Vector3(ReactorHalfX * 2f - 0.6f, 0.45f, 0.9f),
                      MaterialLibrary.Paint("crane", new Color(0.82f, 0.66f, 0.1f), 0.5f, 0.45f));
            Bld.Box("BridgeL", g, new Vector3(0f, 0f, -3.5f), new Vector3(ReactorHalfX * 2f - 0.6f, 0.16f, 0.16f),
                      MaterialLibrary.Paint("crane2", new Color(0.7f, 0.56f, 0.08f), 0.45f, 0.4f), false);

            // тележка и крюк
            Bld.Box("Trolley", g, new Vector3(-1.5f, -0.35f, 0f), new Vector3(1.0f, 0.4f, 1.0f),
                      MaterialLibrary.ConsoleTrim);
            Bld.Cyl("Cable", g, new Vector3(-1.5f, -2.1f, 0f), 0.03f, 3.4f, MaterialLibrary.ConsoleTrim);
            Bld.Box("Hook", g, new Vector3(-1.5f, -3.85f, 0f), new Vector3(0.28f, 0.4f, 0.14f),
                      MaterialLibrary.Chrome, false);
        }

        // =================================================================
        // Машинный зал
        // =================================================================
        static void BuildTurbineHall(Transform root)
        {
            var g = Bld.Group("TurbineHall", root, Vector3.zero, Vector3.zero).transform;
            float zc = (HallZ0 + HallZ1) * 0.5f;
            float depth = HallZ1 - HallZ0;
            float x0 = ReactorHalfX, x1 = TurbineX1;
            float xc = (x0 + x1) * 0.5f;

            Slab(g, "Floor", x0, x1, -0.1f, HallZ0, HallZ1, MaterialLibrary.Concrete);
            Bld.Box("Ceiling", g, new Vector3(xc, HallHeight, zc), new Vector3(x1 - x0, 0.3f, depth),
                      MaterialLibrary.Concrete);

            WallX(g, "WallFront", HallZ0, x0, x1, 0f, HallHeight);
            WallX(g, "WallBack", HallZ1, x0, x1, 0f, HallHeight);
            WallZ(g, "WallRight", x1, HallZ0, HallZ1, 0f, HallHeight);

            // турбоагрегат
            var t = Bld.Group("Turbine", g, new Vector3(18.5f, 0f, 20f), new Vector3(0f, 90f, 0f)).transform;
            Bld.Box("Foundation", t, new Vector3(0f, 0.35f, 0f), new Vector3(3.4f, 0.7f, 9.5f), MaterialLibrary.Concrete);

            // цилиндры высокого/низкого давления
            Bld.Cyl("HP", t, new Vector3(0f, 1.75f, 2.6f), 1.05f, 3.2f,
                      MaterialLibrary.Paint("turbine", new Color(0.55f, 0.56f, 0.58f), 0.65f, 0.55f),
                      new Vector3(90f, 0f, 0f), true);
            Bld.Cyl("LP", t, new Vector3(0f, 2.15f, -1.6f), 1.75f, 4.4f,
                      MaterialLibrary.Paint("turbine2", new Color(0.5f, 0.51f, 0.53f), 0.65f, 0.55f),
                      new Vector3(90f, 0f, 0f), true);

            // генератор
            Bld.Cyl("Generator", t, new Vector3(0f, 2.0f, -5.4f), 1.55f, 3.6f,
                      MaterialLibrary.Paint("gen", new Color(0.30f, 0.42f, 0.52f), 0.55f, 0.5f),
                      new Vector3(90f, 0f, 0f), true);
            Bld.Cyl("Exciter", t, new Vector3(0f, 2.0f, -7.5f), 0.75f, 0.9f, MaterialLibrary.ConsoleTrim);

            // конденсатор и трубопроводы
            Bld.Box("Condenser", g, new Vector3(21.5f, 1.2f, 17.5f), new Vector3(3.0f, 2.4f, 4.0f),
                      MaterialLibrary.Paint("cond", new Color(0.36f, 0.40f, 0.42f), 0.5f, 0.4f));
            for (int i = 0; i < 2; i++)
                Bld.Cyl("SteamLine", g, new Vector3(14.6f + i * 1.1f, 8.4f, zc), 0.32f, depth - 1.5f,
                          MaterialLibrary.Paint("hpsteam", new Color(0.62f, 0.58f, 0.52f), 0.5f, 0.5f),
                          new Vector3(90f, 0f, 0f), true);

            // мостовой кран машинного зала
            Bld.Box("CraneRunwayL", g, new Vector3(xc, 10.6f, HallZ0 + 1.2f), new Vector3(x1 - x0, 0.3f, 0.4f),
                      MaterialLibrary.Paint("crane", new Color(0.82f, 0.66f, 0.1f), 0.5f, 0.45f), false);
            Bld.Box("CraneRunwayR", g, new Vector3(xc, 10.6f, HallZ1 - 1.2f), new Vector3(x1 - x0, 0.3f, 0.4f),
                      MaterialLibrary.Paint("crane", new Color(0.82f, 0.66f, 0.1f), 0.5f, 0.45f), false);

            // Местный пульт у турбины — настоящие органы: клапан, генератор, читаемые приборы.
            var aux = Bld.Group("TurbineDesk", g, new Vector3(12.2f, 0f, 15.5f), new Vector3(0f, 90f, 0f)).transform;
            Bld.Box("Body", aux, new Vector3(0f, 0.42f, 0f), new Vector3(2.2f, 0.84f, 0.75f), MaterialLibrary.ConsoleBody);
            Bld.Box("Desk", aux, new Vector3(0f, 0.86f, 0f), new Vector3(2.3f, 0.06f, 0.85f), MaterialLibrary.DeskTop);
            Bld.Box("Back", aux, new Vector3(0f, 1.45f, -0.34f), new Vector3(2.2f, 0.75f, 0.08f), MaterialLibrary.PanelFace);

            PanelFactory.AddGauge(aux, new Vector3(-0.75f, 1.45f, -0.29f), 0.30f,
                                  "ОБОРОТЫ", Metric.Rpm, 0f, 3400f);
            PanelFactory.AddReadout(aux, new Vector3(0f, 1.52f, -0.29f), new Vector2(0.5f, 0.19f),
                                    "ГЕНЕРАТОР", Metric.GenMW, 0, " МВт");
            PanelFactory.AddReadout(aux, new Vector3(0.8f, 1.52f, -0.29f), new Vector2(0.5f, 0.19f),
                                    "ДАВЛЕНИЕ", Metric.SteamPressure, 2, " МПа");

            PanelFactory.AddKnob(aux, new Vector3(-0.55f, 0.93f, 0.05f), 0.075f, ControlId.Valve, 0f, 1f, 0.02f,
                                 "КЛАПАН ТУРБИНЫ");
            PanelFactory.AddPushButton(aux, new Vector3(0.35f, 0.93f, 0.05f), ControlId.Breaker, 1f,
                                       "ГЕНЕРАТОР В СЕТЬ", new Color(0.12f, 0.5f, 0.2f));
            PanelFactory.AddPushButton(aux, new Vector3(0.75f, 0.93f, 0.05f), ControlId.Breaker, 0f,
                                       "ОТКЛЮЧИТЬ", new Color(0.55f, 0.12f, 0.1f));

            for (int i = 0; i < 3; i++)
                CeilingLamp(g, new Vector3(13f + i * 5.5f, HallHeight - 0.2f, zc - 4f));
            for (int i = 0; i < 3; i++)
                CeilingLamp(g, new Vector3(13f + i * 5.5f, HallHeight - 0.2f, zc + 4f));

            Sign(g, "МАШИННЫЙ ЗАЛ · ТУРБОАГРЕГАТ №4", new Vector3(xc, HallHeight - 1.2f, HallZ0 + 0.18f), 180f, 0.2f);
            Sign(g, "← РЕАКТОРНЫЙ ЗАЛ", new Vector3(x0 + 0.02f, 4.1f, 21.3f), 90f, 0.12f);

            AddHallHum(g, new Vector3(17f, 2.5f, 20f), 0.9f);
        }
    }

    /// <summary>
    /// Раздвижная створка: уезжает вбок, когда игрок подходит ближе openDistance,
    /// и возвращается, когда уходит. Используется в проходах между помещениями.
    /// </summary>
    public class SlidingDoor : MonoBehaviour
    {
        public Vector3 openOffset = new Vector3(1.2f, 0f, 0f);
        public float openDistance = 3f;
        public float speed = 2.4f;

        Vector3 _closed;
        Vector3 _open;
        bool _opened;

        void Start()
        {
            _closed = transform.localPosition;
            _open = _closed + openOffset;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            bool want = false;
            if (gm != null && gm.Player != null)
            {
                float d = Vector3.Distance(gm.Player.transform.position, transform.position);
                want = d < openDistance;
            }
            if (want != _opened)
            {
                _opened = want;
                if (NPP.Audio.SoundBank.Ready) NPP.Audio.SoundBank.Toggle();
            }
            transform.localPosition = Vector3.MoveTowards(transform.localPosition,
                                                          _opened ? _open : _closed,
                                                          speed * Time.deltaTime);
        }
    }
}
