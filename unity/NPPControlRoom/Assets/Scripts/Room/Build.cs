using UnityEngine;

namespace NPP.Room
{
    /// <summary>Хелперы для процедурной сборки геометрии и надписей.</summary>
    public static class Build
    {
        static Font _font;
        static Font _fontMono;

        public static Font UiFont
        {
            get
            {
                if (_font == null)
                {
                    _font = Resources.Load<Font>("Fonts/DejaVuSans");
                    if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    if (_font == null) _font = Font.CreateDynamicFontFromOSFont("Arial", 32);
                }
                return _font;
            }
        }

        public static Font MonoFont
        {
            get
            {
                if (_fontMono == null)
                {
                    _fontMono = Resources.Load<Font>("Fonts/DejaVuSansMono");
                    if (_fontMono == null) _fontMono = UiFont;
                }
                return _fontMono;
            }
        }

        public static GameObject Group(string name, Transform parent, Vector3 pos, Vector3 euler)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = euler;
            return go;
        }

        public static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 size, Material mat, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            if (!collider) Object.Destroy(go.GetComponent<Collider>());
            return go;
        }

        public static GameObject Cyl(string name, Transform parent, Vector3 pos, float radius, float height, Material mat, Vector3 euler = default(Vector3), bool collider = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = euler;
            go.transform.localScale = new Vector3(radius * 2f, height / 2f, radius * 2f);
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            Object.Destroy(go.GetComponent<Collider>());
            if (collider)
            {
                var bc = go.AddComponent<BoxCollider>();
                bc.size = new Vector3(1f, 1f, 1f);
            }
            return go;
        }

        public static GameObject Sphere(string name, Transform parent, Vector3 pos, Vector3 size, Material mat, bool collider = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            Object.Destroy(go.GetComponent<Collider>());
            if (collider)
            {
                var bc = go.AddComponent<BoxCollider>();
                bc.size = Vector3.one;
            }
            return go;
        }

        public static GameObject Quad(string name, Transform parent, Vector3 pos, Vector2 size, Material mat, bool collider = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Object.Destroy(go.GetComponent<Collider>());
            if (collider)
            {
                var bc = go.AddComponent<BoxCollider>();
                bc.size = new Vector3(1f, 1f, 0.02f);
                bc.center = new Vector3(0f, 0f, 0.01f);
            }
            return go;
        }

        /// <summary>Плоская надпись на панели (гравировка/трафарет).</summary>
        public static TextMesh Label(Transform parent, string text, Vector3 localPos, float height, Color color, TextAnchor anchor = TextAnchor.MiddleCenter, float maxWidth = 0f)
        {
            var go = new GameObject("L_" + text);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var tm = go.AddComponent<TextMesh>();
            tm.font = UiFont;
            tm.text = text;
            tm.fontSize = 64;
            tm.characterSize = 1f;
            tm.anchor = anchor;
            tm.alignment = anchor == TextAnchor.MiddleLeft ? TextAlignment.Left :
                            anchor == TextAnchor.MiddleRight ? TextAlignment.Right : TextAlignment.Center;
            tm.color = color;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = UiFont.material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            // масштабируем по фактическим границам меша — не зависит от внутренней метрики TextMesh
            Bounds b = mr.bounds;
            if (b.size.y > 0.0001f)
            {
                float s = height / b.size.y;
                go.transform.localScale = new Vector3(s, s, s);
            }
            if (maxWidth > 0f)
            {
                b = mr.bounds;
                if (b.size.x > maxWidth || b.size.y > height * 1.15f)
                {
                    float s2 = Mathf.Min(maxWidth / Mathf.Max(b.size.x, 0.0001f), height * 1.15f / Mathf.Max(b.size.y, 0.0001f));
                    go.transform.localScale *= s2;
                }
            }

            // Меш TextMesh может быть ещё не построен в момент создания — доводим в LateUpdate.
            var fitter = go.AddComponent<TextFitter>();
            fitter.targetHeight = height;
            fitter.maxWidth = maxWidth;
            return tm;
        }

        public static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform) SetLayerRecursive(t.gameObject, layer);
        }

        public static void AddPointLight(Transform parent, Vector3 pos, Color color, float intensity, float range)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.Soft;
            l.renderMode = LightRenderMode.ForcePixel;
        }
    }

    /// <summary>Подгоняет размер надписи под заданную высоту, когда меш TextMesh уже построен.</summary>
    public class TextFitter : MonoBehaviour
    {
        public float targetHeight = 0.04f;
        public float maxWidth = 0f;

        int _frames;

        void LateUpdate()
        {
            if (_frames > 6) return;
            _frames++;

            var mr = GetComponent<MeshRenderer>();
            if (mr == null) return;

            Bounds b = mr.bounds;
            if (b.size.y < 1e-5f) return;

            float s = targetHeight / b.size.y;
            Vector3 sc = transform.localScale * s;
            if (maxWidth > 0f)
            {
                float w = b.size.x * s;
                if (w > maxWidth) sc *= maxWidth / w;
            }
            transform.localScale = sc;
            _frames = 99;
        }
    }
}
