using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ScreamHotel.Core;

namespace ScreamHotel.UI
{
    public class HudController : MonoBehaviour
    {
        [Header("References")]
        
        public Button tutorialButton;
        public Button pauseButton;
        public Button executeButton;
        public Button skipDayButton;

        [Header("Time Info (HUD)")]
        public Button timeInfoButton;
        public TooltipMousePanel timeInfoTooltip;

        [Header("HUD Text (optional)")]
        public TextMeshProUGUI dayText;

        [Header("Analog Clock")]
        public RectTransform hourHand;
        public RectTransform minuteHand;

        [Header("Suspicion UI (Image Fill)")]
        public Image suspicionFillImage;
        public int suspicionFallbackThreshold = 100;
        public TextMeshProUGUI suspicionText;       // “当前/阈值” 文本（可选）

        [Header("Gold UI (Image Fill)")]
        public Image goldFillImage;
        public TextMeshProUGUI goldFillText;


        private Game game;
        private int suspicionThresholdCached = 100;

        private void Awake()
        {
            game = FindObjectOfType<Game>();

            if(tutorialButton) tutorialButton.onClick.AddListener(OnTutorialButtonClicked);
            if (pauseButton)   pauseButton.onClick.AddListener(OnPauseButtonClicked);
            if (executeButton) executeButton.onClick.AddListener(OnExecuteButtonClicked);
            if (skipDayButton) skipDayButton.onClick.AddListener(OnSkipDayButtonClicked);

            // HUD 时间信息按钮：为其挂载 PointerEnter/Exit/Move
            SetupTimeInfoButtonTriggers();

            UpdateExecuteButtonVisibility();
            UpdateSkipDayButtonVisibility();

            var rules = game?.World?.Config?.Rules;
            suspicionThresholdCached = Mathf.Max(1, rules != null ? rules.suspicionThreshold : suspicionFallbackThreshold);
        }

        private void Start()
        {
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

            // 鼠标悬停时，实时让 Tooltip 跟随鼠标
            if (timeInfoTooltip != null && timeInfoTooltip.gameObject.activeInHierarchy)
            {
                timeInfoTooltip.UpdatePosition(Input.mousePosition);
            }
        }

        // ===== Event handlers =====
        private void OnGoldChanged(GoldChanged g) => RefreshGoldUI();
        
        private void OnDayStarted(DayStartedEvent e)
        {
            UpdateExecuteButtonVisibility();
            UpdateSkipDayButtonVisibility();
            RefreshDayUI();
            RefreshSuspicionUI();
            
            OpenGuestApprovalPanel();
        }

        private void OnGameStateChanged(GameStateChanged e)
        {
            UpdateExecuteButtonVisibility();
            UpdateSkipDayButtonVisibility();
            RefreshDayUI();
            
            if (game != null && game.State == GameState.Day)
                OpenGuestApprovalPanel();
        }

        private void OpenGuestApprovalPanel()
        {
            var panel = UIManager.Instance.OpenPanel("GuestApprovalPanel");
            var gap = panel as GuestApprovalPanel;
            if (gap != null)
            {
                gap.Init(game);
            }
        }

        private void OnNightStarted(NightStartedEvent e)
        {
            UpdateExecuteButtonVisibility();
            UpdateSkipDayButtonVisibility();
        }

        private void OnSuspicionChanged(SuspicionChanged e)
        {
            suspicionThresholdCached = Mathf.Max(1, suspicionThresholdCached);
            RefreshSuspicionUI();
        }

        // ===== Button callbacks =====
        private void OnTutorialButtonClicked() => UIManager.Instance.OpenPanel("GuidePanel");
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

        // ===== UI refresh =====
        
        private void RefreshGoldUI()
        {
            if (game == null) return;

            int currentGold = game.World?.Economy?.Gold ?? 0;
            
            var rules = game.World?.Config?.Rules;
            int target = Mathf.Max(0, rules != null ? rules.targetGold : 0);

            // 处理目标值为 0 的情况：不填充并展示 “当前/—”
            float fill = 0f;
            if (target > 0)
                fill = Mathf.Clamp01((float)currentGold / target);

            if (goldFillImage != null)
                goldFillImage.fillAmount = fill;

            if (goldFillText != null)
            {
                if (target > 0)
                    goldFillText.text = $"{currentGold}/{target}";
                else
                    goldFillText.text = $"{currentGold}/—";
            }
        }

        private void RefreshDayUI()
        {
            if (dayText == null || game == null) return;
            string stateText = game.State switch
            {
                GameState.Day          => "Day",
                GameState.NightShow    => "Night Show",
                GameState.NightExecute => "Night Execute",
                GameState.Settlement   => "Settlement",
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
            
            if (suspicionText != null)
                suspicionText.text = $"{current}/{threshold}";
        }
        
        // ===== TimeInfo Tooltip wiring =====
        private void SetupTimeInfoButtonTriggers()
        {
            if (timeInfoButton == null) return;

            var trigger = timeInfoButton.GetComponent<EventTrigger>();
            if (trigger == null) trigger = timeInfoButton.gameObject.AddComponent<EventTrigger>();
            trigger.triggers ??= new System.Collections.Generic.List<EventTrigger.Entry>();
            
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => ShowTimeInfoTooltip());
            trigger.triggers.Add(enter);
            
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => HideTimeInfoTooltip());
            trigger.triggers.Add(exit);
        }

        private void ShowTimeInfoTooltip()
        {
            if (timeInfoTooltip == null || game == null) return;
            string msg = CurrentPhaseText();
            timeInfoTooltip.Show(msg);
        }

        private void HideTimeInfoTooltip()
        {
            if (timeInfoTooltip == null) return;
            timeInfoTooltip.Hide();
        }

        private string CurrentPhaseText()
        {
            // 英文提示
            return game.State switch
            {
                GameState.Day          => "Daytime: manage your hotel and prepare.",
                GameState.NightShow    => "Night Show: guests arrive and fear builds.",
                GameState.NightExecute => "Night Execute: haunt and execute your plans.",
                GameState.Settlement   => "Settlement: results and rewards are tallied.",
                _ => "Preparing..."
            };
        }
    }
}
