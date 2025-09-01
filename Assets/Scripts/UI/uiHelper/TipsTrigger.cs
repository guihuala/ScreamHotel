using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public enum TipType
    {
        Text,
    }

    [Header("Tip Settings")]
    public TipType tipType = TipType.Text;
    [TextArea]
    public string textTip;

    [Header("Behavior")]
    [Tooltip("鼠标悬停时是否让 Tip 跟随鼠标移动")]
    public bool followCursor = true;

    [Header("Delay Settings")]
    public float showDelay = 0.2f;
    public float hideDelay = 0.1f;
    
    private Coroutine showCoroutine;
    private Coroutine hideCoroutine;
    private bool isPointerOver = false;

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;
        
        // 取消之前的隐藏协程
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }
        
        // 延迟显示提示
        if (showCoroutine != null)
            StopCoroutine(showCoroutine);
            
        showCoroutine = StartCoroutine(ShowTipAfterDelay());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
        
        if (showCoroutine != null)
        {
            StopCoroutine(showCoroutine);
            showCoroutine = null;
        }
        
        // 延迟隐藏提示
        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);
            
        hideCoroutine = StartCoroutine(HideTipAfterDelay());
    }

    private IEnumerator ShowTipAfterDelay()
    {
        yield return new WaitForSecondsRealtime(showDelay);
        if (isPointerOver)
        {
            Vector3 screenPos = Input.mousePosition;

            switch (tipType)
            {
                case TipType.Text:
                    if (!string.IsNullOrEmpty(textTip))
                        TipsController.Instance.ShowTextTip(textTip, screenPos, followCursor);
                    break;
            }
        }
    }

    private IEnumerator HideTipAfterDelay()
    {
        yield return new WaitForSecondsRealtime(hideDelay);
        TipsController.Instance.HideTips();
    }

    private void OnDisable()
    {
        if (isPointerOver)
        {
            TipsController.Instance.HideTips();
            isPointerOver = false;
        }

        if (showCoroutine != null)
        {
            StopCoroutine(showCoroutine);
            showCoroutine = null;
        }
        
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }
    }
}
