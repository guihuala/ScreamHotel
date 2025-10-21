using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ScreamHotel.Core;
using ScreamHotel.Data;
using ScreamHotel.Domain;

public class GuestApprovalPanel : BasePanel
{
    [Header("Main Card")]
    public Image bigPortraitImage;             // 左侧“大图”
    public TextMeshProUGUI titleText;          // 顾客名字（大标题）
    public TextMeshProUGUI introText;          // 顾客简介

    [Header("Actions (Bottom)")]
    public Button acceptButton;                 // 接受
    public Button rejectButton;                 // 拒绝
    public Button closeButton;                  // 关闭

    [Header("Right Strip (Thumbnails)")]
    public Transform thumbsRoot;                // 右侧头像列表的容器（建议加 VerticalLayoutGroup）
    public GameObject thumbTemplate;            // 右侧每一项的模板（需包含：Image + Button），默认隐藏
    public Image selectedFrame;                 // 高亮框（跟随当前选择）

    [Header("Empty State")]
    public GameObject emptyState;               // 没有候选时显示的占位（可选）

    private Game _game;
    private ConfigDatabase _db;
    private readonly List<Guest> _pendingCache = new List<Guest>(); // 当前待审列表快照
    private int _cursor = 0;                    // 当前选中的顾客索引（对应右侧缩略图）

    #region Lifecycle
    public void Init(Game game)
    {
        _game = game;
        _db   = _game?.dataManager?.Database;
        Rebuild();
        WireButtons();
    }

    private void OnEnable()
    {
        // 若面板在开启后数据有变化（例如外部接受/拒绝），再次刷新
        if (_game != null) Rebuild();
    }
    #endregion

    #region Build UI
    private void Rebuild()
    {
        SnapshotPending();
        BuildThumbs();
        UpdateMainCard();
        ToggleEmptyState();
    }

    private void SnapshotPending()
    {
        _pendingCache.Clear();
        var list = _game?.PendingGuests;
        if (list != null) _pendingCache.AddRange(list);
        _cursor = Mathf.Clamp(_cursor, 0, Mathf.Max(0, _pendingCache.Count - 1));
    }

    private void BuildThumbs()
    {
        if (thumbsRoot == null || thumbTemplate == null) return;

        // 清子物体（保留模板）
        for (int i = thumbsRoot.childCount - 1; i >= 0; i--)
        {
            var child = thumbsRoot.GetChild(i);
            if (child.gameObject == thumbTemplate) continue;
            Destroy(child.gameObject);
        }

        thumbTemplate.SetActive(false);

        for (int i = 0; i < _pendingCache.Count; i++)
        {
            var g = _pendingCache[i];
            var go = Instantiate(thumbTemplate, thumbsRoot);
            go.SetActive(true);

            // 找到 Image/按钮
            var img = go.GetComponentInChildren<Image>(true);
            var btn = go.GetComponentInChildren<Button>(true);

            // 取配置(SO)
            var cfg = GetGuestConfig(g);
            var portrait = cfg?.portrait;
            if (img != null)
            {
                img.sprite = portrait;
                img.enabled = portrait != null;
            }

            if (btn != null)
            {
                int capturedIndex = i;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    _cursor = capturedIndex;
                    UpdateMainCard();
                    UpdateSelectedFrame(btn.transform as RectTransform);
                });
            }

            // 初次构建时，同步一次选中高亮
            if (i == _cursor && btn != null)
                UpdateSelectedFrame(btn.transform as RectTransform);
        }
    }

    private void UpdateMainCard()
    {
        if (_pendingCache.Count == 0)
        {
            if (titleText) titleText.text = "";
            if (introText) introText.text = "";
            if (bigPortraitImage) { bigPortraitImage.sprite = null; bigPortraitImage.enabled = false; }
            SetActionButtonsInteractable(false);
            return;
        }

        var g = _pendingCache[_cursor];
        var cfg = GetGuestConfig(g);

        // 标题：优先显示 displayName，否则 TypeId / Id
        string displayName = !string.IsNullOrEmpty(cfg?.displayName) ? cfg.displayName :
                             (!string.IsNullOrEmpty(g.TypeId) ? g.TypeId : g.Id);

        if (titleText) titleText.text = displayName;

        // 简介：SO 自定义；
        string intro = !string.IsNullOrEmpty(cfg?.intro) ? cfg.intro : "null";
        intro += $"\n\npayment:{g.BaseFee}";
        if (g.Fears != null && g.Fears.Count > 0)
        {
            var tags = string.Join("、", g.Fears.Select(t => t.ToString()));
            intro += $"\nimmunities:{tags}";
        }
        if (introText) introText.text = intro;

        // 左侧大图
        if (bigPortraitImage)
        {
            bigPortraitImage.sprite = cfg?.portrait;
            bigPortraitImage.enabled = bigPortraitImage.sprite != null;
        }

        SetActionButtonsInteractable(true);
    }

    private void ToggleEmptyState()
    {
        bool empty = _pendingCache.Count == 0;
        if (emptyState) emptyState.SetActive(empty);

        // 无数据时隐藏右侧条/按钮等
        if (thumbsRoot) thumbsRoot.gameObject.SetActive(!empty);
        if (acceptButton) acceptButton.gameObject.SetActive(!empty);
        if (rejectButton) rejectButton.gameObject.SetActive(!empty);
    }

    private void UpdateSelectedFrame(RectTransform target)
    {
        if (selectedFrame == null) return;
        if (target == null) { selectedFrame.gameObject.SetActive(false); return; }

        selectedFrame.gameObject.SetActive(true);
        selectedFrame.rectTransform.SetParent(target, worldPositionStays: false);
        selectedFrame.rectTransform.anchorMin = Vector2.zero;
        selectedFrame.rectTransform.anchorMax = Vector2.one;
        selectedFrame.rectTransform.offsetMin = Vector2.zero;
        selectedFrame.rectTransform.offsetMax = Vector2.zero;
        selectedFrame.rectTransform.SetAsLastSibling();
    }
    #endregion

    #region Buttons
    private void WireButtons()
    {
        if (acceptButton != null)
        {
            acceptButton.onClick.RemoveAllListeners();
            acceptButton.onClick.AddListener(() =>
            {
                if (_pendingCache.Count == 0) return;
                var id = _pendingCache[_cursor].Id;
                if (_game.ApproveGuest(id))
                {
                    // 保持相同索引，如越界则回退
                    int keep = _cursor;
                    Rebuild();
                    _cursor = Mathf.Clamp(keep, 0, Mathf.Max(0, _pendingCache.Count - 1));
                    UpdateMainCard();
                }
            });
        }

        if (rejectButton != null)
        {
            rejectButton.onClick.RemoveAllListeners();
            rejectButton.onClick.AddListener(() =>
            {
                if (_pendingCache.Count == 0) return;
                var id = _pendingCache[_cursor].Id;
                if (_game.RejectGuest(id))
                {
                    int keep = _cursor;
                    Rebuild();
                    _cursor = Mathf.Clamp(keep, 0, Mathf.Max(0, _pendingCache.Count - 1));
                    UpdateMainCard();
                }
            });
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(ClosePanel);
        }
    }

    private void SetActionButtonsInteractable(bool v)
    {
        if (acceptButton) acceptButton.interactable = v;
        if (rejectButton) rejectButton.interactable = v;
    }
    #endregion

    #region Helpers
    private GuestTypeConfig GetGuestConfig(Guest g)
    {
        if (_db == null || g == null || string.IsNullOrEmpty(g.TypeId)) return null;
        _db.GuestTypes.TryGetValue(g.TypeId, out var cfg);
        return cfg;
    }
    #endregion
}
