using NPP.Core;
using NPP.Net;
using UnityEngine;
using UnityEngine.UI;

namespace NPP.UI
{
    /// <summary>Главное меню: одиночная игра, хостинг, подключение, настройки.</summary>
    public class MainMenu : MonoBehaviour
    {
        GameManager _gm;
        Canvas _canvas;
        GameObject _root;
        GameObject _settings;
        GameObject _busy;
        Text _status;
        Text _busyText;
        InputField _name;
        InputField _code;
        InputField _server;
        Slider _sens;
        Slider _vol;

        static readonly Color Accent = new Color(0.55f, 0.82f, 0.92f);
        static readonly Color Dim = new Color(0.62f, 0.68f, 0.7f);

        public static MainMenu Create(GameManager gm)
        {
            var go = new GameObject("MainMenu");
            var m = go.AddComponent<MainMenu>();
            m._gm = gm;
            m.Build();
            return m;
        }

        void Build()
        {
            _canvas = UiKit.MakeCanvas("MenuCanvas", 50);
            _canvas.transform.SetParent(transform, false);
            _root = _canvas.gameObject;

            UiKit.FullScreen(_canvas.transform, "BG", new Color(0.028f, 0.036f, 0.042f, 1f));

            // декоративные линии
            var line = UiKit.Panel(_canvas.transform, new Vector2(0f, 300f), new Vector2(1560f, 2f),
                                   new Color(0.2f, 0.42f, 0.5f, 0.9f), "Line1");
            line.GetComponent<Image>().raycastTarget = false;

            UiKit.Label(_canvas.transform, "БЛОК №4", 100, new Color(0.9f, 0.94f, 0.96f), TextAnchor.MiddleLeft,
                        new Vector2(-460f, 395f), new Vector2(1000f, 120f));
            UiKit.Label(_canvas.transform, "СИМУЛЯТОР ОПЕРАТОРА АЭС · ВВЭР-1000 · ОНЛАЙН-КООПЕРАТИВ", 26,
                        Accent, TextAnchor.MiddleLeft, new Vector2(-460f, 318f), new Vector2(1100f, 40f));

            // меню
            float bx = -470f;
            UiKit.Button(_canvas.transform, "ОДИНОЧНАЯ ИГРА", new Vector2(bx, 170f), new Vector2(400f, 64f), () => _gm.StartSolo(), null, 27);
            UiKit.Button(_canvas.transform, "СОЗДАТЬ КОМНАТУ", new Vector2(bx, 92f), new Vector2(400f, 64f), () => _gm.StartHost(PlayerName), new Color(0.09f, 0.16f, 0.14f, 0.96f), 27);
            UiKit.Button(_canvas.transform, "ПРИСОЕДИНИТЬСЯ", new Vector2(bx, 14f), new Vector2(400f, 64f), () => _gm.StartJoin(PlayerName, RoomCode), new Color(0.09f, 0.13f, 0.18f, 0.96f), 27);
            UiKit.Button(_canvas.transform, "НАСТРОЙКИ", new Vector2(bx, -64f), new Vector2(400f, 56f), OpenSettings, null, 24);
            UiKit.Button(_canvas.transform, "ВЫХОД", new Vector2(bx, -134f), new Vector2(400f, 56f), () => Application.Quit(), null, 24);

            // карточка сети
            var card = UiKit.Panel(_canvas.transform, new Vector2(440f, 120f), new Vector2(560f, 380f),
                                   new Color(0.05f, 0.07f, 0.08f, 0.92f), "NetCard");
            card.GetComponent<Image>().raycastTarget = true;
            UiKit.Label(card, "СЕТЕВАЯ ИГРА", 28, Accent, TextAnchor.MiddleCenter, new Vector2(0f, 150f), new Vector2(500f, 40f));
            UiKit.Label(card, "Имя оператора", 20, Dim, TextAnchor.MiddleLeft, new Vector2(-250f, 100f), new Vector2(240f, 28f));
            _name = UiKit.Input(card, "Оператор", new Vector2(60f, 100f), new Vector2(300f, 44f),
                                PlayerPrefs.GetString("npp.name", "Оператор"), 22, 16);
            _name.onEndEdit.AddListener(v => { PlayerName = v; });
            UiKit.Label(card, "Код комнаты", 20, Dim, TextAnchor.MiddleLeft, new Vector2(-250f, 30f), new Vector2(240f, 28f));
            _code = UiKit.Input(card, "напр. K7Q2", new Vector2(60f, 30f), new Vector2(300f, 44f), "", 22, 4);
            UiKit.Label(card, "Хост создаёт комнату и получает код.\nГости вводят код и подключаются.\nФизику считает компьютер хоста.",
                        18, Dim, TextAnchor.UpperLeft, new Vector2(0f, -60f), new Vector2(500f, 90f));
            UiKit.Label(card, "Сервер: " + NetConfig.DefaultUrl, 16, new Color(0.4f, 0.46f, 0.5f), TextAnchor.MiddleCenter,
                        new Vector2(0f, -135f), new Vector2(520f, 24f));

            // статус
            _status = UiKit.Label(_canvas.transform, "", 22, new Color(1f, 0.78f, 0.35f), TextAnchor.MiddleCenter,
                                  new Vector2(0f, -330f), new Vector2(1300f, 40f));

            UiKit.Label(_canvas.transform,
                "УЧЕБНЫЙ СИМУЛЯТОР. НЕ ЯВЛЯЕТСЯ РУКОВОДСТВОМ ПО ЭКСПЛУАТАЦИИ РЕАЛЬНЫХ ЭНЕРГОБЛОКОВ.",
                18, new Color(0.85f, 0.4f, 0.35f), TextAnchor.MiddleCenter, new Vector2(0f, -445f), new Vector2(1500f, 26f));
            UiKit.Label(_canvas.transform, "WASD — движение · мышь — обзор · ЛКМ — нажать/переключить · колесо — верньеры · F1 — помощь",
                17, new Color(0.35f, 0.42f, 0.45f), TextAnchor.MiddleCenter, new Vector2(0f, -485f), new Vector2(1500f, 24f));

            BuildSettings();
            BuildBusy();
        }

        void BuildSettings()
        {
            _settings = UiKit.FullScreen(_canvas.transform, "Settings", new Color(0.01f, 0.015f, 0.02f, 0.94f)).gameObject;
            var p = UiKit.Panel(_settings.transform, Vector2.zero, new Vector2(780f, 560f),
                                new Color(0.05f, 0.065f, 0.075f, 1f), "SettingsPanel");
            UiKit.Label(p, "НАСТРОЙКИ", 32, Accent, TextAnchor.MiddleCenter, new Vector2(0f, 220f), new Vector2(400f, 44f));

            UiKit.Label(p, "Чувствительность мыши", 22, Dim, TextAnchor.MiddleLeft, new Vector2(-220f, 130f), new Vector2(320f, 30f));
            _sens = UiKit.Slider(p, new Vector2(160f, 130f), new Vector2(320f, 30f), 0.5f, 6f,
                                 PlayerPrefs.GetFloat("npp.sens", 2.2f), v =>
                                 {
                                     PlayerPrefs.SetFloat("npp.sens", v);
                                     PlayerPrefs.Save();
                                     if (_gm.Player != null) _gm.Player.mouseSensitivity = v;
                                 });

            UiKit.Label(p, "Громкость", 22, Dim, TextAnchor.MiddleLeft, new Vector2(-220f, 70f), new Vector2(320f, 30f));
            _vol = UiKit.Slider(p, new Vector2(160f, 70f), new Vector2(320f, 30f), 0f, 1f,
                                NPP.Audio.SoundBank.Volume, v =>
                                {
                                    PlayerPrefs.SetFloat("npp.vol", v);
                                    PlayerPrefs.Save();
                                    NPP.Audio.SoundBank.Volume = v;
                                });

            UiKit.Label(p, "Адрес сервера (WebSocket)", 22, Dim, TextAnchor.MiddleLeft, new Vector2(-220f, 0f), new Vector2(320f, 30f));
            _server = UiKit.Input(p, "ws://...", new Vector2(80f, 0f), new Vector2(420f, 44f), NetConfig.ServerUrl, 18, 120);
            UiKit.Label(p, "Оставьте пустым для сервера по умолчанию:\n" + NetConfig.DefaultUrl,
                        15, new Color(0.4f, 0.46f, 0.5f), TextAnchor.MiddleCenter, new Vector2(0f, -50f), new Vector2(700f, 50f));

            UiKit.Button(p, "СОХРАНИТЬ И ЗАКРЫТЬ", new Vector2(0f, -160f), new Vector2(340f, 52f), () =>
            {
                string url = _server.text.Trim();
                if (!string.IsNullOrEmpty(url) && !url.StartsWith("ws://") && !url.StartsWith("wss://"))
                    url = "ws://" + url;
                NetConfig.ServerUrl = url;
                CloseSettings();
            }, new Color(0.08f, 0.18f, 0.14f, 1f), 22);

            _settings.SetActive(false);
        }

        void BuildBusy()
        {
            _busy = UiKit.FullScreen(_canvas.transform, "Busy", new Color(0.01f, 0.015f, 0.02f, 0.96f)).gameObject;
            _busyText = UiKit.Label(_busy.transform, "ПОДКЛЮЧЕНИЕ…", 34, Accent, TextAnchor.MiddleCenter,
                                    new Vector2(0f, 40f), new Vector2(1000f, 60f));
            UiKit.Label(_busy.transform, "Это может занять несколько секунд", 20, Dim, TextAnchor.MiddleCenter,
                        new Vector2(0f, -10f), new Vector2(600f, 30f));
            UiKit.Button(_busy.transform, "ОТМЕНА", new Vector2(0f, -110f), new Vector2(260f, 52f), () =>
            {
                _gm.CancelConnect();
                SetBusy(false, "");
            }, null, 22);
            _busy.SetActive(false);
        }

        public string PlayerName
        {
            get { return string.IsNullOrEmpty(_name.text) ? "Оператор" : _name.text.Trim(); }
            set { if (_name != null) _name.text = value; }
        }

        public string RoomCode
        {
            get { return _code == null ? "" : _code.text.Trim().ToUpperInvariant(); }
        }

        public void Show()
        {
            if (_root != null) _root.SetActive(true);
            if (_busy != null) _busy.SetActive(false);
            if (_settings != null) _settings.SetActive(false);
        }

        public void Hide()
        {
            if (_root != null) _root.SetActive(false);
        }

        public void SetStatus(string text, bool error = false)
        {
            if (_status == null) return;
            _status.text = text;
            _status.color = error ? new Color(1f, 0.45f, 0.35f) : new Color(1f, 0.78f, 0.35f);
        }

        public void SetBusy(bool busy, string text)
        {
            if (_busy == null) return;
            _busy.SetActive(busy);
            if (busy && _busyText != null) _busyText.text = text;
        }

        void OpenSettings()
        {
            if (_settings == null) return;
            _server.text = PlayerPrefs.GetString("npp.server.url", "");
            _settings.SetActive(true);
        }

        void CloseSettings()
        {
            _settings.SetActive(false);
        }
    }
}
