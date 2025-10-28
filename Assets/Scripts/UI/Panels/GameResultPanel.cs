using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScreamHotel.UI
{
    public class GameResultPanel : BasePanel
    {
        [Header("UI References")]
        public TextMeshProUGUI detailText;   // 显示游戏结局文字
        public Button backToMenuButton;      // 返回主菜单按钮
        public Image resultImage;            // 显示游戏结局图片

        // 游戏结局图片资源
        public Sprite victoryImage;          // 成功结局图片
        public Sprite moneyFailureImage;     // 未达到金钱目标失败图片
        public Sprite suspicionFailureImage; // 怀疑值突破失败图片

        // 失败结局的原因
        public enum FailureReason
        {
            None,
            InsufficientMoney,
            SuspicionTooHigh
        }

        protected override void Awake()
        {
            base.Awake();
            
            if (backToMenuButton)
                backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
        }

        /// <summary>
        /// 初始化结果面板
        /// </summary>
        public void Init(bool success, FailureReason failureReason = FailureReason.None)
        {
            if (detailText)
            {
                if (success)
                {
                    detailText.text = "Congratulations! No one found the bug!";
                }
                else
                {
                    // 根据失败原因显示不同的文字
                    switch (failureReason)
                    {
                        case FailureReason.InsufficientMoney:
                            detailText.text = "You went bankrupt due to lack of funds!";
                            break;
                        case FailureReason.SuspicionTooHigh:
                            detailText.text = "The bug was exposed!";
                            break;
                    }
                }
            }

            // 根据结局类型加载不同的图片
            if (resultImage)
            {
                if (success)
                {
                    resultImage.sprite = victoryImage;  // 成功加载胜利图片
                }
                else
                {
                    // 根据失败原因加载不同的失败图片
                    switch (failureReason)
                    {
                        case FailureReason.InsufficientMoney:
                            resultImage.sprite = moneyFailureImage;
                            break;
                        case FailureReason.SuspicionTooHigh:
                            resultImage.sprite = suspicionFailureImage;
                            break;
                    }
                }
            }
        }

        private void OnBackToMenuClicked()
        {
            UIManager.Instance.ClosePanel(panelName);
            
            SceneLoader.Instance.LoadScene(GameScene.MainMenu);  // 返回主菜单
            TimeManager.Instance?.ResumeTime();
        }
    }
}
