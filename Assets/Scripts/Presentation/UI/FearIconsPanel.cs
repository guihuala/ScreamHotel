using System.Collections.Generic;
using ScreamHotel.Domain;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class FearIconsPanel : MonoBehaviour
{
    [Header("Refs")]
    public Canvas canvas;
    public RectTransform root;
    public HorizontalLayoutGroup layout;
    public Image iconTemplate;

    [Header("Position")]
    public Vector3 worldOffset = new Vector3(0, 2f, 0);

    [Header("Atlas")]
    public FearIconAtlas atlas;

    Transform _target;
    Camera _cam;
    readonly List<Image> _pool = new();

    void Awake()
    {
        if (!canvas) canvas = GetComponentInParent<Canvas>();
        if (!root) root = GetComponent<RectTransform>();
        if (!layout) layout = GetComponentInChildren<HorizontalLayoutGroup>(true);
        if (!iconTemplate) iconTemplate = GetComponentInChildren<Image>(true);
        _cam = Camera.main;

        // 关键校验：模板必须在 layout 下面
        if (!layout)
        {
            Debug.LogError("[FearIconsPanel] Missing HorizontalLayoutGroup (layout).");
        }
        else if (iconTemplate && iconTemplate.transform.parent != layout.transform)
        {
            Debug.LogWarning("[FearIconsPanel] iconTemplate is NOT under layout. Reparenting it now for correct layout.");
            iconTemplate.transform.SetParent(layout.transform, false);
        }

        // 模板不参与显示
        if (iconTemplate) iconTemplate.gameObject.SetActive(false);

        Hide();
    }

    public void Show(Transform target, IReadOnlyList<FearTag> tags)
    {
        if (target == null)
        {
            Debug.LogWarning("[FearIconsPanel] Show called with null target.");
            Hide();
            return;
        }
        if (tags == null || tags.Count == 0)
        {
            // 空标签直接隐藏，避免留空壳
            Hide();
            return;
        }

        if (!layout)
        {
            Debug.LogError("[FearIconsPanel] layout is null, cannot render fear icons.");
            Hide();
            return;
        }

        if (!atlas)
        {
            Debug.LogWarning("[FearIconsPanel] atlas is NULL, icons will not render.");
        }

        _target = target;

        // 日志：确认拿到多少个标签
        #if UNITY_EDITOR
        Debug.Log($"[FearIconsPanel] Rendering {tags.Count} fear tag(s) on {target.name}");
        #endif

        EnsurePool(tags.Count);

        for (int i = 0; i < _pool.Count; i++)
        {
            var img = _pool[i];
            if (i < tags.Count)
            {
                var sp = atlas ? atlas.Get(tags[i]) : null;
                if (sp == null)
                {
                    Debug.LogWarning($"[FearIconsPanel] Sprite not found for tag {tags[i]} (atlas missing or unmapped).");
                    img.sprite = null;
                    img.enabled = false;
                    img.gameObject.SetActive(false);
                }
                else
                {
                    img.sprite = sp;
                    img.preserveAspect = true;     // 防止拉伸
                    img.raycastTarget = false;     // UI 不遮挡鼠标
                    img.enabled = true;
                    img.gameObject.SetActive(true);
                }
            }
            else
            {
                img.sprite = null;
                img.enabled = false;
                img.gameObject.SetActive(false);
            }
        }

        root.gameObject.SetActive(true);
        Place();

        // 关键：强制刷新一次布局，避免某些分辨率/缩放下不更新
        LayoutRebuilder.ForceRebuildLayoutImmediate(layout.GetComponent<RectTransform>());
    }

    public void Hide()
    {
        root.gameObject.SetActive(false);
        _target = null;
    }

    void Update()
    {
        if (root.gameObject.activeInHierarchy && _target) Place();
    }

    void Place()
    {
        var world = _target.position + worldOffset;
        var screen = _cam ? _cam.WorldToScreenPoint(world) : world;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform, screen, canvas ? canvas.worldCamera : null, out var local);
        root.anchoredPosition = local;
    }

    void EnsurePool(int n)
    {
        if (!iconTemplate)
        {
            Debug.LogError("[FearIconsPanel] iconTemplate is null.");
            return;
        }

        // 确保模板是 layout 的孩子
        if (iconTemplate.transform.parent != layout.transform)
            iconTemplate.transform.SetParent(layout.transform, false);

        // 初始化：把模板加入池，并设为隐藏
        if (_pool.Count == 0)
        {
            iconTemplate.gameObject.SetActive(false);
            _pool.Add(iconTemplate);
        }

        // 扩容：保证新克隆体也放到 layout 下面
        while (_pool.Count < n)
        {
            var clone = Instantiate(iconTemplate, layout.transform); // 明确指定 parent=layout
            clone.name = $"{iconTemplate.name}_clone_{_pool.Count}";
            clone.gameObject.SetActive(false);
            _pool.Add(clone);
        }
    }
}

