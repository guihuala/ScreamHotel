using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GuidePanel : BasePanel
{
    [Header("Paging")]
    [SerializeField] private float pageSpeed = 0.5f;     // 翻页旋转时长（秒）
    [Tooltip("按顺序分配的所有页面（允许有空/未激活项）")]
    public List<Transform> pages;                        // 书本页面（从左到右/从前到后）

    [Header("UI")]
    public Button closeButton;
    public Button nextButton;       // 下一页
    public Button previousButton;   // 上一页

    private readonly List<Transform> _orderedPages = new List<Transform>(); // 过滤后的有效页
    private int currentIndex = 0;   // 当前页索引（在 _orderedPages 上）
    private bool isRotating = false;

    private void Start()
    {
        if (closeButton)    closeButton.onClick.AddListener(() => UIManager.Instance.ClosePanel(panelName));
        if (nextButton)     nextButton.onClick.AddListener(ShowNextPage);
        if (previousButton) previousButton.onClick.AddListener(ShowPreviousPage);

        InitialState();
    }

    private void OnEnable()
    {
        TimeManager.Instance?.PauseTime();
        // 面板重新启用时，重建一次有效页并复位（方便运行时动态开关页面）
        InitialState();
    }

    private void OnDisable()
    {
        TimeManager.Instance?.ResumeTime();
    }

    /// <summary>重建有效页列表（去除 null 与未激活）</summary>
    private void RebuildPageList()
    {
        _orderedPages.Clear();
        if (pages != null)
        {
            foreach (var p in pages)
            {
                if (p != null && p.gameObject.activeInHierarchy)
                    _orderedPages.Add(p);
            }
        }
    }

    /// <summary>初始化书本状态：所有页复位，第一页置顶</summary>
    private void InitialState()
    {
        RebuildPageList();

        if (_orderedPages.Count == 0)
        {
            currentIndex = 0;
            UpdateButtons();
            return;
        }

        // 所有页面旋转归零
        for (int i = 0; i < _orderedPages.Count; i++)
        {
            var t = _orderedPages[i];
            if (t == null) continue;
            t.rotation = Quaternion.identity;
        }

        // 确保第一页在最上层
        _orderedPages[0].SetAsLastSibling();
        currentIndex = Mathf.Clamp(currentIndex, 0, _orderedPages.Count - 1);

        UpdateButtons();
    }

    /// <summary>更新翻页按钮可见性（基于“有效页数”）</summary>
    private void UpdateButtons()
    {
        int pageCount = _orderedPages.Count;

        if (previousButton) previousButton.gameObject.SetActive(pageCount > 1 && currentIndex > 0);
        if (nextButton)     nextButton.gameObject.SetActive(pageCount > 1 && currentIndex < pageCount - 1);
    }

    /// <summary>翻到下一页</summary>
    private void ShowNextPage()
    {
        if (isRotating || _orderedPages.Count == 0) return;
        if (currentIndex >= _orderedPages.Count - 1) return;

        var currentPage = _orderedPages[currentIndex];
        if (currentPage == null) return;

        StartCoroutine(RotatePage(currentPage, 180f, true));
    }

    /// <summary>翻回上一页</summary>
    private void ShowPreviousPage()
    {
        if (isRotating || _orderedPages.Count == 0) return;
        if (currentIndex <= 0) return;

        // 上一页需要被翻回到 0°
        var previousPage = _orderedPages[currentIndex - 1];
        if (previousPage == null) return;

        // 先把上一页置顶，再播放翻回动画
        previousPage.SetAsLastSibling();
        StartCoroutine(RotatePage(previousPage, 0f, false));
    }

    /// <summary>执行页面旋转动画（不受时间缩放影响）</summary>
    private IEnumerator RotatePage(Transform page, float targetAngleY, bool isForward)
    {
        isRotating = true;

        float duration = Mathf.Max(0.01f, pageSpeed);
        float t = 0f;

        Quaternion startRot = page.rotation;
        Quaternion endRot   = Quaternion.Euler(0f, targetAngleY, 0f);

        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // ★ 用非缩放时间
            float k = Mathf.Clamp01(t / duration);
            page.rotation = Quaternion.Slerp(startRot, endRot, k);
            yield return null; // 每帧驱动一次
        }

        // 强制终值
        page.rotation = endRot;

        // 更新当前页索引
        currentIndex = isForward ? currentIndex + 1 : currentIndex - 1;
        currentIndex = Mathf.Clamp(currentIndex, 0, Mathf.Max(0, _orderedPages.Count - 1));

        // 把当前页置顶
        if (_orderedPages.Count > 0)
            _orderedPages[currentIndex].SetAsLastSibling();

        // 确保当前页之前的都处于“翻过去”的状态（180°），视觉更一致
        for (int i = 0; i < _orderedPages.Count; i++)
        {
            var tform = _orderedPages[i];
            if (tform == null) continue;
            if (i < currentIndex)
                tform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }

        UpdateButtons();
        isRotating = false;
    }
}
