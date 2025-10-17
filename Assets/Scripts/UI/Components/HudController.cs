using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ScreamHotel.Core;

namespace ScreamHotel.UI
{
    public class HudController : MonoBehaviour
    {
        [Header("References")]
        private Game game;

        public Button pauseButton;
        public Button executeButton;
        public Button skipDayButton;

        [Header("HUD Text (optional)")]
        public TextMeshProUGUI goldText;
        public TextMeshProUGUI dayText;

        [Header("Analog Clock")]
        [Tooltip("时针（RectTransform，围绕其Z轴旋转）")]
        public RectTransform hourHand;
        [Tooltip("分针（RectTransform，围绕其Z轴旋转）")]
        public RectTransform minuteHand;

        [Header("Suspicion UI (Image Fill)")]
        [Tooltip("用于显示怀疑值的填充图片（Image.type = Filled）")]
        public Image suspicionFillImage;
        [Tooltip("当未从规则读取到阈值时使用的保底阈值")]
        public int suspicionFallbackThreshold = 100;

        private int suspicionThresholdCached = 100;

        private void Awake()
        {
            game = FindObjectOfType<Game>();

            if (pauseButton)   pauseButton.onClick.AddListener(OnPauseButtonClicked);
            if (executeButton) executeButton.onClick.AddListener(OnExecuteButtonClicked);
            if (skipDayButton) skipDayButton.onClick.AddListener(OnSkipDayButtonClicked);

            UpdateExecuteButtonVisibility();
            UpdateSkipDayButtonVisibility();

            // 读取阈值（有则用规则，没有就保底）
            var rules = game?.World?.Config?.Rules;
            suspicionThresholdCached = Mathf.Max(1, rules != null ? rules.suspicionThreshold : suspicionFallbackThreshold);

            // 确保怀疑值图片是 Filled 模式（运行时兜底一次）
            if (suspicionFillImage != null)
            {
                if (suspicionFillImage.type != Image.Type.Filled)
                    suspicionFillImage.type = Image.Type.Filled;
            }
        }

        private void Start()
        {
            RefreshGoldUI();
            RefreshDayUI();
            RefreshSuspicionUI(); // 初始刷新
            UpdateTimeDisplay();  // 初始时钟
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GoldChanged>(OnGoldChanged);
            EventBus.Subscribe<GameStateChanged>(OnGameStateChanged);
            EventBus.Subscribe<DayStartedEvent>(OnDayStarted);
            EventBus.Subscribe<NightStartedEvent>(OnNightStarted);
            EventBus.Subscribe<SuspicionChanged>(OnSuspicionChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GoldChanged>(OnGoldChanged);
            EventBus.Unsubscribe<GameStateChanged>(OnGameStateChanged);
            EventBus.Unsubscribe<DayStartedEvent>(OnDayStarted);
            EventBus.Unsubscribe<NightStartedEvent>(OnNightStarted);
            EventBus.Unsubscribe<SuspicionChanged>(OnSuspicionChanged);
        }

        private void Update()
        {
            UpdateTimeDisplay(); // 驱动模拟表盘
        }

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
            RefreshSuspicionUI(); // 每天开始也刷新一次
        }

        private void OnNightStarted(NightStartedEvent e)
        {
            UpdateExecuteButtonVisibility();
            UpdateSkipDayButtonVisibility();
        }

        private void OnSuspicionChanged(SuspicionChanged e)
        {
            // 若规则临时变化，兜底阈值仍保持 >=1
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
            if (goldText != null) goldText.text = $"$ {game.World.Economy.Gold}";
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
            var ts = game?.TimeSystem;
            if (ts == null) return;

            float t = Mathf.Repeat(ts.currentTimeOfDay, 1f); // 0..1
            float hoursFloat = t * 24f;                      // 0..24
            int   hoursInt   = Mathf.FloorToInt(hoursFloat); // 地板小时（数字显示用）
            float minutes    = (hoursFloat - hoursInt) * 60f;

            // 分针角度（0-360）
            float minuteAngle = (minutes / 60f) * 360f;

            // 时针角度：基于 12 小时制并包含分钟偏移
            float hours12     = Mathf.Repeat(hoursFloat, 12f);
            float hourAngle   = (hours12 / 12f) * 360f + (minutes / 60f) * (360f / 12f);

            // 应用到指针
            if (minuteHand != null)
                minuteHand.localRotation = Quaternion.Euler(0f, 0f, -minuteAngle); // UI Z 轴顺时针为负

            if (hourHand != null)
                hourHand.localRotation = Quaternion.Euler(0f, 0f, -hourAngle);
        }
        
        private void RefreshSuspicionUI()
        {
            var world = game?.World;
            if (world == null) return;

            var rules = world.Config?.Rules;
            int threshold = Mathf.Max(1, rules != null ? rules.suspicionThreshold : suspicionThresholdCached);
            int current   = Mathf.Max(0, world.Suspicion);
            float pct     = Mathf.Clamp01((float)current / threshold);

            if (suspicionFillImage != null)
            {
                if (suspicionFillImage.type != Image.Type.Filled)
                    suspicionFillImage.type = Image.Type.Filled;

                suspicionFillImage.fillAmount = pct;
            }
        }
    }
}
