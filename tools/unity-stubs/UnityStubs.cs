// Минимальные заглушки UnityEngine / UnityEngine.UI / UnityEngine.EventSystems.
// Нужны ТОЛЬКО для офлайн-компиляционной проверки кода игры (mcs), в Unity не собираются.
// Подписи повторяют публичный API Unity настолько, насколько его использует проект.

using System;
using System.Collections;

namespace UnityEngine
{
    public class Object
    {
        public string name;
        public static void Destroy(Object o) { }
        public static void Destroy(Object o, float t) { }
        public static void DestroyImmediate(Object o) { }
        public static void DontDestroyOnLoad(Object o) { }
        public static T FindObjectOfType<T>() where T : Object { return null; }
        public static bool operator ==(Object a, Object b) { return ReferenceEquals(a, b); }
        public static bool operator !=(Object a, Object b) { return !ReferenceEquals(a, b); }
        public override bool Equals(object other) { return ReferenceEquals(this, other); }
        public override int GetHashCode() { return base.GetHashCode(); }
        public static implicit operator bool(Object exists) { return !ReferenceEquals(exists, null); }
    }

    public class Component : Object
    {
        public string tag { get; set; }
        public Transform transform { get { return null; } }
        public GameObject gameObject { get { return null; } }
        public T GetComponent<T>() { return default(T); }
        public Component GetComponent(Type t) { return null; }
        public T GetComponentInParent<T>() { return default(T); }
        public T GetComponentInChildren<T>() { return default(T); }
        public T[] GetComponentsInChildren<T>() { return null; }
    }

    public class Behaviour : Component { public bool enabled { get; set; } }

    public class MonoBehaviour : Behaviour { }

    public class GameObject : Object
    {
        public GameObject() { }
        public GameObject(string name) { }
        public Transform transform { get { return null; } }
        public int layer { get; set; }
        public string tag { get; set; }
        public bool activeSelf { get { return false; } }
        public string tag_ { get; set; }
        public void SetActive(bool value) { }
        public T AddComponent<T>() where T : Component { return default(T); }
        public T GetComponent<T>() { return default(T); }
        public static GameObject CreatePrimitive(PrimitiveType type) { return null; }
    }

    public class Transform : Component, IEnumerable
    {
        public Vector3 position { get; set; }
        public Vector3 localPosition { get; set; }
        public Vector3 eulerAngles { get; set; }
        public Vector3 localEulerAngles { get; set; }
        public Vector3 localScale { get; set; }
        public Vector3 forward { get { return Vector3.forward; } }
        public Vector3 right { get { return Vector3.right; } }
        public Transform parent { get; set; }
        public int childCount { get { return 0; } }
        public void SetParent(Transform p) { }
        public void SetParent(Transform p, bool worldPositionStays) { }
        public void Rotate(float x, float y, float z, Space relativeTo) { }
        public Transform Find(string n) { return null; }
        public Transform GetChild(int i) { return null; }
        public IEnumerator GetEnumerator() { return new Transform[0].GetEnumerator(); }
    }

    public class RectTransform : Transform
    {
        public Vector2 anchorMin { get; set; }
        public Vector2 anchorMax { get; set; }
        public Vector2 pivot { get; set; }
        public Vector2 anchoredPosition { get; set; }
        public Vector2 sizeDelta { get; set; }
        public Vector2 offsetMin { get; set; }
        public Vector2 offsetMax { get; set; }
    }

    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero { get { return new Vector2(0, 0); } }
        public static Vector2 one { get { return new Vector2(1, 1); } }
        public float magnitude { get { return 0; } }
        public float sqrMagnitude { get { return 0; } }
        public Vector2 normalized { get { return this; } }
        public static Vector2 operator +(Vector2 a, Vector2 b) { return new Vector2(a.x + b.x, a.y + b.y); }
        public static Vector2 operator -(Vector2 a, Vector2 b) { return new Vector2(a.x - b.x, a.y - b.y); }
        public static Vector2 operator *(Vector2 a, float d) { return new Vector2(a.x * d, a.y * d); }
        public static Vector2 operator *(float d, Vector2 a) { return new Vector2(a.x * d, a.y * d); }
        public static Vector2 operator /(Vector2 a, float d) { return new Vector2(a.x / d, a.y / d); }
        public static implicit operator Vector3(Vector2 v) { return new Vector3(v.x, v.y, 0); }
        public static implicit operator Vector2(Vector3 v) { return new Vector2(v.x, v.y); }
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public Vector3(float x, float y) { this.x = x; this.y = y; this.z = 0; }
        public static Vector3 zero { get { return new Vector3(0, 0, 0); } }
        public static Vector3 one { get { return new Vector3(1, 1, 1); } }
        public static Vector3 up { get { return new Vector3(0, 1, 0); } }
        public static Vector3 forward { get { return new Vector3(0, 0, 1); } }
        public static Vector3 right { get { return new Vector3(1, 0, 0); } }
        public float magnitude { get { return 0; } }
        public float sqrMagnitude { get { return 0; } }
        public Vector3 normalized { get { return this; } }
        public void Normalize() { }
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) { return a; }
        public static float Distance(Vector3 a, Vector3 b) { return 0; }
        public static Vector3 operator +(Vector3 a, Vector3 b) { return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z); }
        public static Vector3 operator -(Vector3 a, Vector3 b) { return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z); }
        public static Vector3 operator -(Vector3 a) { return new Vector3(-a.x, -a.y, -a.z); }
        public static Vector3 operator *(Vector3 a, float d) { return new Vector3(a.x * d, a.y * d, a.z * d); }
        public static Vector3 operator *(float d, Vector3 a) { return new Vector3(a.x * d, a.y * d, a.z * d); }
        public static Vector3 operator /(Vector3 a, float d) { return new Vector3(a.x / d, a.y / d, a.z / d); }
    }

    public struct Bounds
    {
        public Vector3 center { get; set; }
        public Vector3 size { get; set; }
        public Vector3 extents { get; set; }
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public Color(float r, float g, float b) : this(r, g, b, 1f) { }
        public static Color Lerp(Color a, Color b, float t) { return a; }
        public static Color white { get { return new Color(1, 1, 1, 1); } }
        public static Color black { get { return new Color(0, 0, 0, 1); } }
        public static Color clear { get { return new Color(0, 0, 0, 0); } }
        public static Color operator *(Color c, float d) { return new Color(c.r * d, c.g * d, c.b * d, c.a * d); }
        public static Color operator *(float d, Color c) { return c * d; }
        public static Color operator *(Color a, Color b) { return new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a); }
        public static Color operator +(Color a, Color b) { return new Color(a.r + b.r, a.g + b.g, a.b + b.b, a.a + b.a); }
        public static implicit operator Color32(Color c) { return new Color32((byte)(c.r * 255), (byte)(c.g * 255), (byte)(c.b * 255), (byte)(c.a * 255)); }
    }

    public struct Color32
    {
        public byte r, g, b, a;
        public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static implicit operator Color(Color32 c) { return new Color(c.r / 255f, c.g / 255f, c.b / 255f, c.a / 255f); }
    }

    public static class Mathf
    {
        public const float PI = 3.14159265f;
        public const float Deg2Rad = PI / 180f;
        public const float Rad2Deg = 180f / PI;
        public static float Abs(float v) { return Math.Abs(v); }
        public static int Abs(int v) { return Math.Abs(v); }
        public static float Sign(float v) { return Math.Sign(v); }
        public static float Min(float a, float b) { return Math.Min(a, b); }
        public static int Min(int a, int b) { return Math.Min(a, b); }
        public static float Max(float a, float b) { return Math.Max(a, b); }
        public static int Max(int a, int b) { return Math.Max(a, b); }
        public static float Clamp(float v, float a, float b) { return v < a ? a : (v > b ? b : v); }
        public static int Clamp(int v, int a, int b) { return v < a ? a : (v > b ? b : v); }
        public static float Clamp01(float v) { return Clamp(v, 0f, 1f); }
        public static float Lerp(float a, float b, float t) { return a + (b - a) * Clamp01(t); }
        public static float InverseLerp(float a, float b, float v) { return a == b ? 0f : Clamp01((v - a) / (b - a)); }
        public static int RoundToInt(float v) { return (int)Math.Round(v); }
        public static int FloorToInt(float v) { return (int)Math.Floor(v); }
        public static int CeilToInt(float v) { return (int)Math.Ceiling(v); }
        public static float Sin(float v) { return (float)Math.Sin(v); }
        public static float Cos(float v) { return (float)Math.Cos(v); }
        public static float Atan2(float y, float x) { return (float)Math.Atan2(y, x); }
        public static float Sqrt(float v) { return (float)Math.Sqrt(v); }
        public static float PerlinNoise(float x, float y) { return 0.5f; }
        public static float PingPong(float t, float length) { return 0; }
        public static float Repeat(float t, float length) { return 0; }
    }

    /// <summary>Виртуальные часы для офлайн-тестов (в Unity не используется).</summary>
    public static class SandboxClock { public static float Now; }

    public static class Time
    {
        public static float deltaTime { get { return 0f; } }
        public static float unscaledDeltaTime { get { return 0f; } }
        public static float time { get { return 0f; } }
        public static float unscaledTime { get { return 0f; } }
        public static float realtimeSinceStartup { get { return SandboxClock.Now; } }
        public static int frameCount { get { return 0; } }
        public static float timeScale { get; set; }
    }

    public enum KeyCode
    {
        None, Space, Escape, Return, F1, LeftShift, RightShift, Tab, LeftControl
    }

    public static class Input
    {
        public static float GetAxis(string name) { return 0f; }
        public static float GetAxisRaw(string name) { return 0f; }
        public static bool GetKey(KeyCode k) { return false; }
        public static bool GetKeyDown(KeyCode k) { return false; }
        public static bool GetKeyUp(KeyCode k) { return false; }
        public static bool GetMouseButton(int b) { return false; }
        public static bool GetMouseButtonDown(int b) { return false; }
        public static bool GetMouseButtonUp(int b) { return false; }
        public static Vector3 mousePosition { get { return Vector3.zero; } }
        public static Vector2 mouseScrollDelta { get { return Vector2.zero; } }
    }

    public enum CursorLockMode { None, Locked, Confined }

    public static class Cursor
    {
        public static bool visible { get; set; }
        public static CursorLockMode lockState { get; set; }
    }

    public enum RuntimeInitializeLoadType { AfterSceneLoad, BeforeSceneLoad, AfterAssembliesLoaded, BeforeSplashScreen, SubsystemRegistration }

    [AttributeUsage(AttributeTargets.Method)]
    public class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute() { }
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type) { }
    }

    public static class Application
    {
        public static int targetFrameRate { get; set; }
        public static bool runInBackground { get; set; }
        public static void Quit() { }
    }

    public static class PlayerPrefs
    {
        static readonly System.Collections.Generic.Dictionary<string, string> Store =
            new System.Collections.Generic.Dictionary<string, string>();

        public static float GetFloat(string key, float def) { string v; return Store.TryGetValue(key, out v) ? float.Parse(v, System.Globalization.CultureInfo.InvariantCulture) : def; }
        public static float GetFloat(string key) { return GetFloat(key, 0f); }
        public static int GetInt(string key, int def) { string v; return Store.TryGetValue(key, out v) ? int.Parse(v) : def; }
        public static string GetString(string key, string def) { string v; return Store.TryGetValue(key, out v) ? v : def; }
        public static string GetString(string key) { return GetString(key, ""); }
        public static void SetFloat(string key, float v) { Store[key] = v.ToString(System.Globalization.CultureInfo.InvariantCulture); }
        public static void SetInt(string key, int v) { Store[key] = v.ToString(); }
        public static void SetString(string key, string v) { Store[key] = v; }
        public static void DeleteKey(string key) { Store.Remove(key); }
        public static void Save() { }
    }

    public static class QualitySettings
    {
        public static int vSyncCount { get; set; }
        public static float shadowDistance { get; set; }
        public static int pixelLightCount { get; set; }
        public static int antiAliasing { get; set; }
    }

    public class TextAsset : Object { public string text { get { return ""; } } }

    public static class Resources
    {
        public static T Load<T>(string path) where T : Object { return null; }
        public static Object Load(string path) { return null; }
        public static T GetBuiltinResource<T>(string path) where T : Object { return null; }
    }

    public static class Debug
    {
        public static void Log(object msg) { }
        public static void LogWarning(object msg) { }
        public static void LogError(object msg) { }
    }

    public class Shader : Object
    {
        public bool isSupported { get { return true; } }
        public static Shader Find(string name) { return null; }
    }

    public class Texture : Object { }

    public enum TextureFormat { RGBA32, ARGB32, RGB24 }
    public enum TextureWrapMode { Repeat, Clamp }
    public enum FilterMode { Point, Bilinear, Trilinear }

    public class Texture2D : Texture
    {
        public Texture2D(int width, int height, TextureFormat format, bool mipChain) { }
        public Texture2D(int width, int height, TextureFormat format, bool mipChain, bool linear) { }
        public FilterMode filterMode { get; set; }
        public TextureWrapMode wrapMode { get; set; }
        public int anisoLevel { get; set; }
        public int width { get { return 0; } }
        public int height { get { return 0; } }
        public void SetPixels(Color[] colors) { }
        public void SetPixels32(Color32[] colors) { }
        public void Apply(bool updateMipmaps) { }
        public void Apply(bool updateMipmaps, bool makeNoLongerReadable) { }
        public static Texture2D whiteTexture { get { return null; } }
    }

    public class Material : Object
    {
        public Material(Shader shader) { }
        public Material(Material other) { }
        public Color color { get; set; }
        public Texture mainTexture { get; set; }
        public MaterialGlobalIlluminationFlags globalIlluminationFlags { get; set; }
        public void SetColor(string name, Color c) { }
        public void SetFloat(string name, float v) { }
        public void SetTexture(string name, Texture t) { }
        public void SetTextureScale(string name, Vector2 scale) { }
        public void EnableKeyword(string kw) { }
        public void DisableKeyword(string kw) { }
    }

    public enum MaterialGlobalIlluminationFlags { None, RealtimeEmissive, BakedEmissive }

    public class Font : Object
    {
        public Material material { get { return null; } }
        public static Font CreateDynamicFontFromOSFont(string name, int size) { return null; }
    }

    public enum TextAnchor
    {
        UpperLeft, UpperCenter, UpperRight,
        MiddleLeft, MiddleCenter, MiddleRight,
        LowerLeft, LowerCenter, LowerRight
    }

    public enum TextAlignment { Left, Center, Right }

    public class TextMesh : Component
    {
        public Font font { get; set; }
        public string text { get; set; }
        public int fontSize { get; set; }
        public float characterSize { get; set; }
        public TextAnchor anchor { get; set; }
        public TextAlignment alignment { get; set; }
        public Color color { get; set; }
    }

    public class AudioClip : Object
    {
        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency, bool stream) { return null; }
        public bool SetData(float[] data, int offsetSamples) { return true; }
    }

    public class AudioSource : Behaviour
    {
        public bool playOnAwake { get; set; }
        public bool loop { get; set; }
        public float spatialBlend { get; set; }
        public float volume { get; set; }
        public float pitch { get; set; }
        public AudioClip clip { get; set; }
        public bool isPlaying { get { return false; } }
        public void Play() { }
        public void Stop() { }
        public void PlayOneShot(AudioClip clip) { }
        public void PlayOneShot(AudioClip clip, float volumeScale) { }
    }

    public class AudioListener : Behaviour { }

    public class Camera : Behaviour
    {
        public float nearClipPlane { get; set; }
        public float farClipPlane { get; set; }
        public float fieldOfView { get; set; }
    }

    public class Collider : Component
    {
        public bool enabled { get; set; }
        public bool isTrigger { get; set; }
    }

    public class BoxCollider : Collider
    {
        public Vector3 size { get; set; }
        public Vector3 center { get; set; }
    }

    public class CapsuleCollider : Collider
    {
        public float height { get; set; }
        public float radius { get; set; }
    }

    public class MeshCollider : Collider { public bool convex { get; set; } }

    public enum CollisionFlags { None, Sides, Above, Below }

    public class CharacterController : Collider
    {
        public float height { get; set; }
        public float radius { get; set; }
        public float slopeLimit { get; set; }
        public float stepOffset { get; set; }
        public Vector3 center { get; set; }
        public bool isGrounded { get { return false; } }
        public CollisionFlags Move(Vector3 motion) { return CollisionFlags.None; }
    }

    public class Renderer : Component
    {
        public Material material { get; set; }
        public Material sharedMaterial { get; set; }
        public Bounds bounds { get { return new Bounds(); } }
        public bool enabled { get; set; }
    }

    public class MeshRenderer : Renderer
    {
        public UnityEngine.Rendering.ShadowCastingMode shadowCastingMode { get; set; }
        public bool receiveShadows { get; set; }
    }

    public enum LightType { Spot, Directional, Point, Area }

    public enum LightShadows { None, Hard, Soft }

    public enum LightRenderMode { Auto, ForcePixel, ForceVertex }

    public class Light : Behaviour
    {
        public LightType type { get; set; }
        public Color color { get; set; }
        public float intensity { get; set; }
        public float range { get; set; }
        public LightShadows shadows { get; set; }
        public float shadowStrength { get; set; }
        public LightRenderMode renderMode { get; set; }
    }

    public enum Space { World, Self }

    public enum PrimitiveType { Sphere, Capsule, Cylinder, Cube, Plane, Quad }

    public struct Ray
    {
        public Vector3 origin { get; set; }
        public Vector3 direction { get; set; }
        public Ray(Vector3 origin, Vector3 direction) { this.origin = origin; this.direction = direction; }
    }

    public struct RaycastHit
    {
        public Collider collider { get { return null; } }
        public Vector3 point { get { return Vector3.zero; } }
        public float distance { get { return 0f; } }
    }

    public enum QueryTriggerInteraction { UseGlobal, Ignore, Collide }

    public static class Physics
    {
        public static bool Raycast(Ray ray, out RaycastHit hit, float maxDistance, int layerMask, QueryTriggerInteraction query) { hit = new RaycastHit(); return false; }
        public static bool Raycast(Ray ray, out RaycastHit hit, float maxDistance, int layerMask) { hit = new RaycastHit(); return false; }
        public static bool Raycast(Ray ray, out RaycastHit hit, float maxDistance) { hit = new RaycastHit(); return false; }
    }

    public static class Random
    {
        public static int Range(int min, int max) { return min; }
        public static float Range(float min, float max) { return min; }
        public static float value { get { return 0f; } }
    }

    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }

    public static class RenderSettings
    {
        public static UnityEngine.Rendering.AmbientMode ambientMode { get; set; }
        public static Color ambientLight { get; set; }
        public static bool fog { get; set; }
        public static float reflectionIntensity { get; set; }
    }

    public enum FullScreenMode { ExclusiveFullScreen, FullScreenWindow, MaximizedWindow, Windowed }

    public enum ColorSpace { Gamma, Linear }
}

namespace UnityEngine.Rendering
{
    public enum ShadowCastingMode { Off, On, TwoSided, ShadowsOnly }
    public enum AmbientMode { Skybox = 0, Trilight = 1, Flat = 3, Custom = 4 }
}

namespace UnityEngine.Events
{
    public delegate void UnityAction();
    public delegate void UnityAction<T0>(T0 arg0);

    public class UnityEvent
    {
        public void AddListener(UnityAction call) { }
        public void RemoveListener(UnityAction call) { }
        public void Invoke() { }
    }

    public class UnityEvent<T0>
    {
        public void AddListener(UnityAction<T0> call) { }
        public void RemoveListener(UnityAction<T0> call) { }
        public void Invoke(T0 arg0) { }
    }
}

namespace UnityEngine.EventSystems
{
    public class EventSystem : Behaviour { }
    public class StandaloneInputModule : Behaviour { }
}

namespace UnityEngine.UI
{
    public class Graphic : Behaviour
    {
        public Color color { get; set; }
        public bool raycastTarget { get; set; }
    }

    public class Image : Graphic { }

    public class Text : Graphic
    {
        public Font font { get; set; }
        public string text { get; set; }
        public int fontSize { get; set; }
        public TextAnchor alignment { get; set; }
        public HorizontalWrapMode horizontalOverflow { get; set; }
        public VerticalWrapMode verticalOverflow { get; set; }
        public bool supportRichText { get; set; }
    }

    public enum HorizontalWrapMode { Wrap, Overflow }
    public enum VerticalWrapMode { Truncate, Overflow }

    public struct ColorBlock
    {
        public Color normalColor { get; set; }
        public Color highlightedColor { get; set; }
        public Color pressedColor { get; set; }
        public Color selectedColor { get; set; }
        public Color disabledColor { get; set; }
        public float colorMultiplier { get; set; }
        public float fadeDuration { get; set; }
    }

    public class Selectable : Behaviour
    {
        public ColorBlock colors { get; set; }
        public Graphic targetGraphic { get; set; }
        public bool interactable { get; set; }
    }

    public class Button : Selectable
    {
        public class ButtonClickedEvent : UnityEngine.Events.UnityEvent { }
        public ButtonClickedEvent onClick { get { return new ButtonClickedEvent(); } }
    }

    public class InputField : Selectable
    {
        public enum LineType { SingleLine, MultiLineSubmit, MultiLineNewline }
        public class OnChangeEvent : UnityEngine.Events.UnityEvent<string> { }
        public string text { get; set; }
        public int characterLimit { get; set; }
        public LineType lineType { get; set; }
        public Text textComponent { get; set; }
        public Graphic placeholder { get; set; }
        public OnChangeEvent onEndEdit { get { return new OnChangeEvent(); } }
        public void Select() { }
        public void ActivateInputField() { }
    }

    public class Slider : Selectable
    {
        public enum Direction { LeftToRight, RightToLeft, BottomToTop, TopToBottom }
        public class SliderEvent : UnityEngine.Events.UnityEvent<float> { }
        public RectTransform fillRect { get; set; }
        public RectTransform handleRect { get; set; }
        public Direction direction { get; set; }
        public float minValue { get; set; }
        public float maxValue { get; set; }
        public float value { get; set; }
        public SliderEvent onValueChanged { get { return new SliderEvent(); } }
    }

    public class Outline : Behaviour
    {
        public Color effectColor { get; set; }
        public Vector2 effectDistance { get; set; }
    }

    public class Canvas : Behaviour
    {
        public RenderMode renderMode { get; set; }
        public int sortingOrder { get; set; }
    }

    public class CanvasScaler : Behaviour
    {
        public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize, ConstantPhysicalSize }
        public ScaleMode uiScaleMode { get; set; }
        public Vector2 referenceResolution { get; set; }
        public float matchWidthOrHeight { get; set; }
    }

    public class GraphicRaycaster : Behaviour { }
}
