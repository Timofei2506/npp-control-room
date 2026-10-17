using System;
using UnityEngine;

namespace NPP.Audio
{
    /// <summary>
    /// Все звуки синтезируются в рантайме (шум кликов, гул, сирена) — проект не тянет аудиоассеты.
    /// </summary>
    public static class SoundBank
    {
        const int Rate = 44100;

        static AudioSource _ui;
        static AudioSource _ambient;
        static AudioSource _siren;

        static AudioClip _click, _toggle, _press, _knob, _buzzer, _clunk, _step;
        static AudioClip _ambientClip, _sirenClip;

        static float _volume = 0.8f;

        public static bool Ready { get; private set; }

        public static void Init(GameObject host)
        {
            if (Ready) return;
            Ready = true;

            _ui = host.AddComponent<AudioSource>();
            _ui.playOnAwake = false;
            _ui.spatialBlend = 0f;

            var ambGo = new GameObject("AmbientAudio");
            ambGo.transform.SetParent(host.transform, false);
            _ambient = ambGo.AddComponent<AudioSource>();
            _ambient.loop = true;
            _ambient.playOnAwake = false;
            _ambient.spatialBlend = 0f;

            var sirGo = new GameObject("SirenAudio");
            sirGo.transform.SetParent(host.transform, false);
            _siren = sirGo.AddComponent<AudioSource>();
            _siren.loop = true;
            _siren.playOnAwake = false;
            _siren.spatialBlend = 0f;

            _click = Make("click", 0.045f, t => Noise(t) * Env(t, 0.004f, 0.03f) * 0.6f + Mathf.Sin(2f * Mathf.PI * 1900f * t) * Env(t, 0.002f, 0.02f) * 0.25f);
            _toggle = Make("toggle", 0.07f, t => Noise(t) * Env(t, 0.002f, 0.02f) * 0.5f + Mathf.Sin(2f * Mathf.PI * 1100f * t) * Env(t, 0.002f, 0.05f) * 0.3f + Mathf.Sin(2f * Mathf.PI * 420f * t) * EnvOffset(t, 0.03f, 0.002f, 0.03f) * 0.4f);
            _press = Make("press", 0.16f, t => Mathf.Sin(2f * Mathf.PI * (180f + 300f * t) * t) * Env(t, 0.002f, 0.12f) * 0.5f + Noise(t) * Env(t, 0.001f, 0.03f) * 0.3f);
            _knob = Make("knob", 0.02f, t => Noise(t) * Env(t, 0.001f, 0.012f) * 0.35f + Mathf.Sin(2f * Mathf.PI * 3300f * t) * Env(t, 0.001f, 0.008f) * 0.15f);
            _buzzer = Make("buzzer", 0.35f, t => Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 330f * t)) * Env(t, 0.01f, 0.3f) * 0.22f);
            _clunk = Make("clunk", 0.22f, t => Mathf.Sin(2f * Mathf.PI * (90f - 200f * t) * t) * Env(t, 0.003f, 0.18f) * 0.7f + Noise(t) * Env(t, 0.002f, 0.05f) * 0.4f);

            // шаг по бетонному полу: низкий удар + короткий шорох подошвы
            _step = Make("step", 0.14f, t =>
                Mathf.Sin(2f * Mathf.PI * 70f * t) * Env(t, 0.004f, 0.09f) * 0.45f
              + Noise(t * 3f) * Env(t, 0.010f, 0.07f) * 0.22f);

            _ambientClip = Make("ambient", 8f, t =>
            {
                float hum = Mathf.Sin(2f * Mathf.PI * 50f * t) * 0.5f
                          + Mathf.Sin(2f * Mathf.PI * 100f * t) * 0.25f
                          + Mathf.Sin(2f * Mathf.PI * 150f * t) * 0.12f;
                float noise = Noise(t) * 0.02f;
                float slow = 0.85f + 0.15f * Mathf.Sin(2f * Mathf.PI * 0.13f * t);
                return (hum * 0.05f + noise) * slow;
            });

            _sirenClip = Make("siren", 2f, t =>
            {
                float f = 660f + 140f * Mathf.Sin(2f * Mathf.PI * 1.6f * t);
                float tone = Mathf.Sin(2f * Mathf.PI * f * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * f * 1.5f * t) * 0.2f;
                float gate = Mathf.Sin(2f * Mathf.PI * 0.5f * t) > 0f ? 1f : 0.35f;
                return tone * gate * 0.16f;
            });

            _siren.clip = _sirenClip;
            _ambient.clip = _ambientClip;
            ApplyVolume();
        }

        static AudioClip Make(string name, float dur, Func<float, float> fn)
        {
            int n = Mathf.Max(16, Mathf.RoundToInt(dur * Rate));
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                data[i] = Mathf.Clamp(fn(t), -1f, 1f);
            }
            // сглаживание концов, чтобы петли не щёлкали
            int fade = Mathf.Min(800, n / 4);
            for (int i = 0; i < fade; i++)
            {
                float k = i / (float)fade;
                data[i] *= k;
                data[n - 1 - i] *= k;
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Noise(float t)
        {
            return Mathf.PerlinNoise(t * 24000f, 0.37f) * 2f - 1f;
        }

        static float Env(float t, float a, float d)
        {
            if (t < a) return t / Mathf.Max(a, 1e-5f);
            float k = (t - a) / Mathf.Max(d, 1e-5f);
            return k >= 1f ? 0f : 1f - k;
        }

        static float EnvOffset(float t, float start, float a, float d)
        {
            if (t < start) return 0f;
            return Env(t - start, a, d);
        }

        public static float Volume
        {
            get { return _volume; }
            set
            {
                _volume = Mathf.Clamp01(value);
                ApplyVolume();
            }
        }

        static void ApplyVolume()
        {
            if (_ui != null) _ui.volume = 0.9f * _volume;
            if (_ambient != null) _ambient.volume = 0.55f * _volume;
            if (_siren != null) _siren.volume = 0.9f * _volume;
        }

        public static void Click() { OneShot(_click, 0.8f); }
        public static void Toggle() { OneShot(_toggle, 0.9f); }
        public static void Press() { OneShot(_press, 1f); }
        public static void KnobTick() { OneShot(_knob, 0.5f); }
        public static void Buzzer() { OneShot(_buzzer, 1f); }
        public static void Clunk() { OneShot(_clunk, 0.9f); }

        /// <summary>Шаг. pitch слегка меняется, чтобы шаги не звучали одинаково.</summary>
        public static void Footstep(float volume)
        {
            if (_ui == null || _step == null) return;
            _ui.pitch = 0.92f + UnityEngine.Random.value * 0.16f;
            _ui.PlayOneShot(_step, Mathf.Clamp01(volume));
            _ui.pitch = 1f;
        }

        static void OneShot(AudioClip c, float vol)
        {
            if (_ui == null || c == null) return;
            _ui.PlayOneShot(c, vol);
        }

        public static void StartAmbient()
        {
            if (_ambient != null && !_ambient.isPlaying) _ambient.Play();
        }

        public static void StopAmbient()
        {
            if (_ambient != null) _ambient.Stop();
        }

        public static void SetSiren(bool on)
        {
            if (_siren == null) return;
            if (on && !_siren.isPlaying) _siren.Play();
            else if (!on && _siren.isPlaying) _siren.Stop();
        }
    }
}
