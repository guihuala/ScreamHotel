using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class TipsController : Singleton<TipsController>
{
    [Header("UI Elements")] 
    public GameObject tipTextPrefab;

    [Header("References")] 
    public Canvas uiCanvas;

    [Header("Z-Order")] 
    public bool alwaysOnTop = true;

    [Header("Animation Settings")]
    public float fadeInDuration = 0.2f;
    public float fadeOutDuration = 0.15f;
    public float scaleFrom = 0.8f;
    
    private GameObject currentTip;
    private bool _followCursor;
    private Vector2 _screenOffset;
    private Coroutine fadeCoroutine;
    private Sequence currentAnimation;

    private Camera UICamera => uiCanvas != null ? uiCanvas.worldCamera : null;

    public void ShowTextTip(string message, Vector3 screenPosition, bool followCursor = true)
    {
        SpawnAndCommonSetup(tipTextPrefab, screenPosition, followCursor);
        var txts = currentTip.GetComponentsInChildren<Text>(true);
        foreach (var txt in txts)
        {
            txt.text = message;
        }
    }
    
    private void SpawnAndCommonSetup(GameObject prefab, Vector3 screenPosition, bool followCursor)
    {
        if (!uiCanvas)
        {
            Debug.LogError("[TipsController] 请在 Inspector 中指定 uiCanvas。");
            return;
        }

        // 停止所有正在进行的动画
        if (currentAnimation != null && currentAnimation.IsActive())
        {
            currentAnimation.Kill();
        }
        
        if (fadeCoroutine != null) 
            StopCoroutine(fadeCoroutine);

        // 如果已有提示，先立即隐藏
        if (currentTip != null)
            DestroyImmediate(currentTip);

        currentTip = Instantiate(prefab, uiCanvas.transform);
        if (alwaysOnTop) currentTip.transform.SetAsLastSibling();

        _followCursor = followCursor;

        AutoAdjustOffset(screenPosition);
        UpdateTipPositionByScreen(screenPosition);

        // 使用 DOTween 替代协程动画
        FadeIn(currentTip);
    }

    private void AutoAdjustOffset(Vector3 screenPos)
    {
        if (currentTip == null) return;

        var rt = currentTip.GetComponent<RectTransform>();
        if (!rt) return;

        Vector2 size = rt.sizeDelta;
        Vector2 offset = new Vector2(size.x * 0.5f + 10f, -size.y * 0.5f - 10f);

        if (screenPos.x + size.x > Screen.width)
            offset.x = -(size.x * 0.5f + 10f);

        if (screenPos.y - size.y < 0)
            offset.y = size.y * 0.5f + 10f;

        _screenOffset = offset;
    }

    private void UpdateTipPositionByScreen(Vector3 screenPosition)
    {
        if (currentTip == null) return;

        screenPosition += (Vector3)_screenOffset;

        var rt = currentTip.GetComponent<RectTransform>();
        if (!rt) return;

        var canvasRect = uiCanvas.transform as RectTransform;
        if (!canvasRect) return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            UICamera,
            out localPoint
        );

        rt.localPosition = localPoint;

        if (alwaysOnTop)
            currentTip.transform.SetAsLastSibling();
    }

    private void LateUpdate()
    {
        if (currentTip == null) return;
        if (!_followCursor) return;

        var screenPos = (Vector3)Input.mousePosition;
        UpdateTipPositionByScreen(screenPos);
    }

    public void HideTips()
    {
        if (currentTip != null)
        {
            // 停止所有正在进行的动画
            if (currentAnimation != null && currentAnimation.IsActive())
            {
                currentAnimation.Kill();
            }
            
            if (fadeCoroutine != null) 
                StopCoroutine(fadeCoroutine);
                
            FadeOutAndDestroy(currentTip);
            currentTip = null;
        }
    }

    #region 动画

    private void FadeIn(GameObject tip)
    {
        var cg = tip.GetComponent<CanvasGroup>();
        if (!cg) cg = tip.AddComponent<CanvasGroup>();

        var rt = tip.GetComponent<RectTransform>();

        cg.alpha = 0f;
        rt.localScale = Vector3.one * scaleFrom;

        // 使用 DOTween 创建动画序列
        currentAnimation = DOTween.Sequence();
        currentAnimation.SetUpdate(UpdateType.Normal, true); // 不受时间缩放影响
        
        // 添加淡入和缩放动画
        currentAnimation.Join(cg.DOFade(1f, fadeInDuration).SetEase(Ease.OutQuad));
        currentAnimation.Join(rt.DOScale(Vector3.one, fadeInDuration).SetEase(Ease.OutBack));
        
        currentAnimation.OnComplete(() => {
            currentAnimation = null;
        });
    }

    private void FadeOutAndDestroy(GameObject tip)
    {
        var cg = tip.GetComponent<CanvasGroup>();
        if (!cg) cg = tip.AddComponent<CanvasGroup>();

        var rt = tip.GetComponent<RectTransform>();

        // 使用 DOTween 创建动画序列
        currentAnimation = DOTween.Sequence();
        currentAnimation.SetUpdate(UpdateType.Normal, true); // 不受时间缩放影响
        
        // 添加淡出和缩放动画
        currentAnimation.Join(cg.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuad));
        currentAnimation.Join(rt.DOScale(Vector3.one * scaleFrom, fadeOutDuration).SetEase(Ease.InBack));
        
        currentAnimation.OnComplete(() => {
            if (tip != null)
                Destroy(tip);
            currentAnimation = null;
        });
    }

    #endregion
}