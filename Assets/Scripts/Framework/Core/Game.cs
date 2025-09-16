using System.Linq;
using UnityEngine;
using ScreamHotel.Core;
using ScreamHotel.Data;
using ScreamHotel.Systems;
using ScreamHotel.Domain;

namespace ScreamHotel.Core
{
    public enum GameState { Boot, Day, NightShow, NightExecute, Settlement }

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
            State = GameState.NightExecute;
            EventBus.Raise(new GameStateChanged(State));

            var results = _executionSystem.ResolveNight(rngSeed);
            EventBus.Raise(new NightResolved(results));

            State = GameState.Settlement;
            EventBus.Raise(new GameStateChanged(State));

            _buildSystem.ApplySettlement(results);
            _progressionSystem.Advance(results, DayIndex);

            DayIndex++;
            GoToDay();
        }
        
        private void SeedInitialWorld(World w)
        {
            var setup = initialSetup;

            w.Economy.Gold = setup.startGold;

            var priceCfg = w.Config.RoomPrices.Count > 0 ? w.Config.RoomPrices.Values.First() : null;
            for (int i = 0; i < setup.startRoomCount; i++)
            {
                var id = $"Room_{i+1:00}";
                if (!w.Rooms.Exists(r => r.Id == id))
                    w.Rooms.Add(new Room {
                        Id = id, Level = 1,
                        Capacity = priceCfg != null ? priceCfg.capacityLv1 : 1,
                        RoomTag = null
                    });
            }
            if (setup.giveDemoLv3 && !w.Rooms.Exists(r => r.Level == 3))
                w.Rooms.Add(new Room {
                    Id = "Room_99", Level = 3,
                    Capacity = priceCfg != null ? priceCfg.capacityLv3 : 2,
                    RoomTag = FearTag.Darkness
                });

            if (w.Ghosts.Count == 0)
            {
                int idx = 1;
                foreach (var main in setup.starterGhostMains)
                {
                    w.Ghosts.Add(new Ghost {
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
