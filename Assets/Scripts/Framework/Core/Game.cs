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
            float previousTime = (TimeSystem.currentTimeOfDay - Time.deltaTime / TimeSystem.dayDurationInSeconds) % 1f;
            if (previousTime < 0.25f && TimeSystem.currentTimeOfDay >= 0.25f)
            {
                EventBus.Raise(new DayStartedEvent());
            }
            else if (previousTime < 0.75f && TimeSystem.currentTimeOfDay >= 0.75f)
            {
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
            // 确保在夜晚时间执行
            if (!TimeSystem.IsNight)
            {
                Debug.LogWarning("只能在夜晚执行!");
                return;
            }
    
            State = GameState.NightExecute;
            EventBus.Raise(new GameStateChanged(State));
    
            // 暂停时间
            TimeSystem.isPaused = true;
    
            var results = _executionSystem.ResolveNight(rngSeed);
            EventBus.Raise(new NightResolved(results));
    
            State = GameState.Settlement;
            EventBus.Raise(new GameStateChanged(State));
    
            _buildSystem.ApplySettlement(results);
            _progressionSystem.Advance(results, DayIndex);
    
            DayIndex++;
    
            // 恢复时间，推进到第二天早晨
            TimeSystem.isPaused = false;
            TimeSystem.currentTimeOfDay = 0.25f; // 清晨6点
            GoToDay();
        }

        private void SeedInitialWorld(World w)
        {
            var setup = initialSetup;

            w.Economy.Gold = setup.startGold;

            var priceCfg = w.Config.RoomPrices.Count > 0 ? w.Config.RoomPrices.Values.First() : null;
    
            int actuallyAdded = 0;
            for (int i = 0; i < setup.startRoomCount; i++)
            {
                int floor = (i / 4) + 1;
                string[] slots = { "LA", "LB", "RA", "RB" };
                string slot = slots[i % 4];
                var id = $"Room_F{floor}_{slot}";
        
                bool exists = w.Rooms.Exists(r => r.Id == id);
                Debug.Log($"房间 {i}: ID={id}, 是否存在={exists}");
        
                if (!exists)
                {
                    w.Rooms.Add(new Room
                    {
                        Id = id, Level = 1,
                        Capacity = priceCfg != null ? priceCfg.capacityLv1 : 1,
                        RoomTag = null
                    });
                    actuallyAdded++;
                }
            }
    
            Debug.Log($"实际添加房间数量: {actuallyAdded}");
            Debug.Log($"世界中的总房间数: {w.Rooms.Count}");

            if (w.Ghosts.Count == 0)
            {
                int idx = 1;
                foreach (var main in setup.starterGhostMains)
                {
                    w.Ghosts.Add(new Ghost
                    {
                        Id = $"G{idx++}", Main = main,
                        BaseScare = setup.defaultBaseScare,
                        Fatigue = 0f, State = GhostState.Idle
                    });
                }
            }

            EventBus.Raise(new GoldChanged(w.Economy.Gold));
        }
    }
}