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
        public TimeSystem TimeSystem { get; private set; }

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
            
            TimeSystem = new TimeSystem();
            // 初始化为白天状态
            GoToDay();
        }
        
        private Material skyboxMaterial;

        private void Start()
        {
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
                
                // 自动从黑夜过渡到白天（包括 Settlement 状态）
                if (State == GameState.NightShow || State == GameState.Settlement || State == GameState.NightExecute)
                {
                    GoToDay();
                }
            }
            if (Crossed(prev, curr, 0.75f))
            {
                Debug.Log("[Time] NightStartedEvent @ 0.75");
                EventBus.Raise(new NightStartedEvent());
                
                // 自动从白天过渡到黑夜展示阶段
                if (State == GameState.Day)
                {
                    StartNightShow();
                }
            }
        }
        
        public void GoToDay()
        {
            State = GameState.Day;
            _dayPhaseSystem.PrepareDay(DayIndex);
            EventBus.Raise(new GameStateChanged(State));
            
            // 确保时间系统正常运行
            TimeSystem.isPaused = false;
            Debug.Log($"Enter Day {DayIndex}");
        }

        public void StartNightShow()
        {
            State = GameState.NightShow;
            EventBus.Raise(new GameStateChanged(State));
            Debug.Log("Enter Night Show phase, waiting for player to execute");
        }
        
        // 直接从白天跳到黑夜展示的快捷方法
        public void SkipToNightShow()
        {
            if (State == GameState.Day)
            {
                // 设置时间为傍晚，触发黑夜事件
                TimeSystem.SetNormalizedTime(0.75f);
                StartNightShow();
            }
        }
        
        public void StartNightExecution(int rngSeed)
        {
            if (State != GameState.NightShow)
            {
                Debug.LogWarning("Can only execute during Night Show phase!");
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

            // 天数递增
            DayIndex++;

            Debug.Log($"Night execution completed, preparing for Day {DayIndex}");
            
            // 立即设置时间为接近早晨，让时间自然过渡到白天
            TimeSystem.SetNormalizedTime(0.24f); // 设置为早晨前一刻
            TimeSystem.isPaused = false; // 恢复时间流动
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