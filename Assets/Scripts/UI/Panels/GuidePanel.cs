using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GuidePanel : BasePanel
{
    [Header("Paging")]
    [SerializeField] private float pageSpeed = 0.5f;     // 翻页旋转时长（秒）
    public List<Transform> pages;                        // 书本页面（从左到右/从前到后）

    [Header("UI")]
    public Button closeButton;
    public Button nextButton;       // 下一页
    public Button previousButton;   // 上一页

    private int currentIndex = 0;   // 当前页索引（可见页）
    private bool isRotating = false;

    private void Start()
    {
        // 绑定按钮
        if (closeButton)    closeButton.onClick.AddListener(() => UIManager.Instance.ClosePanel(panelName));
        if (nextButton)     nextButton.onClick.AddListener(ShowNextPage);
        if (previousButton) previousButton.onClick.AddListener(ShowPreviousPage);

        // 初始化书本
        InitialState();
    }

    /// <summary>初始化书本状态：所有页复位，第一页置顶</summary>
    private void InitialState()
    {
        if (pages == null || pages.Count == 0) return;

        // 所有页面旋转归零
        for (int i = 0; i < pages.Count; i++)
        {
            if (pages[i] == null) continue;
            pages[i].rotation = Quaternion.identity;
        }

        // 确保第一页在最上层
        pages[0].SetAsLastSibling();
        currentIndex = 0;

        // 更新按钮显隐
        UpdateButtons();
    }

    /// <summary>更新翻页按钮可见性</summary>
    private void UpdateButtons()
    {
        bool hasPages = pages != null && pages.Count > 0;
        if (!hasPages)
        {
            if (previousButton) previousButton.gameObject.SetActive(false);
            if (nextButton)     nextButton.gameObject.SetActive(false);
            return;
        }

        if (previousButton) previousButton.gameObject.SetActive(currentIndex > 0);
        if (nextButton)     nextButton.gameObject.SetActive(currentIndex < pages.Count - 1);
    }

    /// <summary>翻到下一页</summary>
    private void ShowNextPage()
    {
        if (isRotating || pages == null || pages.Count == 0) return;
        if (currentIndex >= pages.Count - 1) return;

        Transform currentPage = pages[currentIndex];
        if (currentPage == null) return;

        StartCoroutine(RotatePage(currentPage, 180f, true));
    }

    /// <summary>翻回上一页</summary>
    private void ShowPreviousPage()
    {
        if (isRotating || pages == null || pages.Count == 0) return;
        if (currentIndex <= 0) return;

        // 上一页需要被翻回到 0°
        Transform previousPage = pages[currentIndex - 1];
        if (previousPage == null) return;

        // 先把上一页置顶，再播放翻回动画
        previousPage.SetAsLastSibling();
        StartCoroutine(RotatePage(previousPage, 0f, false));
    }

    /// <summary>执行页面旋转动画</summary>
    private IEnumerator RotatePage(Transform page, float targetAngleY, bool isForward)
    {
        isRotating = true;

        float duration = Mathf.Max(0.01f, pageSpeed);
        float t = 0f;

        Quaternion startRot = page.rotation;
        Quaternion endRot   = Quaternion.Euler(0f, targetAngleY, 0f);

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            page.rotation = Quaternion.Slerp(startRot, endRot, k);
            yield return null;
        }

        // 强制终值
        page.rotation = endRot;

        // 更新当前页索引
        currentIndex = isForward ? currentIndex + 1 : currentIndex - 1;
        currentIndex = Mathf.Clamp(currentIndex, 0, pages.Count - 1);

        // 把当前页置顶
        pages[currentIndex].SetAsLastSibling();

        // 确保当前页之前的都处于“翻过去”的状态（180°），视觉更一致
        for (int i = 0; i < pages.Count; i++)
        {
            if (pages[i] == null) continue;
            if (i < currentIndex)
                pages[i].rotation = Quaternion.Euler(0f, 180f, 0f);
        }

        UpdateButtons();
        isRotating = false;
    }
}
