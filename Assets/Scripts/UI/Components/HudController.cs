using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ScreamHotel.Core;

namespace ScreamHotel.UI
{
    public class HudController : MonoBehaviour
    {
        [Header("References")] private Game game;
        public Button pauseButton;
        public Button executeButton;
        public Button skipDayButton;
        public TextMeshProUGUI goldText;
        public TextMeshProUGUI dayText;
        public TextMeshProUGUI timeText;
        
        [Header("Suspicion UI")]
        public TextMeshProUGUI suspicionText;
        public Slider suspicionSlider;

        private int suspicionThresholdCached = 100;

        private void Awake()
        {
            game = FindObjectOfType<Game>();

            pauseButton.onClick.AddListener(OnPauseButtonClicked);
            executeButton.onClick.AddListener(OnExecuteButtonClicked);
            skipDayButton.onClick.AddListener(OnSkipDayButtonClicked);

            UpdateExecuteButtonVisibility();
            UpdateSkipDayButtonVisibility();

            // 读取阈值（有则用规则，没有就保底100）
            var rules = game?.World?.Config?.Rules;
            if (rules != null) suspicionThresholdCached = Mathf.Max(1, rules.suspicionThreshold);
        }

        private void Start()
        {
            RefreshGoldUI();
            RefreshDayUI();
            RefreshSuspicionUI(); // 初始刷新
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GoldChanged>(OnGoldChanged);
            EventBus.Subscribe<GameStateChanged>(OnGameStateChanged);
            EventBus.Subscribe<DayStartedEvent>(OnDayStarted);
            EventBus.Subscribe<NightStartedEvent>(OnNightStarted);

            // === 新增订阅：怀疑值变化 ===
            EventBus.Subscribe<SuspicionChanged>(OnSuspicionChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GoldChanged>(OnGoldChanged);
            EventBus.Unsubscribe<GameStateChanged>(OnGameStateChanged);
            EventBus.Unsubscribe<DayStartedEvent>(OnDayStarted);
            EventBus.Unsubscribe<NightStartedEvent>(OnNightStarted);

            // === 取消订阅 ===
            EventBus.Unsubscribe<SuspicionChanged>(OnSuspicionChanged);
        }

        private void Update() => UpdateTimeDisplay();

        private void OnGoldChanged(GoldChanged g) => RefreshGoldUI();

        private void OnGameStateChanged(GameStateChanged e)
        {
            UpdateExecuteButtonVisibility();
            UpdateSkipDayButtonVisibility();
            RefreshDayUI();
        }

        private void OnDayStarted(DayStartedEvent e)
        {
            UpdateExecuteButtonVisibility();
            UpdateSkipDayButtonVisibility();
            RefreshDayUI();

            // 每天开始也刷新一次（防止UI不同步）
            RefreshSuspicionUI();
        }

        private void OnNightStarted(NightStartedEvent e)
        {
            UpdateExecuteButtonVisibility();
            UpdateSkipDayButtonVisibility();
        }

        // === 新增：怀疑值事件回调 ===
        private void OnSuspicionChanged(SuspicionChanged e)
        {
            suspicionThresholdCached = Mathf.Max(1, suspicionThresholdCached);
            RefreshSuspicionUI();
        }

        private void OnPauseButtonClicked() => UIManager.Instance.OpenPanel("PausePanel");

        private void OnExecuteButtonClicked()
        {
            if (game.State == GameState.NightShow)
            {
                game.StartNightExecution();
                UpdateExecuteButtonVisibility();
                UpdateSkipDayButtonVisibility();
            }
        }

        private void OnSkipDayButtonClicked()
        {
            if (game.State == GameState.Day)
            {
                game.SkipToNightShow();
                UpdateSkipDayButtonVisibility();
                UpdateExecuteButtonVisibility();
            }
        }

        private void UpdateExecuteButtonVisibility()
        {
            if (executeButton == null) return;
            bool show = game.State == GameState.NightShow;
            executeButton.gameObject.SetActive(show);
            executeButton.interactable = show;
        }

        private void UpdateSkipDayButtonVisibility()
        {
            if (skipDayButton == null) return;
            bool show = game.State == GameState.Day;
            skipDayButton.gameObject.SetActive(show);
            skipDayButton.interactable = show;
        }

        private void RefreshGoldUI()
        {
            if (goldText != null) goldText.text = $"Gold: {game.World.Economy.Gold}";
        }

        private void RefreshDayUI()
        {
            if (dayText == null) return;
            string stateText = game.State switch
            {
                GameState.Day => "Day",
                GameState.NightShow => "Night Show",
                GameState.NightExecute => "Night Execute",
                GameState.Settlement => "Settlement",
                _ => "Preparing"
            };
            dayText.text = $"Day {game.DayIndex} - {stateText}";
        }

        private void UpdateTimeDisplay()
        {
            if (timeText == null || game.TimeSystem == null) return;
            float t = game.TimeSystem.currentTimeOfDay;
            int h = Mathf.FloorToInt(t * 24f);
            int m = Mathf.FloorToInt((t * 24f - h) * 60f);
            timeText.text = $"{h:D2}:{m:D2}";
        }

        // 新增：刷新怀疑值 UI
        private void RefreshSuspicionUI()
        {
            var world = game?.World;
            if (world == null) return;

            // 阈值优先读当前规则
            var rules = world.Config?.Rules;
            int threshold = rules != null ? Mathf.Max(1, rules.suspicionThreshold) : suspicionThresholdCached;
            int current = Mathf.Max(0, world.Suspicion);
            float pct = Mathf.Clamp01((float)current / threshold);

            if (suspicionText != null)
                suspicionText.text = $"Sus: {current} / {threshold}";

            if (suspicionSlider != null)
            {
                suspicionSlider.minValue = 0f;
                suspicionSlider.maxValue = 1f;
                suspicionSlider.value = pct;
            }
        }
    }
}
