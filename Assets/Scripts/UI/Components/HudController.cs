using System;
using ScreamHotel.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        private void Awake()
        {
            game = FindObjectOfType<Game>();

            pauseButton.onClick.AddListener(OnPauseButtonClicked);
            executeButton.onClick.AddListener(OnExecuteButtonClicked);
            skipDayButton.onClick.AddListener(OnSkipDayButtonClicked); // 新增
            
            // 初始隐藏执行按钮
            UpdateExecuteButtonVisibility();
            UpdateSkipDayButtonVisibility();
        }

        private void Start()
        {
            RefreshGoldUI();
            RefreshDayUI();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GoldChanged>(OnGoldChanged);
            EventBus.Subscribe<GameStateChanged>(OnGameStateChanged);
            EventBus.Subscribe<DayStartedEvent>(OnDayStarted);
            EventBus.Subscribe<NightStartedEvent>(OnNightStarted);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GoldChanged>(OnGoldChanged);
            EventBus.Unsubscribe<GameStateChanged>(OnGameStateChanged);
            EventBus.Unsubscribe<DayStartedEvent>(OnDayStarted);
            EventBus.Unsubscribe<NightStartedEvent>(OnNightStarted);
        }

        private void Update()
        {
            // 更新时间显示
            UpdateTimeDisplay();
        }

        private void OnGoldChanged(GoldChanged g) => RefreshGoldUI();

        private void OnGameStateChanged(GameStateChanged e)
        {
            // 当游戏状态改变时更新按钮可见性
            UpdateExecuteButtonVisibility();
            UpdateSkipDayButtonVisibility();
            RefreshDayUI();
        }

        private void OnDayStarted(DayStartedEvent e)
        {
            UpdateExecuteButtonVisibility();
            UpdateSkipDayButtonVisibility();
            RefreshDayUI();
        }

        private void OnNightStarted(NightStartedEvent e)
        {
            UpdateExecuteButtonVisibility();
            UpdateSkipDayButtonVisibility();
        }

        private void OnPauseButtonClicked()
        {
            UIManager.Instance.OpenPanel("PausePanel");
        }

        private void OnExecuteButtonClicked()
        {
            if (game.State == GameState.NightShow)
            {
                game.StartNightExecution();
                
                // 执行后立即隐藏按钮
                UpdateExecuteButtonVisibility();
                UpdateSkipDayButtonVisibility();
            }
        }

        // 跳过白天按钮点击事件
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
            if (executeButton != null)
            {
                // 只在黑夜展示阶段显示执行按钮
                bool shouldShow = game.State == GameState.NightShow;
                executeButton.gameObject.SetActive(shouldShow);
                executeButton.interactable = shouldShow;
            }
        }
        
        private void UpdateSkipDayButtonVisibility()
        {
            if (skipDayButton != null)
            {
                // 只在白天显示跳过按钮
                bool shouldShow = game.State == GameState.Day;
                skipDayButton.gameObject.SetActive(shouldShow);
                skipDayButton.interactable = shouldShow;
            }
        }

        private void RefreshGoldUI()
        {
            if (goldText != null) goldText.text = $"Gold: {game.World.Economy.Gold}";
        }

        private void RefreshDayUI()
        {
            if (dayText != null) 
            {
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
        }

        private void UpdateTimeDisplay()
        {
            if (timeText != null && game.TimeSystem != null)
            {
                // 将时间转换为24小时制显示
                float timeOfDay = game.TimeSystem.currentTimeOfDay;
                int hours = Mathf.FloorToInt(timeOfDay * 24f);
                int minutes = Mathf.FloorToInt((timeOfDay * 24f - hours) * 60f);
                
                timeText.text = $"{hours:D2}:{minutes:D2}";
            }
        }
    }
}