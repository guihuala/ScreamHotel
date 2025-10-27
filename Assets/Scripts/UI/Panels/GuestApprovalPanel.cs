using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ScreamHotel.Core;
using ScreamHotel.Data;
using ScreamHotel.Domain;
using Spine.Unity;

public class GuestApprovalPanel : BasePanel
{
    [Header("Main Card")]
    public TextMeshProUGUI titleText; // 顾客名字（大标题）
    public TextMeshProUGUI introText; // 顾客简介

    [Header("Actions (Bottom)")]
    public Button acceptButton; // 接受
    public Button rejectButton; // 拒绝

    [Header("Right Strip (Thumbnails)")]
    public Transform thumbsRoot; // 右侧头像列表的容器（建议加 VerticalLayoutGroup）
    public GameObject thumbTemplate;
    public Image selectedFrame; // 高亮框（跟随当前选择）

    [Header("Main Card")]
    public RectTransform spineRoot;
    public SkeletonGraphic spineGraphic;
    public bool preferSpineOverSprite = true;
    
    [Header("Immunity (Fear) Icons")]
    public FearIconAtlas fearAtlas;
    public Transform immunitiesRoot;
    public GameObject immunityIconTemplate;
    public bool showTextFallback = true;


    private Game _game;
    private ConfigDatabase _db;
    private readonly List<Guest> _pendingCache = new List<Guest>();
    private int _cursor = 0;

    #region Lifecycle

    public void Init(Game game)
    {
        _game = game;
        _db = _game?.dataManager?.Database;
        Rebuild();
        WireButtons();

    }

    private void OnEnable()
    {
        if (_game != null) Rebuild();
        
        TimeManager.Instance?.PauseTime();
        EventBus.Subscribe<GameStateChanged>(OnGameStateChanged);
    }

    private void OnDisable()
    {
        TimeManager.Instance?.ResumeTime();
        EventBus.Unsubscribe<GameStateChanged>(OnGameStateChanged);
    }

    #endregion

    #region Build UI

    private void Rebuild()
    {
        SnapshotPending();
        BuildThumbs();
        UpdateMainCard();
        ToggleEmptyState();
        TryAutoCloseIfDone();
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
            var btn = go.GetComponentInChildren<Button>(true);
            var img = btn.GetComponent<Image>();
            
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
            
            // Spine隐藏
            SetSpineActive(false);

            SetActionButtonsInteractable(false);
            return;
        }

        var g = _pendingCache[_cursor];
        var cfg = GetGuestConfig(g);

        string displayName = !string.IsNullOrEmpty(cfg?.displayName)
            ? cfg.displayName
            : (!string.IsNullOrEmpty(g.TypeId) ? g.TypeId : g.Id);
        if (titleText) titleText.text = displayName;

        string intro = !string.IsNullOrEmpty(cfg?.intro) ? cfg.intro : "null";
        intro += $"\n\npayment:{g.BaseFee}";
        
        if (g.Immunities != null && g.Immunities.Count > 0)
        {
            BuildImmunityIcons(g);
        }
        else
        {
            ClearImmunityIcons();     // 没有免疫则清空
        }

        if (introText) introText.text = intro;
        
        TryShowSpine(cfg);
        SetActionButtonsInteractable(true);
    }
    
    private bool TryShowSpine(GuestTypeConfig cfg)
    {
        if (!preferSpineOverSprite) return false;
        if (cfg == null || cfg.spineUIData == null) return false;
        if (spineGraphic == null) return false; 
        
        spineGraphic.skeletonDataAsset = cfg.spineUIData;
        
        spineGraphic.Initialize(overwrite: true);
        
        if (!string.IsNullOrEmpty(cfg.spineDefaultSkin))
        {
            var skel = spineGraphic.Skeleton;
            if (skel != null && skel.Data.FindSkin(cfg.spineDefaultSkin) != null)
            {
                skel.SetSkin(cfg.spineDefaultSkin);
                skel.SetSlotsToSetupPose();
            }
        }
        
        var state = spineGraphic.AnimationState;
        if (state != null && !string.IsNullOrEmpty(cfg.spineDefaultAnimation))
        {
            state.SetAnimation(0, cfg.spineDefaultAnimation, cfg.spineDefaultLoop);
        }

        SetSpineActive(true);
        return true;
    }

    private void SetSpineActive(bool v)
    {
        if (spineGraphic != null) spineGraphic.gameObject.SetActive(v);
        if (spineRoot != null) spineRoot.gameObject.SetActive(v); // 可选
    }

    private void ToggleEmptyState()
    {
        bool empty = _pendingCache.Count == 0;

        // 无数据时隐藏右侧条/按钮等
        if (thumbsRoot) thumbsRoot.gameObject.SetActive(!empty);
        if (acceptButton) acceptButton.gameObject.SetActive(!empty);
        if (rejectButton) rejectButton.gameObject.SetActive(!empty);
    }

    private void UpdateSelectedFrame(RectTransform target)
    {
        if (selectedFrame == null) return;
        if (target == null)
        {
            selectedFrame.gameObject.SetActive(false);
            return;
        }

        selectedFrame.gameObject.SetActive(true);
        selectedFrame.rectTransform.SetParent(target, worldPositionStays: false);
        selectedFrame.rectTransform.anchorMin = Vector2.zero;
        selectedFrame.rectTransform.anchorMax = Vector2.one;
        selectedFrame.rectTransform.offsetMin = Vector2.zero;
        selectedFrame.rectTransform.offsetMax = Vector2.zero;
        selectedFrame.rectTransform.SetAsLastSibling();
    }

    private void OnGameStateChanged(GameStateChanged e)
    {
        if (e.State is GameState s && s != GameState.Day)
        {
            UIManager.Instance.ClosePanel(panelName);
        }
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
                    int keep = _cursor;
                    Rebuild();
                    _cursor = Mathf.Clamp(keep, 0, Mathf.Max(0, _pendingCache.Count - 1));
                    UpdateMainCard();

                    // NEW: 处理后检查是否全空 -> 自动关闭
                    TryAutoCloseIfDone();
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

                    // NEW: 处理后检查是否全空 -> 自动关闭
                    TryAutoCloseIfDone();
                }
            });
        }
    }

    private void TryAutoCloseIfDone()
    {
        if (_pendingCache.Count == 0)
        {
            // 全部处理完成 -> 自动关闭
            UIManager.Instance.ClosePanel(panelName);
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
    
    private void BuildImmunityIcons(Guest g)
    {
        if (immunitiesRoot == null || immunityIconTemplate == null)
            return;

        // 清空旧的
        ClearImmunityIcons();

        // 模板本体隐藏
        immunityIconTemplate.SetActive(false);

        // 没图集也允许继续（只是不显示图标）
        foreach (var tag in g.Immunities)
        {
            // 从图集拿图
            Sprite s = fearAtlas ? fearAtlas.Get(tag) : null;

            // 没有配置该 tag 的图标，则跳过（或走文字兜底）
            if (s == null)
                continue;

            // 实例化一个
            var go = Instantiate(immunityIconTemplate, immunitiesRoot);
            go.name = $"ImmunityIcon_{tag}";
            go.SetActive(true);

            // 取 Image 赋图
            var img = go.GetComponentInChildren<UnityEngine.UI.Image>(true);
            if (img != null)
            {
                img.sprite = s;
                img.enabled = s != null;
                // 可选：根据图标尺寸自动设置 preserveAspect
                img.preserveAspect = true;
            }
        }
    }

    private void ClearImmunityIcons()
    {
        if (immunitiesRoot == null) return;

        // 不删模板，只删除运行时生成的孩子
        for (int i = immunitiesRoot.childCount - 1; i >= 0; i--)
        {
            var child = immunitiesRoot.GetChild(i);
            if (child.gameObject == immunityIconTemplate) continue;
            Destroy(child.gameObject);
        }
    }
}