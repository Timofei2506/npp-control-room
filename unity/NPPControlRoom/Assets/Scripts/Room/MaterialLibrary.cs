using System.Collections.Generic;
using UnityEngine;

namespace NPP.Room
{
    /// <summary>Процедурные текстуры и материалы. Всё генерируется в рантайме — ноль внешних ассетов.</summary>
    public static class MaterialLibrary
    {
        static Shader _lit, _unlit, _unlitTransparent;

        static bool _init;

        static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();
        static readonly Dictionary<int, Material> LampCache = new Dictionary<int, Material>();

        public static void EnsureInit()
        {
            if (_init) return;
            _init = true;
            _lit = Shader.Find("NPP/Lit");
            _unlit = Shader.Find("NPP/Unlit");
            _unlitTransparent = Shader.Find("NPP/UnlitTransparent");
            if (_lit == null) { Debug.LogError("Shader NPP/Lit not found"); _lit = Shader.Find("Standard"); }
            if (_unlit == null) { Debug.LogError("Shader NPP/Unlit not found"); _unlit = Shader.Find("Unlit/Texture"); }
            if (_unlitTransparent == null) _unlitTransparent = _unlit;
        }

        // ---------------- texturing ----------------
        public static Texture2D NoiseTex(int size, Color a, Color b, float scale, int seed, bool mono = false)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            var px = new Color[size * size];
            var rnd = new System.Random(seed);
            float off = rnd.Next(0, 100);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float nx = (float)x / size * scale + off;
                    float ny = (float)y / size * scale + off;
                    float n = Mathf.PerlinNoise(nx, ny) * 0.6f
                            + Mathf.PerlinNoise(nx * 3.1f, ny * 3.1f) * 0.3f
                            + Mathf.PerlinNoise(nx * 9.3f, ny * 9.3f) * 0.1f;
                    Color c = Color.Lerp(a, b, n);
                    if (!mono && rnd.Next(0, 100) < 5) c *= 0.95f;
                    px[y * size + x] = c;
                }
            tex.SetPixels(px);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.anisoLevel = 4;
            tex.Apply(true, false);
            return tex;
        }

        public static Texture2D FloorTex(int size)
        {
            var pc = new PixelCanvas(size, size, new Color32(62, 66, 70, 255));
            var baseC = new Color32(64, 68, 72, 255);
            var grout = new Color32(34, 36, 38, 255);
            var rnd = new System.Random(12345);
            pc.Clear(baseC);
            // лёгкий шум плитки
            for (int i = 0; i < size * 20; i++)
            {
                int x = rnd.Next(size), y = rnd.Next(size);
                var c = pc.Px[y * size + x];
                int d = rnd.Next(-8, 9);
                pc.Px[y * size + x] = new Color32(
                    (byte)Mathf.Clamp(c.r + d, 0, 255),
                    (byte)Mathf.Clamp(c.g + d, 0, 255),
                    (byte)Mathf.Clamp(c.b + d, 0, 255), 255);
            }
            int tile = size / 4;
            for (int i = 0; i <= 4; i++)
            {
                pc.FillRect(i * tile - 1, 0, 2, size, grout);
                pc.FillRect(0, i * tile - 1, size, 2, grout);
            }
            pc.Apply();
            pc.Texture.wrapMode = TextureWrapMode.Repeat;
            return pc.Texture;
        }

        public static Texture2D CeilTex(int size)
        {
            var pc = new PixelCanvas(size, size, new Color32(196, 198, 200, 255));
            pc.Clear(new Color32(198, 200, 202, 255));
            var line = new Color32(150, 152, 154, 255);
            int tile = size / 2;
            for (int i = 0; i <= 2; i++)
            {
                pc.FillRect(i * tile - 1, 0, 2, size, line);
                pc.FillRect(0, i * tile - 1, size, 2, line);
            }
            for (int i = 0; i < size * 6; i++)
            {
                int x = Random.Range(0, size), y = Random.Range(0, size);
                pc.P(x, y, new Color32(186, 188, 190, 255));
            }
            pc.Apply();
            pc.Texture.wrapMode = TextureWrapMode.Repeat;
            return pc.Texture;
        }

        static Material MakeLit(string key, Color color, float metallic, float gloss, Texture tex = null, Vector2? tiling = null)
        {
            Material m;
            if (Cache.TryGetValue(key, out m)) return m;
            m = new Material(_lit);
            m.SetColor("_Color", color);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Glossiness", gloss);
            m.SetColor("_EmissionColor", Color.black);
            if (tex != null)
            {
                m.SetTexture("_MainTex", tex);
                if (tiling != null) m.SetTextureScale("_MainTex", tiling.Value);
            }
            Cache[key] = m;
            return m;
        }

        // ---------------- публичные материалы ----------------
        public static Material Wall { get { EnsureInit(); return MakeLit("wall", new Color(0.62f, 0.66f, 0.63f), 0.02f, 0.22f, NoiseTex(256, new Color(0.58f, 0.62f, 0.59f), new Color(0.66f, 0.70f, 0.66f), 6f, 7), new Vector2(2f, 2f)); } }

        public static Material Floor { get { EnsureInit(); return MakeLit("floor", Color.white, 0.1f, 0.35f, FloorTex(256), new Vector2(6f, 6f)); } }

        public static Material Ceiling { get { EnsureInit(); return MakeLit("ceil", Color.white, 0.02f, 0.3f, CeilTex(256), new Vector2(4f, 4f)); } }

        public static Material Concrete { get { EnsureInit(); return MakeLit("concrete", new Color(0.55f, 0.56f, 0.57f), 0f, 0.15f, NoiseTex(256, new Color(0.5f, 0.51f, 0.52f), new Color(0.6f, 0.61f, 0.62f), 5f, 42), new Vector2(3f, 3f)); } }

        public static Material ConsoleBody { get { EnsureInit(); return MakeLit("console", new Color(0.34f, 0.38f, 0.40f), 0.35f, 0.45f); } }

        public static Material ConsoleTrim { get { EnsureInit(); return MakeLit("trim", new Color(0.16f, 0.17f, 0.18f), 0.5f, 0.5f); } }

        public static Material PanelFace { get { EnsureInit(); return MakeLit("panel", new Color(0.10f, 0.115f, 0.12f), 0.2f, 0.55f); } }

        public static Material PanelFaceLight { get { EnsureInit(); return MakeLit("panel2", new Color(0.72f, 0.73f, 0.70f), 0.15f, 0.4f); } }

        public static Material PlasticBlack { get { EnsureInit(); return MakeLit("plastic", new Color(0.07f, 0.07f, 0.075f), 0.05f, 0.5f); } }

        public static Material PlasticRed { get { EnsureInit(); return MakeLit("red", new Color(0.62f, 0.06f, 0.05f), 0.05f, 0.55f); } }

        public static Material PlasticYellow { get { EnsureInit(); return MakeLit("yellow", new Color(0.85f, 0.62f, 0.05f), 0.05f, 0.5f); } }

        public static Material PlasticGreen { get { EnsureInit(); return MakeLit("green", new Color(0.08f, 0.42f, 0.16f), 0.05f, 0.5f); } }

        public static Material Chrome { get { EnsureInit(); return MakeLit("chrome", new Color(0.75f, 0.76f, 0.78f), 0.9f, 0.75f); } }

        public static Material AluPlate { get { EnsureInit(); return MakeLit("alu", new Color(0.58f, 0.60f, 0.62f), 0.7f, 0.55f, NoiseTex(128, new Color(0.5f, 0.52f, 0.54f), new Color(0.66f, 0.68f, 0.7f), 12f, 99), new Vector2(2f, 1f)); } }

        public static Material RubberBlack { get { EnsureInit(); return MakeLit("rubber", new Color(0.03f, 0.03f, 0.03f), 0f, 0.1f); } }

        public static Material DeskTop { get { EnsureInit(); return MakeLit("desk", new Color(0.22f, 0.24f, 0.25f), 0.25f, 0.4f, NoiseTex(128, new Color(0.2f, 0.22f, 0.23f), new Color(0.26f, 0.28f, 0.29f), 20f, 5), new Vector2(4f, 1f)); } }

        public static Material DoorSteel { get { EnsureInit(); return MakeLit("door", new Color(0.3f, 0.33f, 0.35f), 0.6f, 0.5f, NoiseTex(128, new Color(0.28f, 0.31f, 0.33f), new Color(0.34f, 0.37f, 0.39f), 14f, 3), new Vector2(1f, 2f)); } }

        public static Material Glass { get { EnsureInit(); return MakeUnlitTransparent("glass", new Color(0.7f, 0.85f, 0.9f, 0.16f)); } }

        public static Material GlassRed { get { EnsureInit(); return MakeUnlitTransparent("glassred", new Color(0.8f, 0.12f, 0.1f, 0.34f)); } }

        static Material MakeUnlitTransparent(string key, Color c)
        {
            Material m;
            if (Cache.TryGetValue(key, out m)) return m;
            m = new Material(_unlitTransparent);
            m.SetTexture("_MainTex", Texture2D.whiteTexture);
            m.SetColor("_Color", c);
            Cache[key] = m;
            return m;
        }

        /// <summary>Произвольная окрашенная поверхность (кросспультовые цвета стен и корпусов).</summary>
        public static Material Paint(string key, Color color, float gloss = 0.35f, float metallic = 0.05f)
        {
            EnsureInit();
            return MakeLit("paint_" + key, color, metallic, gloss);
        }

        /// <summary>Экраны: непрозрачный unlit с текстурой и лёгким свечением.</summary>
        public static Material ScreenMat(Texture tex, float brightness = 1f)
        {
            EnsureInit();
            var m = new Material(_unlit);
            m.SetTexture("_MainTex", tex);
            m.SetColor("_Color", new Color(brightness, brightness, brightness, 1f));
            m.SetColor("_EmissionColor", new Color(brightness, brightness, brightness, 1f));
            return m;
        }

        /// <summary>Матовый цветной unlit — для надписей, табло без бликов.</summary>
        public static Material UnlitColor(Color c)
        {
            EnsureInit();
            var m = new Material(_unlit);
            m.SetColor("_Color", c);
            m.SetColor("_EmissionColor", Color.white);
            return m;
        }

        /// <summary>Светящийся колпачок лампы.</summary>
        public static Material Lamp(Color c, float intensity = 1.4f)
        {
            EnsureInit();
            int key = c.GetHashCode() ^ Mathf.RoundToInt(intensity * 10) * 397;
            Material m;
            if (LampCache.TryGetValue(key, out m) && m != null) return m;
            m = new Material(_lit);
            m.SetColor("_Color", c * 0.6f + new Color(0.06f, 0.06f, 0.06f));
            m.SetFloat("_Metallic", 0.0f);
            m.SetFloat("_Glossiness", 0.9f);
            m.SetColor("_EmissionColor", c * intensity);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            LampCache[key] = m;
            return m;
        }

        /// <summary>Тусклая (выключенная) лампа того же цвета.</summary>
        public static Material LampOff(Color c)
        {
            return Lamp(c * 0.12f + new Color(0.05f, 0.05f, 0.05f), 0.02f);
        }
    }
}
