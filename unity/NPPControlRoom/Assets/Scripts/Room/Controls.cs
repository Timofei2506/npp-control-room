using System;
using System.Collections.Generic;
using NPP.Audio;
using NPP.Core;
using NPP.Sim;
using UnityEngine;

namespace NPP.Room
{
    public interface IInteractable
    {
        string Hint();
        void Click();
        void Scroll(float delta);
        void Drag(Vector2 delta);
    }

    // =====================================================================
    // Стрелочный прибор
    // =====================================================================
    public class AnalogGauge : MonoBehaviour
    {
        public Metric metric = Metric.ReactorPowerPct;
        public float minValue = 0f;
        public float maxValue = 100f;
        public Transform needle;
        public float minAngle = 140f;
        public float maxAngle = -140f;

        float _angle = 140f;

        void Update()
        {
            if (GameManager.Instance == null || needle == null) return;
            PlantSnapshot s = GameManager.Instance.Latest;
            float v = Metrics.Get(s, metric);
            float t = Mathf.InverseLerp(minValue, maxValue, v);
            float target = Mathf.Lerp(minAngle, maxAngle, t);
            _angle = Mathf.Lerp(_angle, target, Time.deltaTime * 8f);
            needle.localEulerAngles = new Vector3(0f, 0f, _angle);
        }
    }

    // =====================================================================
    // Цифровое табло (семисегментные цифры или пиксельный текст для статусов)
    // =====================================================================
    public class DigitalReadout : MonoBehaviour
    {
        public Metric metric = Metric.ReactorPowerPct;
        public int decimals = 1;
        public string unit = "%";
        public string label = "";
        public Func<PlantSnapshot, string> textProvider;
        public Color32 fg = new Color32(120, 255, 170, 255);
        public Color32 bg = new Color32(5, 14, 9, 255);
        public int texW = 256;
        public int texH = 96;

        PixelCanvas _pc;
        Renderer _r;
        string _last = "";
        float _timer;

        public void Build()
        {
            _pc = new PixelCanvas(texW, texH, bg);
            _r = GetComponent<Renderer>();
            if (_r == null) _r = GetComponentInChildren<Renderer>();
            if (_r != null) _r.sharedMaterial = MaterialLibrary.ScreenMat(_pc.Texture, 1f);
            Redraw(ForceText(GameManager.Instance != null ? GameManager.Instance.Latest : default(PlantSnapshot)));
        }

        string ForceText(PlantSnapshot s)
        {
            if (textProvider != null) return textProvider(s);
            return Metrics.Get(s, metric).ToString("F" + decimals);
        }

        void Update()
        {
            if (_pc == null) return;
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = 0.12f;
            PlantSnapshot s = GameManager.Instance != null ? GameManager.Instance.Latest : default(PlantSnapshot);
            string txt = ForceText(s);
            if (txt != _last) Redraw(txt);
        }

        void Redraw(string value)
        {
            _last = value;
            _pc.Clear(bg);
            // рамка
            _pc.FrameRect(1, 1, texW - 2, texH - 2, 2, new Color32(28, 48, 36, 255));
            if (!string.IsNullOrEmpty(label))
                _pc.Text5x7(label, 6, texH - 16, 1, new Color32((byte)(fg.r * 0.55f), (byte)(fg.g * 0.55f), (byte)(fg.b * 0.55f), 255));

            bool status = textProvider != null;
            if (status)
            {
                int scale = value.Length <= 12 ? 3 : (value.Length <= 20 ? 2 : 1);
                int tw = _pc.TextWidth(value, scale) - scale;
                _pc.Text5x7(value, Mathf.Max(4, (texW - tw) / 2), texH / 2 - 7 * scale / 2 + 2, scale, fg);
            }
            else
            {
                // семисегментный индикатор
                int cell = 26, dotCell = 12, digitH = 46, thick = 4;
                int total = 0;
                foreach (char c in value) total += c == '.' ? dotCell : cell;
                int x = Mathf.Max(6, (texW - total) / 2);
                int y = 16;
                foreach (char c in value)
                {
                    if (c == '.') { _pc.FillRect(x, y, 5, 5, fg); x += dotCell; }
                    else { _pc.Digit7(c, x, y, cell - 3, digitH, thick, fg, new Color32((byte)(fg.r * 0.10f), (byte)(fg.g * 0.10f), (byte)(fg.b * 0.10f), 255)); x += cell; }
                }
                if (!string.IsNullOrEmpty(unit))
                {
                    int uw = _pc.TextWidth(unit, 1);
                    _pc.Text5x7(unit, texW - uw - 6, 4, 1, new Color32((byte)(fg.r * 0.7f), (byte)(fg.g * 0.7f), (byte)(fg.b * 0.7f), 255));
                }
            }
            _pc.Apply();
        }
    }

    // =====================================================================
    // Тумблер на два положения
    // =====================================================================
    public class ToggleLever : MonoBehaviour, IInteractable
    {
        public ControlId command = ControlId.Breaker;
        public Transform lever;
        public float offAngleX = 26f;
        public float onAngleX = -26f;

        float _angle;
        bool _localOn;
        float _lastIssue;
        bool _inited;

        void Update()
        {
            if (GameManager.Instance == null || lever == null) return;
            PlantSnapshot s = GameManager.Instance.Latest;
            float confirmed = CommandState.Read(s, command);
            bool confirmedOn = confirmed > 0.5f;
            if (!_inited) { _localOn = confirmedOn; _inited = true; }
            else if (Time.time - _lastIssue > 0.5f) _localOn = confirmedOn;

            float target = _localOn ? onAngleX : offAngleX;
            _angle = Mathf.Lerp(_angle, target, Time.deltaTime * 10f);
            lever.localEulerAngles = new Vector3(_angle, 0f, 0f);
        }

        public string Hint()
        {
            bool on = _inited ? _localOn : false;
            return "Тумблер " + command + " — сейчас " + (on ? "ВКЛ" : "ВЫКЛ") + " (ЛКМ переключить)";
        }

        public void Click()
        {
            _localOn = !_localOn;
            _lastIssue = Time.time;
            GameManager.Instance.IssueCommand(command, _localOn ? 1f : 0f);
            SoundBank.Toggle();
        }

        public void Scroll(float delta) { }
        public void Drag(Vector2 delta) { }
    }

    // =====================================================================
    // Поворотная ручка (верньер) — колесо мыши или перетаскивание
    // =====================================================================
    public class RotaryKnob : MonoBehaviour, IInteractable
    {
        public ControlId command = ControlId.Valve;
        public float min = 0f;
        public float max = 1f;
        public float step = 0.02f;
        public float angleRange = 270f;
        public Transform knob;
        public string caption = "";

        float _value;
        float _lastSend;
        float _angle;
        bool _inited;

        void Update()
        {
            if (GameManager.Instance == null || knob == null) return;
            float confirmed = CommandState.Read(GameManager.Instance.Latest, command);
            if (!_inited) { _value = confirmed; _inited = true; }
            else if (Time.time - _lastSend > 0.45f) _value = confirmed;

            float norm = Mathf.InverseLerp(min, max, _value);
            float target = -135f + norm * angleRange;
            _angle = Mathf.Lerp(_angle, target, Time.deltaTime * 12f);
            knob.localEulerAngles = new Vector3(0f, 0f, _angle);
        }

        void Push(float v, float sendInterval)
        {
            _value = Mathf.Clamp(v, min, max);
            if (Time.time - _lastSend >= sendInterval)
            {
                _lastSend = Time.time;
                GameManager.Instance.IssueCommand(command, _value);
                SoundBank.KnobTick();
            }
        }

        public string Hint()
        {
            return (string.IsNullOrEmpty(caption) ? command.ToString() : caption)
                   + ": " + Mathf.RoundToInt(Mathf.InverseLerp(min, max, _value) * 100f) + "%  [колесо мыши / ЛКМ+вертикальное движение]";
        }

        public void Click() { }

        public void Scroll(float delta)
        {
            Push(_value + Mathf.Sign(delta) * step * (max - min), 0.02f);
        }

        public void Drag(Vector2 delta)
        {
            float dv = delta.y * 0.0035f * (max - min);
            Push(_value + dv, 0.06f);
        }
    }

    // =====================================================================
    // Кнопка (нефиксируемая)
    // =====================================================================
    public class PushButton : MonoBehaviour, IInteractable
    {
        public ControlId command = ControlId.Scram;
        public float value = 1f;
        public Transform cap;
        public string caption = "";
        public Color capColor = new Color(0.1f, 0.1f, 0.1f);

        Vector3 _rest;
        float _restoreAt;
        bool _inited;

        void Start()
        {
            if (cap != null) { _rest = cap.localPosition; _inited = true; }
        }

        void Update()
        {
            if (!_inited) return;
            if (_restoreAt > 0f && Time.time > _restoreAt)
            {
                cap.localPosition = _rest;
                _restoreAt = 0f;
            }
        }

        public string Hint()
        {
            return (string.IsNullOrEmpty(caption) ? command.ToString() : caption) + " (ЛКМ нажать)";
        }

        public void Click()
        {
            if (cap != null) cap.localPosition = _rest + new Vector3(0f, 0f, -0.008f);
            _restoreAt = Time.time + 0.18f;
            GameManager.Instance.IssueCommand(command, value);
            SoundBank.Press();
        }

        public void Scroll(float delta) { }
        public void Drag(Vector2 delta) { }
    }

    // =====================================================================
    // Защитная крышка с петлёй
    // =====================================================================
    public class FlipCover : MonoBehaviour, IInteractable
    {
        public Transform pivot;
        public float openAngle = 105f;
        public bool IsOpen { get; private set; }

        float _angle;
        float _target;

        void Update()
        {
            _target = IsOpen ? openAngle : 0f;
            _angle = Mathf.Lerp(_angle, _target, Time.deltaTime * 8f);
            if (pivot != null) pivot.localEulerAngles = new Vector3(-_angle, 0f, 0f);
        }

        public string Hint()
        {
            return IsOpen ? "Закрыть защитную крышку" : "Открыть защитную крышку (ЛКМ)";
        }

        public void Click()
        {
            IsOpen = !IsOpen;
            SoundBank.Clunk();
        }

        public void Scroll(float delta) { }
        public void Drag(Vector2 delta) { }
    }

    // =====================================================================
    // АЗ-5 — грибовидная кнопка аварийной защиты
    // =====================================================================
    public class ScramButton : MonoBehaviour, IInteractable
    {
        public FlipCover cover;
        public Transform cap;
        public float pressedDepth = 0.02f;

        Vector3 _rest;
        float _restoreAt;
        bool _inited;

        void Start()
        {
            if (cap != null) { _rest = cap.localPosition; _inited = true; }
        }

        void Update()
        {
            if (!_inited) return;
            if (_restoreAt > 0f && Time.time > _restoreAt)
            {
                cap.localPosition = _rest;
                _restoreAt = 0f;
            }
        }

        public string Hint()
        {
            if (cover != null && !cover.IsOpen) return "АЗ-5 под защитной крышкой — сначала откройте крышку";
            return "АЗ-5 — АВАРИЙНАЯ ЗАЩИТА. Нажать (ЛКМ)";
        }

        public void Click()
        {
            if (cover != null && !cover.IsOpen) { SoundBank.Buzzer(); return; }
            if (cap != null) cap.localPosition = _rest + new Vector3(0f, 0f, -pressedDepth);
            _restoreAt = Time.time + 0.5f;
            GameManager.Instance.IssueCommand(ControlId.Scram, 1f);
            SoundBank.Press();
        }

        public void Scroll(float delta) { }
        public void Drag(Vector2 delta) { }
    }

    // =====================================================================
    // Сигнальная лампа
    // =====================================================================
    public enum LampMode { MetricAbove, MetricBelow, ControlOn }

    public class IndicatorLamp : MonoBehaviour
    {
        public LampMode mode = LampMode.ControlOn;
        public Metric metric = Metric.ReactorPowerPct;
        public float threshold = 50f;
        public ControlId control = ControlId.Breaker;
        public int maskBit = -1;      // >=0: читать бит maskBit из значения команды
        public bool invert = false;
        public Renderer dome;
        public Color onColor = new Color(0.2f, 1f, 0.35f);
        public float onIntensity = 1.5f;

        bool _state;
        bool _init;

        void Update()
        {
            if (GameManager.Instance == null || dome == null) return;
            PlantSnapshot s = GameManager.Instance.Latest;
            bool on;
            switch (mode)
            {
                case LampMode.MetricAbove: on = Metrics.Get(s, metric) > threshold; break;
                case LampMode.MetricBelow: on = Metrics.Get(s, metric) < threshold; break;
                default:
                    {
                        float cv = CommandState.Read(s, control);
                        if (maskBit >= 0) on = ((int)cv & (1 << maskBit)) != 0;
                        else on = cv > 0.5f;
                    }
                    break;
            }
            if (invert) on = !on;
            if (!_init || on != _state)
            {
                _init = true;
                _state = on;
                dome.sharedMaterial = on
                    ? MaterialLibrary.Lamp(onColor, onIntensity)
                    : MaterialLibrary.Lamp(onColor * 0.1f + new Color(0.03f, 0.035f, 0.035f), 0.01f);
            }
        }
    }

    // =====================================================================
    // Табло аварийной сигнализации
    // =====================================================================
    public class AlarmTile : MonoBehaviour, IInteractable
    {
        public AlarmBits bit;
        public string caption = "";
        public Renderer face;

        static readonly Color Red = new Color(1f, 0.12f, 0.08f);

        void Update()
        {
            if (GameManager.Instance == null || face == null) return;
            bool active = GameManager.Instance.IsAlarmActive(bit);
            bool acked = GameManager.Instance.IsAlarmAcked(bit);
            if (!active)
            {
                face.sharedMaterial = MaterialLibrary.Lamp(new Color(0.05f, 0.055f, 0.055f), 0.01f);
            }
            else if (!acked)
            {
                float blink = Mathf.PingPong(Time.time * 2.2f, 1f);
                face.sharedMaterial = MaterialLibrary.Lamp(Red, 0.5f + blink * 1.6f);
            }
            else
            {
                face.sharedMaterial = MaterialLibrary.Lamp(Red, 0.55f);
            }
        }

        public string Hint()
        {
            bool active = GameManager.Instance != null && GameManager.Instance.IsAlarmActive(bit);
            return caption + " — " + (active ? "АВАРИЯ (ЛКМ квитировать)" : "норма");
        }

        public void Click()
        {
            GameManager.Instance.Acknowledge(bit);
            SoundBank.Click();
        }

        public void Scroll(float delta) { }
        public void Drag(Vector2 delta) { }
    }

    // =====================================================================
    // Мнемосхема (пиксельный экран)
    // =====================================================================
    public class MimicScreen : MonoBehaviour
    {
        public int texW = 336;
        public int texH = 208;

        PixelCanvas _pc;
        Renderer _r;
        float _timer;
        readonly List<float> _hist = new List<float>();

        static readonly Color32 Bg = new Color32(7, 13, 20, 255);
        static readonly Color32 FrameC = new Color32(40, 62, 78, 255);
        static readonly Color32 Dim = new Color32(70, 105, 120, 255);
        static readonly Color32 Ciano = new Color32(110, 210, 230, 255);
        static readonly Color32 Green = new Color32(110, 240, 150, 255);
        static readonly Color32 Amber = new Color32(255, 190, 60, 255);
        static readonly Color32 RedC = new Color32(255, 80, 60, 255);

        public void Build()
        {
            _pc = new PixelCanvas(texW, texH, Bg);
            _r = GetComponent<Renderer>();
            if (_r == null) _r = GetComponentInChildren<Renderer>();
            if (_r != null) _r.sharedMaterial = MaterialLibrary.ScreenMat(_pc.Texture, 1f);
            DrawVoid();
        }

        void DrawVoid()
        {
            _pc.Clear(Bg);
            _pc.Text5x7("WAITING FOR DATA", 96, 100, 2, Ciano);
            _pc.Apply();
        }

        void Update()
        {
            if (_pc == null) return;
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = 0.25f;
            Draw();
        }

        void Draw()
        {
            PlantSnapshot s = GameManager.Instance != null ? GameManager.Instance.Latest : default(PlantSnapshot);
            bool has = GameManager.Instance != null && GameManager.Instance.HasState;

            _pc.Clear(Bg);
            _pc.FrameRect(0, 0, texW, texH, 1, FrameC);
            _pc.Text5x7("UNIT 4  VVER-1000  PROCESS MIMIC", 8, texH - 14, 1, Dim);
            _pc.Line(8, texH - 18, texW - 8, texH - 18, FrameC);

            if (!has)
            {
                _pc.Text5x7("WAITING FOR DATA", 96, 100, 2, Ciano);
                _pc.Apply();
                return;
            }

            float power = s.power;
            bool alarm = s.alarms != 0;

            // --- реактор ---
            int rx = 16, ry = 78, rw = 54, rh = 64;
            _pc.FrameRect(rx, ry, rw, rh, 2, Ciano);
            int fill = Mathf.Clamp(Mathf.RoundToInt(power * rh), 0, rh - 4);
            _pc.FillRect(rx + 3, ry + 2, rw - 6, fill, new Color32(40, 120, 90, 255));
            _pc.Text5x7("RPV", rx + 14, ry + rh - 12, 1, Ciano);
            ColoredPct(power * 100f, rx + 2, ry - 2);

            // --- парогенератор ---
            int sx = 96, sy = 78, sw = 46, sh = 64;
            _pc.FrameRect(sx, sy, sw, sh, 2, Ciano);
            int lf = Mathf.Clamp(Mathf.RoundToInt(s.level / 100f * (sh - 6)), 0, sh - 6);
            _pc.FillRect(sx + 3, sy + 2, sw - 6, lf, new Color32(30, 70, 110, 255));
            _pc.Text5x7("SG", sx + 16, sy + sh - 12, 1, Ciano);
            // давление
            DrawValue("PS", s.steamPress, 2, sx - 2, sy - 16);

            // --- турбина ---
            int tx = 168, ty = 96, tw = 56, th = 26;
            _pc.FrameRect(tx, ty, tw, th, 2, Ciano);
            _pc.Text5x7("TURB", tx + 14, ty + 8, 1, Dim);
            // вращение
            int phase = Mathf.Abs(Mathf.RoundToInt(s.rpm / 83f)) % 4;
            for (int i = 0; i < 4; i++)
            {
                int px = tx + 8 + ((i * 13 + phase * 3) % (tw - 16));
                _pc.FillRect(px, ty + 13, 3, 2, Ciano);
            }
            // генератор
            _pc.Circle(tx + tw + 14, ty + th / 2, 8, s.breaker ? Green : Dim, 2);
            _pc.Text5x7("G", tx + tw + 11, ty + th / 2 - 3, 1, s.breaker ? Green : Dim);
            DrawValue("MW", s.genMW, 0, tx, ty - 16);

            // --- связи ---
            _pc.Line(rx + rw, ry + rh / 2, sx, sy + sh / 2, Ciano);
            _pc.Line(sx + sw, sy + sh - 16, tx, ty + th / 2, Ciano);
            int flowPhase = Mathf.Abs(Mathf.RoundToInt(Time.time * 24f)) % 8;
            _pc.FillRect(Mathf.RoundToInt(Mathf.Lerp(rx + rw, sx - 4, flowPhase / 8f)), ry + rh / 2 - 1, 3, 2, Green);

            // конденсатор/линия от турбины вниз
            _pc.Line(tx + tw / 2, ty, tx + tw / 2, 66, Ciano);
            _pc.Line(tx + tw / 2, 66, sx + sw / 2, 66, Ciano);
            _pc.Text5x7("COND", tx + tw / 2 - 10, 54, 1, Dim);

            // --- параметры ---
            int px0 = 232;
            DrawLabeled("TF", s.fuelTemp, px0, 168, 0);
            DrawLabeled("TC", s.coolTemp, px0, 148, 0);
            DrawLabeled("LVL", s.level, px0, 128, 1);
            DrawLabeled("XE", s.xenon * 100f, px0, 108, 0);
            DrawLabeled("RHO", s.rho * 1000f, px0, 88, 2);
            DrawLabeled("RAD", s.rad, px0, 68, 2);

            // --- тренд мощности ---
            _hist.Add(power);
            if (_hist.Count > 100) _hist.RemoveAt(0);
            int gx = 232, gy = 20, gw = 96, gh = 36;
            _pc.FrameRect(gx, gy, gw, gh, 1, FrameC);
            for (int i = 1; i < _hist.Count; i++)
            {
                int x0 = gx + (int)((i - 1) / 100f * gw);
                int x1 = gx + (int)(i / 100f * gw);
                int y0 = gy + Mathf.Clamp(Mathf.RoundToInt(_hist[i - 1] * gh), 0, gh - 1);
                int y1 = gy + Mathf.Clamp(Mathf.RoundToInt(_hist[i] * gh), 0, gh - 1);
                _pc.Line(x0, y0, x1, y1, Green, 1);
            }
            _pc.Text5x7("POWER 100%", gx, gy + gh + 4, 1, Dim);

            // --- аварийная строка ---
            if (s.coreDamage)
                _pc.Text5x7("CORE DAMAGE - EVACUATE", 62, 8, 2, RedC);
            else if (s.scrammed)
                _pc.Text5x7("REACTOR SCRAM", 88, 8, 2, RedC);
            else if (alarm)
                _pc.Text5x7("ALARM ACTIVE", 100, 8, 2, Amber);
            else
                _pc.Text5x7("NORMAL OPERATION", 88, 8, 2, Green);

            _pc.Apply();
            if (_r != null && _r.sharedMaterial != null) _r.sharedMaterial.mainTexture = _pc.Texture;
        }

        void ColoredPct(float pct, int x, int y)
        {
            string t = Mathf.RoundToInt(pct) + "%";
            _pc.Text5x7(t, x, y, 1, pct > 110 ? RedC : (pct > 104 ? Amber : Green));
        }

        void DrawValue(string tag, float v, int dec, int x, int y)
        {
            string t = tag + " " + v.ToString("F" + dec);
            _pc.Text5x7(t, x, y, 1, Ciano);
        }

        void DrawLabeled(string tag, float v, int x, int y, int dec)
        {
            _pc.Text5x7(tag, x, y, 1, Dim);
            string t = v.ToString("F" + dec);
            _pc.Text5x7(t, x + 26, y, 1, Ciano);
        }
    }

    // =====================================================================
    // Часы на стене
    // =====================================================================
    public class ClockDisplay : MonoBehaviour
    {
        public int texW = 224;
        public int texH = 64;

        PixelCanvas _pc;
        float _timer;

        public void Build()
        {
            _pc = new PixelCanvas(texW, texH, new Color32(4, 6, 8, 255));
            Renderer r = GetComponent<Renderer>();
            if (r == null) r = GetComponentInChildren<Renderer>();
            if (r != null) r.sharedMaterial = MaterialLibrary.ScreenMat(_pc.Texture, 1f);
            Redraw();
        }

        void Update()
        {
            if (_pc == null) return;
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = 0.5f;
            Redraw();
        }

        void Redraw()
        {
            DateTime now = DateTime.Now;
            string t = now.ToString("HH:mm:ss");
            _pc.Clear(new Color32(4, 6, 8, 255));
            int x = 12;
            foreach (char c in t)
            {
                if (c == ':')
                {
                    _pc.FillRect(x + 2, 20, 4, 4, new Color32(255, 160, 60, 255));
                    _pc.FillRect(x + 2, 40, 4, 4, new Color32(255, 160, 60, 255));
                    x += 12;
                }
                else
                {
                    _pc.Digit7(c, x, 10, 24, 44, 4, new Color32(255, 176, 70, 255), new Color32(40, 26, 10, 255));
                    x += 30;
                }
            }
            _pc.Apply();
        }
    }

    // =====================================================================
    // Индикатор уровня в виде вертикальной шкалы
    // =====================================================================
    public class LevelBar : MonoBehaviour
    {
        public Metric metric = Metric.DrumLevel;
        public float min = 0f, max = 100f;
        public Transform fill;
        public float height = 0.2f;

        void Update()
        {
            if (GameManager.Instance == null || fill == null) return;
            float v = Metrics.Get(GameManager.Instance.Latest, metric);
            float t = Mathf.Clamp01(Mathf.InverseLerp(min, max, v));
            Vector3 sc = fill.localScale;
            sc.y = Mathf.Lerp(sc.y, Mathf.Max(0.01f, t * height), Time.deltaTime * 8f);
            fill.localScale = sc;
            Vector3 p = fill.localPosition;
            fill.localPosition = new Vector3(p.x, -height * 0.5f + sc.y * 0.5f, p.z);
        }
    }

    // =====================================================================
    // Кнопка квитирования сигнализации
    // =====================================================================
    public class AckButton : MonoBehaviour, IInteractable
    {
        public Transform cap;
        Vector3 _rest;
        float _restoreAt;

        void Start() { if (cap != null) _rest = cap.localPosition; }

        void Update()
        {
            if (cap != null && _restoreAt > 0f && Time.time > _restoreAt)
            {
                cap.localPosition = _rest;
                _restoreAt = 0f;
            }
        }

        public string Hint() { return "КВИТИРОВАНИЕ сигнализации (ЛКМ)"; }

        public void Click()
        {
            if (cap != null) cap.localPosition = _rest + new Vector3(0f, 0f, 0.012f);
            _restoreAt = Time.time + 0.16f;
            GameManager.Instance.AcknowledgeAll();
            SoundBank.Click();
        }

        public void Scroll(float delta) { }
        public void Drag(Vector2 delta) { }
    }

    // =====================================================================
    // Проблесковый маяк на стене
    // =====================================================================
    public class AlarmBeacon : MonoBehaviour
    {
        public Light beaconLight;
        public Renderer dome;
        float _t;

        void Update()
        {
            if (GameManager.Instance == null || beaconLight == null) return;
            _t += Time.deltaTime;
            if (GameManager.Instance.HasUnackedAlarms)
            {
                float k = 0.5f + 0.5f * Mathf.Sin(_t * 9f);
                beaconLight.intensity = 1.8f * k;
                transform.localEulerAngles = new Vector3(0f, _t * 260f, 0f);
                if (dome != null) dome.sharedMaterial = MaterialLibrary.Lamp(new Color(1f, 0.1f, 0.05f), 0.3f + 1.4f * k);
            }
            else
            {
                beaconLight.intensity = 0f;
                if (dome != null) dome.sharedMaterial = MaterialLibrary.Lamp(new Color(0.07f, 0.05f, 0.05f), 0.01f);
            }
        }
    }

    // =====================================================================
    // Независимый выключатель ГЦН (бит в маске насосов)
    // =====================================================================
    public class PumpSwitch : MonoBehaviour, IInteractable
    {
        public int bitIndex;              // 0..2
        public Transform lever;
        public string caption = "ГЦН";

        float _angle;
        bool _localOn;
        bool _inited;
        float _lastIssue;

        bool ConfirmedOn()
        {
            if (GameManager.Instance == null) return false;
            int mask = (int)CommandState.Read(GameManager.Instance.Latest, ControlId.Pumps);
            return (mask & (1 << bitIndex)) != 0;
        }

        void Update()
        {
            if (lever == null) return;
            bool on = ConfirmedOn();
            if (!_inited) { _localOn = on; _inited = true; }
            else if (Time.time - _lastIssue > 0.5f) _localOn = on;

            float target = _localOn ? -26f : 26f;
            _angle = Mathf.Lerp(_angle, target, Time.deltaTime * 10f);
            lever.localEulerAngles = new Vector3(_angle, 0f, 0f);
        }

        public string Hint()
        {
            return caption + " — " + (_localOn ? "ВКЛ" : "ВЫКЛ") + " (ЛКМ переключить)";
        }

        public void Click()
        {
            if (GameManager.Instance == null) return;
            int mask = (int)CommandState.Read(GameManager.Instance.Latest, ControlId.Pumps);
            mask ^= (1 << bitIndex);
            _localOn = (mask & (1 << bitIndex)) != 0;
            _lastIssue = Time.time;
            GameManager.Instance.IssueCommand(ControlId.Pumps, mask);
            SoundBank.Toggle();
        }

        public void Scroll(float delta) { }
        public void Drag(Vector2 delta) { }
    }
}
