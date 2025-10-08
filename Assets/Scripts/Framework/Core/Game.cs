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
        
        private int _settleGuestsTotal;
        private int _settleGuestsScared;
        private int _settleGoldDelta;
        
        public TimeSystem TimeSystem { get; private set; }
        public World World { get; private set; }
        
        private void Awake()
        {
            EventBus.Subscribe<ExecNightResolved>(OnNightResolved);
            
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
        
        private void OnDestroy()
        {
            EventBus.Unsubscribe<ExecNightResolved>(OnNightResolved);
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
        
        private (float dayEnd, float showEnd, float execEnd) GetPhaseBoundaries()
        {
            var rules = World?.Config?.Rules;

            // 默认比例
            float rDay = 0.50f, rShow = 0.20f, rExec = 0.20f, rSettle = 0.10f;

            if (rules != null)
            {
                rDay   = Mathf.Max(0f, rules.dayRatio);
                rShow  = Mathf.Max(0f, rules.nightShowRatio);
                rExec  = Mathf.Max(0f, rules.nightExecuteRatio);
                rSettle= Mathf.Max(0f, rules.settlementRatio);
                float sum = rDay + rShow + rExec + rSettle;
                if (sum > 1e-4f)
                {
                    rDay   /= sum;
                    rShow  /= sum;
                    rExec  /= sum;
                    rSettle/= sum;
                }
                else
                {
                    rDay = 0.50f; rShow = 0.20f; rExec = 0.20f; rSettle = 0.10f;
                }
            }

            float dayEnd  = rDay;
            float showEnd = dayEnd + rShow;
            float execEnd = showEnd + rExec;
            return (dayEnd, showEnd, execEnd);
        }

        public void SkipToNightShow()
        {
            if (State == GameState.Day)
            {
                var (dayEnd, _, _) = GetPhaseBoundaries();
                TimeSystem.SetNormalizedTime(Mathf.Repeat(dayEnd + 0.0001f, 1f));
            }
        }
        
        public void StartNightExecution()
        {
            var (_, showEnd, _) = GetPhaseBoundaries();
            TimeSystem.SetNormalizedTime(Mathf.Repeat(showEnd + 0.0001f, 1f));
        }

        public void StartSettlement()
        {
            State = GameState.Settlement;
            EventBus.Raise(new GameStateChanged(State));
            Debug.Log("Enter Settlement phase");

            // 暂停时间流逝，等待玩家确认
            TimeSystem.isPaused = true;

            // 显示结算面板（点击后才进入下一天）
            DisplaySettlementUI();
        }

        private void DisplaySettlementUI()
        {
            Debug.Log("Displaying settlement UI.");

            // 计算完成度
            float completion = 0f;
            if (_settleGuestsTotal > 0)
                completion = (float)_settleGuestsScared / _settleGuestsTotal;

            // 打开UI
            var panel = UIManager.Instance.OpenPanel(nameof(SettlementPanel)) as SettlementPanel;
            if (panel != null)
            {
                var data = new SettlementPanel.Data
                {
                    dayIndex      = DayIndex,
                    guestsTotal   = _settleGuestsTotal,
                    guestsScared  = _settleGuestsScared,
                    goldChange    = _settleGoldDelta,
                    completion    = completion,
                    onContinue    = OnSettlementContinue
                };
                panel.Init(data);
            }
        }
        
        private void OnSettlementContinue()
        {
            TimeSystem.isPaused = false;
            GoToDay(); // 这里会自增 DayIndex
        }
        
        public void GoToDay()
        {
            bool fromSettlement = (State == GameState.Settlement);
            if (fromSettlement)
            {
                DayIndex++;
                TimeSystem.SetNormalizedTime(0.0001f);
            }

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
            _executionSystem.ResolveNight();
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
            
            EventBus.Raise(new GoldChanged(w.Economy.Gold));
        }
        
        private void OnNightResolved(ExecNightResolved r)
        {
            _settleGuestsTotal = r.GuestsTotal;
            _settleGuestsScared = r.GuestsScared;
            _settleGoldDelta = r.TotalGold;
        }
    }
}