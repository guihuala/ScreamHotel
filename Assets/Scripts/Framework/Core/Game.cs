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
        
        [SerializeField] private float skyTransitionSpeed = 3f;
        
        public GameState State { get; private set; } = GameState.Boot;
        public int DayIndex { get; private set; } = 1;
        
        private Material skyboxMaterial;
        private float _skyTransition;

        private AssignmentSystem _assignmentSystem;
        private NightExecutionSystem _executionSystem;
        private BuildSystem _buildSystem;
        private DayPhaseSystem _dayPhaseSystem;
        private ProgressionSystem _progressionSystem;
        private GhostTrainer _ghostTrainer;
        
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
            _dayPhaseSystem = new DayPhaseSystem(World, dataManager.Database);
            _progressionSystem = new ProgressionSystem(World);
            
            _ghostTrainer = new GhostTrainer();
            _ghostTrainer.Initialize(World); 

            TimeSystem = new TimeSystem(this);
            GoToDay();
        }

        private void Start()
        {
            skyboxMaterial = RenderSettings.skybox;
            _skyTransition = CalculateSkyTransition(TimeSystem.currentTimeOfDay);
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
            if (skyboxMaterial == null) return;

            float target = CalculateSkyTransition(TimeSystem.currentTimeOfDay);
            _skyTransition = Mathf.MoveTowards(
                _skyTransition, 
                target, 
                skyTransitionSpeed * Time.deltaTime
            );

            skyboxMaterial.SetFloat("_CubemapTransition", Mathf.Clamp01(_skyTransition));
        }

        private float CalculateSkyTransition(float timeOfDay)
        {
            float transition = timeOfDay;

            transition = Mathf.SmoothStep(0f, 1f, transition);
            
            return Mathf.Clamp01(transition);
        }
        
        public void SkipToNightShow()
        {
            if (State == GameState.Day)
            {
                // 设置时间为傍晚，触发黑夜事件
                TimeSystem.SetNormalizedTime(0.5f);
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

            if (w.Ghosts.Count == 0)
            {
                int idx = 1;
                
                if (initialSetup.starterGhosts != null && initialSetup.starterGhosts.Count > 0)
                {
                    foreach (var cfg in initialSetup.starterGhosts)
                    {
                        if (cfg == null) continue;

                        var ghost = new Ghost
                        {
                            Id = $"G{idx++}", // 实例唯一ID
                            Main = cfg.main, // 从配置读主恐惧
                            State = GhostState.Idle
                        };

                        w.Ghosts.Add(ghost);
                    }
                }
            }

            // 同步金币 UI
            EventBus.Raise(new GoldChanged(w.Economy.Gold));
        }
    }
}