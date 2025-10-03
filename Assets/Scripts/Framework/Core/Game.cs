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
        
        private Material skyboxMaterial;

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

            TimeSystem = new TimeSystem(this);
            GoToDay();
        }

        private void Start()
        {
            skyboxMaterial = RenderSettings.skybox;
        }
        
        private void Update()
        {
            TimeSystem.Update(Time.deltaTime);
            UpdateSkyboxTransition();
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
        
        // 直接从白天跳到黑夜展示的快捷方法
        public void SkipToNightShow()
        {
            if (State == GameState.Day)
            {
                // 设置时间为傍晚，触发黑夜事件
                TimeSystem.SetNormalizedTime(0.5f);
                StartNightShow();
            }
        }
        
        public void StartNightExecution()
        {
            TimeSystem.SetNormalizedTime(0.7f);
            EventBus.Raise(new GameStateChanged(State));
        }

        public void GoToDay()
        {
            State = GameState.Day;
            _dayPhaseSystem.PrepareDay(DayIndex);
            EventBus.Raise(new GameStateChanged(State));

            TimeSystem.isPaused = false;
            Debug.Log($"Enter Day {DayIndex}");
        }

        public void StartNightShow()
        {
            State = GameState.NightShow;
            EventBus.Raise(new GameStateChanged(State));
            Debug.Log("Enter Night Show phase");
        }

        public void StartNightExecute()
        {
            State = GameState.NightExecute;
            EventBus.Raise(new GameStateChanged(State));
            Debug.Log("Enter Night Execute phase");

            // 执行夜晚的相关逻辑
            ExecuteNightActions();
        }

        private void ExecuteNightActions()
        {
            Debug.Log("Performing NightExecute actions...");

            // 在此处添加 NightExecute 阶段的具体逻辑，比如结算、清理等
            var results = _executionSystem.ResolveNight();
            EventBus.Raise(new NightResolved(results));
        }

        public void StartSettlement()
        {
            State = GameState.Settlement;
            EventBus.Raise(new GameStateChanged(State));
            Debug.Log("Enter Settlement phase");
            
            // 显示结算面板
            DisplaySettlementUI();
        }

        private void DisplaySettlementUI()
        {
            Debug.Log("Displaying settlement UI.");
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