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
        
        [Header("Guests")]
        public Button guestsButton;

        [Header("HUD Text (optional)")]
        public TextMeshProUGUI goldText;
        public TextMeshProUGUI dayText;

        [Header("Analog Clock")]
        public RectTransform hourHand;
        public RectTransform minuteHand;

        [Header("Suspicion UI (Image Fill)")]
        public Image suspicionFillImage;
        public int suspicionFallbackThreshold = 100;

        private int suspicionThresholdCached = 100;

        private void Awake()
        {
            game = FindObjectOfType<Game>();

            if (pauseButton)   pauseButton.onClick.AddListener(OnPauseButtonClicked);
            if (executeButton) executeButton.onClick.AddListener(OnExecuteButtonClicked);
            if (skipDayButton) skipDayButton.onClick.AddListener(OnSkipDayButtonClicked);

            // NEW
            if (guestsButton)  guestsButton.onClick.AddListener(OnGuestsButtonClicked);

            UpdateExecuteButtonVisibility();
            UpdateSkipDayButtonVisibility();
            UpdateGuestsButtonVisibility(); // NEW

            var rules = game?.World?.Config?.Rules;
            suspicionThresholdCached = Mathf.Max(1, rules != null ? rules.suspicionThreshold : suspicionFallbackThreshold);

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
            RefreshSuspicionUI();
            UpdateTimeDisplay();
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
            UpdateTimeDisplay();
        }

        private void OnGoldChanged(GoldChanged g) => RefreshGoldUI();

        private void OnGameStateChanged(GameStateChanged e)
        {
            UpdateExecuteButtonVisibility();
            UpdateSkipDayButtonVisibility();
            UpdateGuestsButtonVisibility(); // NEW
            RefreshDayUI();
        }

        private void OnDayStarted(DayStartedEvent e)
        {
            UpdateExecuteButtonVisibility();
            UpdateSkipDayButtonVisibility();
            UpdateGuestsButtonVisibility(); // NEW
            RefreshDayUI();
            RefreshSuspicionUI();
        }

        private void OnNightStarted(NightStartedEvent e)
        {
            UpdateExecuteButtonVisibility();
            UpdateSkipDayButtonVisibility();
            UpdateGuestsButtonVisibility(); // NEW
        }

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
                UpdateGuestsButtonVisibility();
            }
        }

        private void OnSkipDayButtonClicked()
        {
            if (game.State == GameState.Day)
            {
                game.SkipToNightShow();
                UpdateSkipDayButtonVisibility();
                UpdateExecuteButtonVisibility();
                UpdateGuestsButtonVisibility();
            }
        }

        // NEW: 打开候选顾客面板
        private void OnGuestsButtonClicked()
        {
            var panel = UIManager.Instance.OpenPanel("GuestApprovalPanel");
            var gap = panel as GuestApprovalPanel;
            if (gap != null)
            {
                gap.Init(game); // 让面板自己去读 Pending/操作 Accept/Reject
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

        // NEW: 仅 Day 显示“客人名单”按钮；是否>0可根据需要限制显示
        private void UpdateGuestsButtonVisibility()
        {
            if (guestsButton == null) return;
            bool show = game.State == GameState.Day;
            guestsButton.gameObject.SetActive(show);
            guestsButton.interactable = show;
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

            float t = Mathf.Repeat(ts.currentTimeOfDay, 1f);
            float hoursFloat = t * 24f;
            int   hoursInt   = Mathf.FloorToInt(hoursFloat);
            float minutes    = (hoursFloat - hoursInt) * 60f;

            float minuteAngle = (minutes / 60f) * 360f;
            float hours12     = Mathf.Repeat(hoursFloat, 12f);
            float hourAngle   = (hours12 / 12f) * 360f + (minutes / 60f) * (360f / 12f);

            if (minuteHand != null)
                minuteHand.localRotation = Quaternion.Euler(0f, 0f, -minuteAngle);
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