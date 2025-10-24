using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScreamHotel.UI
{
    public class GameResultPanel : BasePanel
    {
        [Header("UI References")]
        public TextMeshProUGUI detailText;
        public Button backToMenuButton;
        
        protected override void Awake()
        {
            base.Awake();
            
            if (backToMenuButton)
                backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
        }

        /// <summary>
        /// 初始化结果面板
        /// </summary>
        public void Init(bool success)
        {
            if (detailText)
            {
                string state = success ? "No one found the bug!" : "The bug was exposed";
                detailText.text = state;
            }
        }

        private void OnBackToMenuClicked()
        {
            UIManager.Instance.ClosePanel(panelName);
            TimeManager.Instance?.ResumeTime();
            
            SceneLoader.Instance.LoadScene(GameScene.MainMenu);
        }
    }
}