using NPP.Audio;
using NPP.Net;
using NPP.Player;
using NPP.Room;
using NPP.Sim;
using NPP.UI;
using UnityEngine;

namespace NPP.Core
{
    /// <summary>
    /// Точка сборки приложения: меню → блочный щит, локальная симуляция (хост) и сетевой режим.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public enum AppState { Menu, Playing }

        public static GameManager Instance { get; private set; }

        public AppState State { get; private set; }
        public PlantSimulation Sim { get; private set; }
        public PlantSnapshot Latest { get; private set; }
        public bool HasState { get; private set; }
        public NetworkSession Session { get; private set; }
        public NetRole Role { get; private set; }
        public FirstPersonPlayer Player;
        public MainMenu Menu;
        public Hud Hud;
        public Transform RoomRoot;

        public int AckedMask { get; private set; }
        public bool IsMultiplayer { get { return Role != NetRole.Offline; } }
        public string RoomCode { get { return Session != null ? Session.RoomCode : ""; } }
        public bool HasUnackedAlarms { get { return (Latest.alarms & ~AckedMask) != 0; } }

        const float StepDt = 0.05f;
        float _simAcc;
        float _netAcc;
        bool _joining;
        NetPhase _lastPhase = NetPhase.Idle;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Application.runInBackground = true;
            Application.targetFrameRate = 120;
            QualitySettings.vSyncCount = 1;
            QualitySettings.shadowDistance = 26f;
            QualitySettings.pixelLightCount = 8;
            QualitySettings.antiAliasing = 4;

            Sim = new PlantSimulation();
            Latest = Sim.Snapshot();
            SoundBank.Init(gameObject);
            SoundBank.Volume = PlayerPrefs.GetFloat("npp.vol", 0.8f);
        }

        void Start()
        {
            State = AppState.Menu;
            Menu = MainMenu.Create(this);
        }

        // ------------------------------------------------------------------
        // Обновление
        // ------------------------------------------------------------------
        void Update()
        {
            if (Session != null)
            {
                Session.Update();
                WatchSessionPhase();
            }

            if (State == AppState.Playing)
            {
                if (Role == NetRole.Host || Role == NetRole.Offline)
                {
                    _simAcc += Time.deltaTime;
                    int guard = 0;
                    while (_simAcc >= StepDt && guard < 8)
                    {
                        Sim.Step(StepDt);
                        _simAcc -= StepDt;
                        guard++;
                    }
                    Latest = Sim.Snapshot();
                    HasState = true;

                    if (Role == NetRole.Host && Session != null && Session.Phase == NetPhase.InRoom)
                    {
                        _netAcc += Time.deltaTime;
                        if (_netAcc >= 0.1f)
                        {
                            _netAcc = 0f;
                            Session.BroadcastState(Latest);
                        }
                    }
                }

                AckedMask &= Latest.alarms;
                SoundBank.SetSiren(HasUnackedAlarms);
            }
            else
            {
                SoundBank.SetSiren(false);
            }

            if (_joining && Session != null)
            {
                if (Session.Phase == NetPhase.InRoom)
                {
                    _joining = false;
                    if (Menu != null) Menu.SetBusy(false, "");
                    EnterGame();
                }
                else if (Session.Phase == NetPhase.Failed)
                {
                    _joining = false;
                    if (Menu != null)
                    {
                        Menu.SetBusy(false, "");
                        Menu.SetStatus("Не удалось подключиться: " + Session.LastError, true);
                    }
                    DiscardSession();
                    Role = NetRole.Offline;
                }
            }
        }

        void WatchSessionPhase()
        {
            if (Session == null) return;
            NetPhase ph = Session.Phase;
            if (ph == _lastPhase) return;
            _lastPhase = ph;

            if (ph == NetPhase.InRoom)
            {
                if (Role == NetRole.Host && State == AppState.Playing)
                    Toast("Комната " + Session.RoomCode + " активна. Код для друзей: " + Session.RoomCode);
            }
            else if (ph == NetPhase.Failed && Role == NetRole.Host && State == AppState.Playing)
            {
                Toast("Сервер недоступен — продолжаем одиночную игру. " + Session.LastError);
                DiscardSession();
                Role = NetRole.Offline;
            }
        }

        // ------------------------------------------------------------------
        // Запуск режимов
        // ------------------------------------------------------------------
        public void StartSolo()
        {
            DiscardSession();
            Role = NetRole.Offline;
            AckedMask = 0;
            Sim.Reset();
            Latest = Sim.Snapshot();
            HasState = false;
            EnterGame();
        }

        public void StartHost(string playerName)
        {
            DiscardSession();
            Role = NetRole.Host;
            AckedMask = 0;
            Sim.Reset();
            Latest = Sim.Snapshot();
            HasState = false;

            Session = new NetworkSession();
            AttachSession(Session);
            Session.StartHost(NetworkSession.MakeRoomCode(), playerName);
            PlayerPrefs.SetString("npp.name", playerName);
            PlayerPrefs.Save();

            EnterGame();
            Toast("Комната " + Session.RoomCode + ". Подключение к серверу…");
        }

        public void StartJoin(string playerName, string roomCode)
        {
            if (Menu != null) Menu.SetStatus("");
            if (string.IsNullOrEmpty(roomCode) || roomCode.Length < 3)
            {
                if (Menu != null) Menu.SetStatus("Введите код комнаты (4 символа), который показал хост", true);
                return;
            }

            DiscardSession();
            Role = NetRole.Guest;
            HasState = false;
            AckedMask = 0;

            Session = new NetworkSession();
            AttachSession(Session);
            Session.StartJoin(roomCode, playerName);
            PlayerPrefs.SetString("npp.name", playerName);
            PlayerPrefs.Save();

            _joining = true;
            if (Menu != null)
            {
                Menu.SetBusy(true, "ПОДКЛЮЧЕНИЕ К КОМНАТЕ " + roomCode);
                Menu.SetStatus("");
            }
        }

        public void CancelConnect()
        {
            _joining = false;
            DiscardSession();
            Role = NetRole.Offline;
        }

        void AttachSession(NetworkSession s)
        {
            s.CommandReceived += OnRemoteCommand;
            s.StateReceived += OnRemoteState;
            s.RoomClosed += OnRoomClosed;
        }

        void DetachSession(NetworkSession s)
        {
            if (s == null) return;
            s.CommandReceived -= OnRemoteCommand;
            s.StateReceived -= OnRemoteState;
            s.RoomClosed -= OnRoomClosed;
            s.Leave();
        }

        void DiscardSession()
        {
            if (Session != null) DetachSession(Session);
            Session = null;
            _lastPhase = NetPhase.Idle;
            _joining = false;
        }

        // ------------------------------------------------------------------
        // Игровой цикл
        // ------------------------------------------------------------------
        void EnterGame()
        {
            if (RoomRoot != null) Object.Destroy(RoomRoot.gameObject);
            RoomRoot = ControlRoomBuilder.Build();

            if (Player != null) Object.Destroy(Player.gameObject);
            Player = FirstPersonPlayer.Create(new Vector3(0f, 0.15f, 4.2f), 180f);
            Player.mouseSensitivity = PlayerPrefs.GetFloat("npp.sens", 2.2f);

            if (Hud != null) Object.Destroy(Hud.gameObject);
            Hud = Hud.Create(this);

            if (Menu != null) Menu.Hide();

            State = AppState.Playing;
            SoundBank.StartAmbient();

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        public void LeaveToMenu()
        {
            DiscardSession();
            Role = NetRole.Offline;
            AckedMask = 0;
            HasState = false;

            Sim.Reset();
            Latest = Sim.Snapshot();

            if (RoomRoot != null) Object.Destroy(RoomRoot.gameObject);
            RoomRoot = null;
            if (Player != null) Object.Destroy(Player.gameObject);
            Player = null;
            if (Hud != null) Object.Destroy(Hud.gameObject);
            Hud = null;

            State = AppState.Menu;
            SoundBank.StopAmbient();
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            if (Menu != null)
            {
                Menu.Show();
                Menu.SetStatus("");
            }
        }

        // ------------------------------------------------------------------
        // Команды и сеть
        // ------------------------------------------------------------------
        public void IssueCommand(ControlId id, float value)
        {
            if (State != AppState.Playing) return;

            if (Role == NetRole.Guest)
            {
                if (Session != null) Session.SendCommand(id, value);
                return;
            }

            Sim.Apply(id, value);
            if (id == ControlId.Reset) AckedMask = 0;
        }

        void OnRemoteCommand(ControlId id, float value, string fromPeer)
        {
            if (Role != NetRole.Host || State != AppState.Playing) return;
            Sim.Apply(id, value);
            if (id == ControlId.Reset) AckedMask = 0;
        }

        void OnRemoteState(PlantSnapshot snap)
        {
            if (Role != NetRole.Guest) return;
            Latest = snap;
            HasState = true;
        }

        void OnRoomClosed(string message)
        {
            if (Role == NetRole.Guest && State == AppState.Playing)
            {
                Toast("Сессия завершена: " + message);
                LeaveToMenu();
                if (Menu != null) Menu.SetStatus("Сессия завершена: " + message, true);
            }
        }

        public void SendChat(string text)
        {
            if (Session != null && Session.Phase == NetPhase.InRoom) Session.SendChat(text);
        }

        public void Toast(string text)
        {
            if (Hud != null) Hud.Toast(text);
            else if (Menu != null) Menu.SetStatus(text);
        }

        // ------------------------------------------------------------------
        // Сигнализация
        // ------------------------------------------------------------------
        public bool IsAlarmActive(AlarmBits bit)
        {
            return (Latest.alarms & (int)bit) != 0;
        }

        public bool IsAlarmAcked(AlarmBits bit)
        {
            return (AckedMask & (int)bit) != 0;
        }

        public void Acknowledge(AlarmBits bit)
        {
            if (IsAlarmActive(bit)) AckedMask |= (int)bit;
        }

        public void AcknowledgeAll()
        {
            AckedMask |= Latest.alarms;
            SoundBank.Click();
        }
    }

    /// <summary>Автозапуск игры в любой сцене — сцена может быть пустой.</summary>
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (Object.FindObjectOfType<GameManager>() != null) return;
            var go = new GameObject("NPP_Game");
            go.AddComponent<GameManager>();
        }
    }
}
