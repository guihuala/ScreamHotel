using System.Linq;
using UnityEngine;
using ScreamHotel.Core;
using ScreamHotel.Data;
using ScreamHotel.Systems;
using ScreamHotel.Domain;

namespace ScreamHotel.Core
{
    public enum GameState
    {
        Boot,
        Day,
        NightShow,
        NightExecute,
        Settlement
    }

    public class Game : MonoBehaviour
    {
        [Header("Entry References")] public DataManager dataManager;

        public InitialSetupConfig initialSetup;

        public GameState State { get; private set; } = GameState.Boot;
        public int DayIndex { get; private set; } = 1;

        private AssignmentSystem _assignmentSystem;
        private NightExecutionSystem _executionSystem;
        private BuildSystem _buildSystem;
        private DayPhaseSystem _dayPhaseSystem;
        private ProgressionSystem _progressionSystem;

        public World World { get; private set; }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            if (dataManager == null) dataManager = FindObjectOfType<DataManager>();
            dataManager.Initialize();

            World = new World(dataManager.Database);
            SeedInitialWorld(World);

            _assignmentSystem = new AssignmentSystem(World);
            _executionSystem = new NightExecutionSystem(World);
            _buildSystem = new BuildSystem(World);
            _dayPhaseSystem = new DayPhaseSystem(World);
            _progressionSystem = new ProgressionSystem(World);

            GoToDay();
        }
        
        public TimeSystem TimeSystem { get; private set; }
        private Material skyboxMaterial;

        private void Start()
        {
            TimeSystem = new TimeSystem();
            
            skyboxMaterial = RenderSettings.skybox;
            
            UpdateSkyboxTransition();
        }

        private void Update()
        {
            TimeSystem.Update(Time.deltaTime);
            
            UpdateSkyboxTransition();
            
            CheckDayNightTransition();
        }
        
        public bool ShopTryReroll()
        {
            return _dayPhaseSystem != null && _dayPhaseSystem.ShopReroll(DayIndex);
        }

        public bool ShopTryBuy(int slot, out string newGhostId)
        {
            newGhostId = null;
            return _dayPhaseSystem != null && _dayPhaseSystem.ShopBuy(slot, out newGhostId);
        }

        private void UpdateSkyboxTransition()
        {
            if (skyboxMaterial != null)
            {
                float transition = CalculateSkyTransition(TimeSystem.currentTimeOfDay);
                skyboxMaterial.SetFloat("_CubemapTransition", transition);
            }
        }

        private float CalculateSkyTransition(float timeOfDay)
        {
            float transition;

            if (timeOfDay < 0.25f)
            {
                transition = 1f - (timeOfDay / 0.25f);
            }
            else if (timeOfDay < 0.5f)
            {
                transition = (timeOfDay - 0.25f) / 0.25f;
            }
            else if (timeOfDay < 0.75f)
            {
                transition = 1f - ((timeOfDay - 0.5f) / 0.25f);
            }
            else // 傍晚到午夜
            {
                transition = ((timeOfDay - 0.75f) / 0.25f);
            }
            
            transition = Mathf.SmoothStep(0, 1, transition);
            return transition;
        }
        
        private void CheckDayNightTransition()
        {
            float prev = Mathf.Repeat(
                TimeSystem.currentTimeOfDay - Time.deltaTime / TimeSystem.dayDurationInSeconds, 1f);
            float curr = TimeSystem.currentTimeOfDay;

            // 是否跨过阈值（含跨午夜）
            bool Crossed(float a, float b, float thr)
            {
                if (a <= b) return a < thr && b >= thr;
                // wrap-around: a > b 表示从接近1跳到接近0
                return (a < 1f && thr > a) || (thr <= b);
            }

            if (Crossed(prev, curr, 0.25f))
            {
                Debug.Log("[Time] DayStartedEvent @ 0.25");
                EventBus.Raise(new DayStartedEvent());
            }
            if (Crossed(prev, curr, 0.75f))
            {
                Debug.Log("[Time] NightStartedEvent @ 0.75");
                EventBus.Raise(new NightStartedEvent());
            }
        }
        
        public void GoToDay()
        {
            State = GameState.Day;
            _dayPhaseSystem.PrepareDay(DayIndex);
            EventBus.Raise(new GameStateChanged(State));
        }

        public void StartNightShow()
        {
            State = GameState.NightShow;
            EventBus.Raise(new GameStateChanged(State));
        }
        
        public void StartNightExecution(int rngSeed)
        {
            if (!TimeSystem.IsNight)
            {
                Debug.LogWarning("只能在夜晚执行!");
                return;
            }

            State = GameState.NightExecute;
            EventBus.Raise(new GameStateChanged(State));

            // 暂停时间
            TimeSystem.isPaused = true;

            // 调用无参 ResolveNight()
            var results = _executionSystem.ResolveNight();
            EventBus.Raise(new NightResolved(results));

            State = GameState.Settlement;
            EventBus.Raise(new GameStateChanged(State));

            DayIndex++;

            // 恢复时间，推进到第二天早晨（清晨 6 点）
            TimeSystem.isPaused = false;
            TimeSystem.currentTimeOfDay = 0.25f;
            GoToDay();
        }

        
        private void SeedInitialWorld(World w)
        {
            var setup = initialSetup;

            // 初始金币
            w.Economy.Gold = setup.startGold;
            
            var rules = w.Config?.Rules;
            if (rules == null)
            {
                Debug.LogWarning("[Game] Rules is null. Using safe defaults.");
            }
            int capLv1 = rules != null ? rules.capacityLv1 : 1;

            // 播种首批房间（按 LA/LB/RA/RB 顺序铺层）
            int actuallyAdded = 0;
            for (int i = 0; i < setup.startRoomCount; i++)
            {
                int floor = (i / 4) + 1;
                string[] slots = { "LA", "LB", "RA", "RB" };
                string slot = slots[i % 4];
                var id = $"Room_F{floor}_{slot}";

                bool exists = w.Rooms.Exists(r => r.Id == id);
                if (!exists)
                {
                    w.Rooms.Add(new Room
                    {
                        Id = id,
                        Level = 1,
                        Capacity = capLv1,
                        RoomTag = null
                    });
                    actuallyAdded++;
                }
            }

            // 开局赠送鬼
            if (w.Ghosts.Count == 0)
            {
                int idx = 1;
                foreach (var main in setup.starterGhostMains)
                {
                    w.Ghosts.Add(new Ghost
                    {
                        Id = $"G{idx++}",
                        Main = main,
                        State = GhostState.Idle
                    });
                }
            }

            // 同步金币 UI
            EventBus.Raise(new GoldChanged(w.Economy.Gold));
        }
    }
}