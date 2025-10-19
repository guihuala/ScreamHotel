using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ScreamHotel.Core;

public class GuestApprovalPanel : BasePanel
{
    [Header("Layout")]
    public Transform contentRoot;     // 垂直列表容器
    public GameObject itemTemplate;   // 隐藏的模板（包含：nameText/acceptBtn/rejectBtn）

    [Header("Actions")]
    public Button acceptAllButton;
    public Button closeButton;

    private Game _game;

    public void Init(Game game)
    {
        _game = game;
        RebuildList();
        WireButtons();
    }

    private void WireButtons()
    {
        if (acceptAllButton != null)
        {
            acceptAllButton.onClick.RemoveAllListeners();
            acceptAllButton.onClick.AddListener(() =>
            {
                var ids = _game.PendingGuests.Select(g => g.Id).ToList();
                foreach (var id in ids) _game.ApproveGuest(id);
                RebuildList();
            });
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(ClosePanel);
        }
    }

    private void ClearChildren(Transform t)
    {
        if (t == null) return;
        for (int i = t.childCount - 1; i >= 0; i--)
        {
            var c = t.GetChild(i);
            if (itemTemplate != null && c.gameObject == itemTemplate) continue; // 保留模板
            Destroy(c.gameObject);
        }
    }

    private void RebuildList()
    {
        if (contentRoot == null || itemTemplate == null || _game == null) return;

        ClearChildren(contentRoot);
        itemTemplate.SetActive(false);

        var list = _game.PendingGuests?.ToList();
        if (list == null || list.Count == 0)
        {
            // 空态：显示一个占位项
            var go = Instantiate(itemTemplate, contentRoot);
            go.SetActive(true);
            var nameText = go.GetComponentInChildren<TextMeshProUGUI>();
            if (nameText) nameText.text = "No pending guests";
            var btns = go.GetComponentsInChildren<Button>(true);
            foreach (var b in btns) b.gameObject.SetActive(false);
            return;
        }

        foreach (var g in list)
        {
            var go = Instantiate(itemTemplate, contentRoot);
            go.SetActive(true);

            var nameText = go.GetComponentInChildren<TextMeshProUGUI>();
            if (nameText) nameText.text = $"{g.Id}  (Type: {g.TypeId}, Fee: {g.BaseFee})";

            var buttons = go.GetComponentsInChildren<Button>(true);
            var acceptBtn = buttons.FirstOrDefault(b => b.name.ToLower().Contains("accept"));
            var rejectBtn = buttons.FirstOrDefault(b => b.name.ToLower().Contains("reject"));

            if (acceptBtn != null)
            {
                acceptBtn.onClick.RemoveAllListeners();
                acceptBtn.onClick.AddListener(() =>
                {
                    if (_game.ApproveGuest(g.Id))
                        Destroy(go);
                });
            }

            if (rejectBtn != null)
            {
                rejectBtn.onClick.RemoveAllListeners();
                rejectBtn.onClick.AddListener(() =>
                {
                    if (_game.RejectGuest(g.Id))
                        Destroy(go);
                });
            }
        }
    }
}
