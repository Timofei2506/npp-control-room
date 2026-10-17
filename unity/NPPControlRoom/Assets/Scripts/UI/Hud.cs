using System.Collections.Generic;
using System.Text;
using NPP.Core;
using NPP.Net;
using NPP.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace NPP.UI
{
    /// <summary>Игровой интерфейс: подсказки, чат, тревоги, пауза, помощь.</summary>
    public class Hud : MonoBehaviour
    {
        GameManager _gm;
        Canvas _canvas;

        Text _hint;
        Text _status;
        Text _alarmBar;
        Text _coreBanner;
        Text _zone;
        Text _chatText;
        Text _toastText;
        RectTransform _alarmPanel;
        GameObject _pause;
        GameObject _help;
        GameObject _chatPanel;
        InputField _chatInput;
        bool _chatOpen;
        bool _pauseOpen;
        bool _helpOpen;

        float _toastUntil;
        readonly Queue<string> _toasts = new Queue<string>();
        string _lastChatStamp = "";

        public static Hud Create(GameManager gm)
        {
            var go = new GameObject("HUD");
            var h = go.AddComponent<Hud>();
            h._gm = gm;
            h.Build();
            return h;
        }

        void Build()
        {
            _canvas = UiKit.MakeCanvas("HudCanvas", 40);
            _canvas.transform.SetParent(transform, false);

            // прицел
            var cross = UiKit.Panel(_canvas.transform, Vector2.zero, new Vector2(6f, 6f), new Color(1f, 1f, 1f, 0.75f), "Dot");
            cross.GetComponent<Image>().raycastTarget = false;
            AddCrossLine(new Vector2(0f, 16f), new Vector2(1.5f, 8f));
            AddCrossLine(new Vector2(0f, -16f), new Vector2(1.5f, 8f));
            AddCrossLine(new Vector2(16f, 0f), new Vector2(8f, 1.5f));
            AddCrossLine(new Vector2(-16f, 0f), new Vector2(8f, 1.5f));

            // подсказка
            _hint = UiKit.Label(_canvas.transform, "", 24, new Color(0.95f, 0.96f, 0.9f), TextAnchor.MiddleCenter,
                                new Vector2(0f, -330f), new Vector2(1400f, 36f));

            // статус слева сверху
            var st = UiKit.Panel(_canvas.transform, new Vector2(-790f, 480f), new Vector2(460f, 112f),
                                 new Color(0.02f, 0.03f, 0.035f, 0.65f), "Status");
            st.GetComponent<Image>().raycastTarget = false;
            _status = UiKit.Label(st, "", 18, new Color(0.72f, 0.8f, 0.82f), TextAnchor.UpperLeft,
                                  new Vector2(0f, 0f), new Vector2(430f, 100f));

            // плита тревог
            var ap = UiKit.Panel(_canvas.transform, new Vector2(0f, 500f), new Vector2(1240f, 46f),
                                 new Color(0.35f, 0.03f, 0.02f, 0.92f), "AlarmPanel");
            _alarmPanel = ap;
            _alarmBar = UiKit.Label(ap, "", 22, new Color(1f, 0.92f, 0.85f), TextAnchor.MiddleCenter,
                                    Vector2.zero, new Vector2(1200f, 42f));
            ap.gameObject.SetActive(false);

            // баннер повреждения активной зоны
            var cd = UiKit.Panel(_canvas.transform, new Vector2(0f, 120f), new Vector2(1000f, 84f),
                                 new Color(0.5f, 0.02f, 0.02f, 0.85f), "CoreBanner");
            _coreBanner = UiKit.Label(cd, "АКТИВНАЯ ЗОНА ПОВРЕЖДЕНА — МОДЕЛЬ ТРЕБУЕТ СБРОСА", 34,
                                      new Color(1f, 0.9f, 0.85f), TextAnchor.MiddleCenter, Vector2.zero, new Vector2(960f, 80f));
            cd.gameObject.SetActive(false);

            // чат
            _chatPanel = UiKit.Panel(_canvas.transform, new Vector2(-620f, -370f), new Vector2(680f, 210f),
                                     new Color(0.015f, 0.02f, 0.025f, 0.55f), "Chat").gameObject;
            _chatText = UiKit.Label(_chatPanel.transform, "", 18, new Color(0.8f, 0.85f, 0.85f), TextAnchor.LowerLeft,
                                    Vector2.zero, new Vector2(640f, 190f));
            _chatPanel.SetActive(false);

            var chatIn = UiKit.Panel(_canvas.transform, new Vector2(-620f, -480f), new Vector2(680f, 46f),
                                     new Color(0f, 0f, 0f, 0f), "ChatInputBg");
            _chatInput = UiKit.Input(chatIn, "Сообщение… (Enter — отправить, Esc — отмена)",
                                     Vector2.zero, new Vector2(680f, 44f), "", 20, 120);
            chatIn.gameObject.SetActive(false);
            _chatBg = chatIn.gameObject;

            // текущее помещение (слева сверху, под статусом)
            _zone = UiKit.Label(_canvas.transform, "", 17, new Color(0.62f, 0.78f, 0.66f), TextAnchor.MiddleLeft,
                                new Vector2(-790f, 410f), new Vector2(460f, 26f));
            _zone.raycastTarget = false;

            // тосты
            _toastText = UiKit.Label(_canvas.transform, "", 24, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleCenter,
                                     new Vector2(0f, 400f), new Vector2(1200f, 70f));

            BuildPause();
            BuildHelp();
        }

        GameObject _chatBg;

        void AddCrossLine(Vector2 pos, Vector2 size)
        {
            var rt = UiKit.Panel(_canvas.transform, pos, size, new Color(1f, 1f, 1f, 0.55f), "Cross");
            rt.GetComponent<Image>().raycastTarget = false;
        }

        void BuildPause()
        {
            _pause = UiKit.FullScreen(_canvas.transform, "Pause", new Color(0.01f, 0.015f, 0.02f, 0.9f)).gameObject;
            var p = UiKit.Panel(_pause.transform, Vector2.zero, new Vector2(620f, 620f),
                                new Color(0.05f, 0.065f, 0.075f, 1f), "PausePanel");
            UiKit.Label(p, "ПАУЗА", 40, new Color(0.85f, 0.92f, 0.95f), TextAnchor.MiddleCenter,
                        new Vector2(0f, 250f), new Vector2(400f, 54f));

            UiKit.Button(p, "ПРОДОЛЖИТЬ", new Vector2(0f, 170f), new Vector2(420f, 56f), () => ShowPause(false), null, 24);
            UiKit.Button(p, "СБРОС МОДЕЛИ", new Vector2(0f, 100f), new Vector2(420f, 56f), () =>
            {
                _gm.IssueCommand(ControlId.Reset, 1f);
                Toast("Модель блока сброшена в исходное состояние");
                ShowPause(false);
            }, new Color(0.16f, 0.1f, 0.08f, 1f), 24);

            UiKit.Label(p, "Чувствительность", 20, new Color(0.62f, 0.68f, 0.7f), TextAnchor.MiddleLeft,
                        new Vector2(-150f, 30f), new Vector2(260f, 28f));
            UiKit.Slider(p, new Vector2(120f, 30f), new Vector2(260f, 28f), 0.5f, 6f,
                         PlayerPrefs.GetFloat("npp.sens", 2.2f), v =>
                         {
                             PlayerPrefs.SetFloat("npp.sens", v);
                             if (_gm.Player != null) _gm.Player.mouseSensitivity = v;
                         });

            UiKit.Label(p, "Громкость", 20, new Color(0.62f, 0.68f, 0.7f), TextAnchor.MiddleLeft,
                        new Vector2(-150f, -20f), new Vector2(260f, 28f));
            UiKit.Slider(p, new Vector2(120f, -20f), new Vector2(260f, 28f), 0f, 1f,
                         NPP.Audio.SoundBank.Volume, v => NPP.Audio.SoundBank.Volume = v);

            UiKit.Button(p, "ВЫЙТИ В МЕНЮ", new Vector2(0f, -110f), new Vector2(420f, 56f), () =>
            {
                ShowPause(false);
                _gm.LeaveToMenu();
            }, new Color(0.12f, 0.1f, 0.08f, 1f), 24);
            UiKit.Button(p, "ВЫХОД ИЗ ИГРЫ", new Vector2(0f, -180f), new Vector2(420f, 56f), () => Application.Quit(), null, 24);
            UiKit.Label(p, "F1 — помощь по управлению\nEsc — закрыть", 17, new Color(0.45f, 0.5f, 0.52f),
                        TextAnchor.MiddleCenter, new Vector2(0f, -260f), new Vector2(500f, 50f));
            _pause.SetActive(false);
        }

        void BuildHelp()
        {
            _help = UiKit.FullScreen(_canvas.transform, "Help", new Color(0.01f, 0.015f, 0.02f, 0.92f)).gameObject;
            var p = UiKit.Panel(_help.transform, Vector2.zero, new Vector2(1100f, 720f),
                                new Color(0.05f, 0.065f, 0.075f, 1f), "HelpPanel");
            UiKit.Label(p, "УПРАВЛЕНИЕ", 34, new Color(0.62f, 0.85f, 0.92f), TextAnchor.MiddleCenter,
                        new Vector2(0f, 310f), new Vector2(500f, 46f));
            string help =
                "WASD — движение, Shift — бег, мышь — обзор, ПКМ — приблизить\n" +
                "ЛКМ — нажать кнопку / переключить тумблер / открыть крышку\n" +
                "Колесо мыши над верньером — изменить уставку\n" +
                "ЛКМ + движение мыши по верньеру — плавная регулировка\n\n" +
                "ПУЛЬТ РЕАКТОРА\n" +
                "  СТЕРЖНИ — глубина погружения стержней (0% — извлечены, 100% — введены)\n" +
                "  ГЦН-1..3 — главные циркуляционные насосы; менее двух — аварийная защита\n" +
                "  БРУ-А — сброс пара в атмосферу, АЗ-5 — аварийная защита (открыть крышку)\n\n" +
                "ПУЛЬТ ТУРБИНЫ\n" +
                "  КЛАПАН — подача пара на турбину, ГЕНЕРАТОР — включение в сеть\n" +
                "  ПИТАТ. НАСОСЫ / АВТО УРОВНЯ — питание парогенератора\n\n" +
                "ТАБЛО СИГНАЛИЗАЦИИ — мигает при аварии; КВИТ — квитировать (выключить сирену)\n" +
                "СБРОС МОДЕЛИ — на панели КИП под крышкой или в паузе\n\n" +
                "ЗДАНИЕ БЛОКА\n" +
                "  Через дверь за спиной — коридор, дальше РЕАКТОРНЫЙ ЗАЛ (реактор, ПГ, ГЦН)\n" +
                "  Из реакторного зала — проход в МАШИННЫЙ ЗАЛ (турбина, генератор)\n" +
                "  Двери раздвигаются сами, когда подходите\n" +
                "  В залах есть местные приборы и пульты — ими тоже можно управлять\n\n" +
                "Enter — чат · F1 — эта справка · Esc — пауза";
            UiKit.Label(p, help, 21, new Color(0.82f, 0.87f, 0.88f), TextAnchor.UpperLeft,
                        new Vector2(0f, -10f), new Vector2(1020f, 560f));
            UiKit.Button(p, "ЗАКРЫТЬ", new Vector2(0f, -305f), new Vector2(260f, 50f), () => ToggleHelp(false), null, 22);
            _help.SetActive(false);
        }

        public void SetHint(string text)
        {
            if (_hint == null) return;
            _hint.text = text ?? "";
        }

        public void Toast(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            _toasts.Enqueue(text);
        }

        public void ToggleHelp(bool? force = null)
        {
            _helpOpen = force.HasValue ? force.Value : !_helpOpen;
            if (_help != null) _help.SetActive(_helpOpen);
            if (_gm.Player != null) _gm.Player.InputLocked = _helpOpen || _pauseOpen || _chatOpen;
            RefreshCursor();
        }

        public void ShowPause(bool show)
        {
            _pauseOpen = show;
            if (_pause != null) _pause.SetActive(show);
            if (_gm.Player != null) _gm.Player.InputLocked = _helpOpen || _pauseOpen || _chatOpen;
            RefreshCursor();
        }

        public bool IsOverlayOpen { get { return _pauseOpen || _helpOpen || _chatOpen; } }

        void RefreshCursor()
        {
            bool ui = IsOverlayOpen;
            Cursor.visible = ui;
            Cursor.lockState = ui ? CursorLockMode.None : CursorLockMode.Locked;
        }

        void OpenChat()
        {
            if (!_gm.IsMultiplayer) return;
            _chatOpen = true;
            if (_chatBg != null) _chatBg.SetActive(true);
            if (_chatInput != null)
            {
                _chatInput.text = "";
                _chatInput.Select();
                _chatInput.ActivateInputField();
            }
            PlayerInputLock(true);
        }

        void CloseChat()
        {
            _chatOpen = false;
            if (_chatBg != null) _chatBg.SetActive(false);
            PlayerInputLock(true);
        }

        void PlayerInputLock(bool refresh)
        {
            if (_gm.Player != null) _gm.Player.InputLocked = _helpOpen || _pauseOpen || _chatOpen;
            if (refresh) RefreshCursor();
        }

        void Update()
        {
            if (_gm == null) return;

            // Escape / F1 / Enter
            if (Input.GetKeyDown(KeyCode.F1))
            {
                if (_chatOpen) CloseChat(); else ToggleHelp();
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_chatOpen) CloseChat();
                else if (_helpOpen) ToggleHelp(false);
                else ShowPause(!_pauseOpen);
            }

            if (Input.GetKeyDown(KeyCode.Return) && !_chatOpen && !_pauseOpen && !_helpOpen && _gm.IsMultiplayer)
            {
                OpenChat();
                return;
            }

            if (_chatOpen)
            {
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    string msg = _chatInput.text.Trim();
                    if (msg.Length > 0) _gm.SendChat(msg);
                    CloseChat();
                }
                else if (Input.GetKeyDown(KeyCode.Escape)) CloseChat();
            }

            // тосты
            if (_toastUntil < Time.unscaledTime && _toasts.Count > 0)
            {
                _toastText.text = _toasts.Dequeue();
                _toastUntil = Time.unscaledTime + 3.2f;
            }
            if (_toastUntil < Time.unscaledTime && _toastText.text != "")
                _toastText.text = "";
            if (_toasts.Count > 6) _toasts.Dequeue();

            RefreshStatus();
            RefreshAlarms();
            RefreshChat();
            RefreshZone();
        }

        /// <summary>Помещение, в котором сейчас оператор: щит → коридор → залы.</summary>
        void RefreshZone()
        {
            if (_zone == null) return;
            if (_gm.Player == null) { _zone.text = ""; return; }

            Vector3 p = _gm.Player.transform.position;
            string zone;

            if (p.z > 12f)
            {
                if (p.x > 9f) zone = "МАШИННЫЙ ЗАЛ · ТУРБОАГРЕГАТ";
                else zone = "РЕАКТОРНЫЙ ЗАЛ";
            }
            else if (p.z > 5.5f)
            {
                zone = "КОРИДОР";
            }
            else
            {
                zone = "БЛОЧНЫЙ ЩИТ УПРАВЛЕНИЯ";
            }

            _zone.text = "◉ " + zone;
        }

        void RefreshStatus()
        {
            if (_status == null) return;
            var sb = new StringBuilder(256);
            if (_gm.Role == NetRole.Offline)
            {
                sb.Append("РЕЖИМ: ОДИНОЧНАЯ ИГРА\n");
            }
            else
            {
                var s = _gm.Session;
                string role = _gm.Role == NetRole.Host ? "ХОСТ" : "ГОСТЬ";
                sb.Append("КОМНАТА: ").Append(_gm.RoomCode).Append("  (").Append(role).Append(")\n");
                if (s != null)
                {
                    sb.Append(s.Phase == NetPhase.InRoom ? "СЕТЬ: OK" : "СЕТЬ: " + s.Phase);
                    if (s.PingMs >= 0) sb.Append("  ").Append(s.PingMs).Append(" мс");
                    sb.Append("  ·  игроков: ").Append(Mathf.Max(1, s.Peers.Count)).Append('\n');
                    string srv = NetConfig.ServerUrl;
                    sb.Append(srv.Length > 46 ? srv.Substring(0, 46) + "…" : srv);
                }
            }
            _status.text = sb.ToString();
        }

        void RefreshAlarms()
        {
            PlantSnapshot snap = _gm.Latest;
            int active = snap.alarms;
            int unacked = active & ~_gm.AckedMask;

            if (active != 0)
            {
                var sb = new StringBuilder();
                sb.Append("⚠  ");
                AppendAlarms(sb, unacked != 0 ? unacked : active);
                _alarmBar.text = sb.ToString();
                float a = 0.75f + 0.25f * Mathf.Sin(Time.time * (unacked != 0 ? 6f : 2f));
                _alarmPanel.GetComponent<Image>().color = new Color(0.35f * a + 0.1f, 0.03f, 0.02f, 0.92f);
                _alarmPanel.gameObject.SetActive(true);
            }
            else _alarmPanel.gameObject.SetActive(false);

            _coreBanner.gameObject.SetActive(snap.coreDamage);
        }

        static void AppendAlarms(StringBuilder sb, int bits)
        {
            bool first = true;
            foreach (AlarmBits b in AlarmNames.Keys)
            {
                if ((bits & (int)b) == 0) continue;
                if (!first) sb.Append("  ·  ");
                sb.Append(AlarmNames.Get(b));
                first = false;
            }
        }

        void RefreshChat()
        {
            if (_chatText == null || _gm.Session == null) return;
            if (!_gm.IsMultiplayer) { _chatPanel.SetActive(false); return; }
            _chatPanel.SetActive(true);

            var msgs = _gm.Session.Chat;
            string stamp = msgs.Count + ":" + (msgs.Count > 0 ? msgs[msgs.Count - 1].text : "");
            if (stamp == _lastChatStamp) return;
            _lastChatStamp = stamp;

            var sb = new StringBuilder();
            int start = Mathf.Max(0, msgs.Count - 8);
            for (int i = start; i < msgs.Count; i++)
            {
                var m = msgs[i];
                if (m.system) sb.Append("<color=#8fb6c9>[СИСТЕМА] ").Append(m.text).Append("</color>\n");
                else sb.Append("<color=#9fd6a8>").Append(m.from).Append(":</color> ").Append(m.text).Append('\n');
            }
            _chatText.supportRichText = true;
            _chatText.text = sb.ToString();
        }
    }

    /// <summary>Человекочитаемые имена тревог.</summary>
    public static class AlarmNames
    {
        public static readonly AlarmBits[] Keys =
        {
            AlarmBits.CoreDamage, AlarmBits.Scram, AlarmBits.PowerHighHigh, AlarmBits.PowerHigh,
            AlarmBits.FuelHotHigh, AlarmBits.FuelHot, AlarmBits.PressHighHigh, AlarmBits.PressHigh,
            AlarmBits.LevelLowLow, AlarmBits.LevelLow, AlarmBits.Overspeed, AlarmBits.FlowLow,
            AlarmBits.RadHigh, AlarmBits.TurbineTrip, AlarmBits.GenOffline
        };

        public static string Get(AlarmBits b)
        {
            switch (b)
            {
                case AlarmBits.PowerHigh: return "МОЩНОСТЬ >105%";
                case AlarmBits.PowerHighHigh: return "МОЩНОСТЬ >115%";
                case AlarmBits.FuelHot: return "ТОПЛИВО >800°C";
                case AlarmBits.FuelHotHigh: return "ТОПЛИВО >1000°C";
                case AlarmBits.PressHigh: return "ДАВЛЕНИЕ >7.0 МПа";
                case AlarmBits.PressHighHigh: return "ДАВЛЕНИЕ >8.5 МПа";
                case AlarmBits.LevelLow: return "УРОВЕНЬ <30%";
                case AlarmBits.LevelLowLow: return "УРОВЕНЬ <15%";
                case AlarmBits.Overspeed: return "ОБОРОТЫ >3100";
                case AlarmBits.FlowLow: return "РАСХОД ГЦН НИЗКИЙ";
                case AlarmBits.RadHigh: return "РАДИАЦИЯ";
                case AlarmBits.Scram: return "АЗ СРАБОТАЛА";
                case AlarmBits.GenOffline: return "ГЕНЕРАТОР ОТКЛЮЧЁН";
                case AlarmBits.TurbineTrip: return "ТУРБИНА ОТКЛЮЧЕНА";
                case AlarmBits.CoreDamage: return "ПОВРЕЖДЕНИЕ АКТИВНОЙ ЗОНЫ";
                default: return b.ToString();
            }
        }
    }
}
