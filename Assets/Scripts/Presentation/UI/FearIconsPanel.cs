using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ScreamHotel.Domain;

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
        if (!iconTemplate) iconTemplate = GetComponentInChildren<Image>(true);
        _cam = Camera.main;
        Hide();
    }

    public void Show(Transform target, IReadOnlyList<FearTag> tags)
    {
        _target = target;
        EnsurePool(tags.Count);

        // 绑定图标
        for (int i = 0; i < _pool.Count; i++)
        {
            var img = _pool[i];
            if (i < tags.Count)
            {
                var sp = atlas ? atlas.Get(tags[i]) : null;
                img.sprite = sp;
                img.enabled = sp != null;
                img.gameObject.SetActive(true);
            }
            else
            {
                img.gameObject.SetActive(false);
            }
        }

        root.gameObject.SetActive(true);
        Place();
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
            canvas.transform as RectTransform, screen, canvas.worldCamera, out var local);
        root.anchoredPosition = local;
    }

    void EnsurePool(int n)
    {
        // 初始化模板
        if (iconTemplate && !_pool.Contains(iconTemplate))
        {
            iconTemplate.gameObject.SetActive(false);
            _pool.Add(iconTemplate);
        }

        // 扩容
        while (_pool.Count < n)
        {
            var clone = Instantiate(iconTemplate, iconTemplate.transform.parent);
            clone.gameObject.SetActive(false);
            _pool.Add(clone);
        }
    }
}
