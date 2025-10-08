using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 每日结算面板：显示今日完成度/收入等，并在点击“继续”时回调 Game 进入下一天
/// </summary>
public class SettlementPanel : BasePanel
{
    [Header("Bind")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI completionText;
    public TextMeshProUGUI statsText;
    public Button continueButton;

    // 供 Game 传入的数据
    public struct Data
    {
        public int dayIndex;
        public int guestsTotal;
        public int guestsScared;
        public int goldChange;    // 今日净增/结算金币（可选）
        public float completion;  // 0~1
        public System.Action onContinue; // 点击继续的回调
    }

    private Data _data;

    public void Init(Data data)
    {
        _data = data;
        Refresh();
        BindEvents();
    }

    private void Refresh()
    {
        if (titleText != null)
            titleText.text = $"The {_data.dayIndex} day outcome";

        if (completionText != null)
            completionText.text = $"completion:{Mathf.RoundToInt(_data.completion * 100f)}%";

        if (statsText != null)
        {
            statsText.text =
                $"guest:{_data.guestsScared}/{_data.guestsTotal} \n" +
                $"gold erned:{_data.goldChange:+#;-#;0}";
        }
    }

    private void BindEvents()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(() =>
            {
                // 关闭自身 + 回调
                SetInteractable(false);
                _data.onContinue?.Invoke();
                UIManager.Instance.ClosePanel(nameof(SettlementPanel));
            });
        }
    }
    
    public override void OpenPanel(string name)
    {
        base.OpenPanel(name);
        SetInteractable(true);
    }
}