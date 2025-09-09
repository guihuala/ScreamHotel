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
    }
}
