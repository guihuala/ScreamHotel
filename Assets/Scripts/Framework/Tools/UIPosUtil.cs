using UnityEngine;

public static class UIPosUtil
{
    /// <summary>
    /// 依据屏幕坐标在 Canvas 上定位一个面板
    /// </summary>
    public static void PlacePanelAtScreenPoint(
        RectTransform panel, Canvas canvas, Vector2 screenPos,
        Vector2 cursorPadding, float edgePadding, bool smartFlip = true)
    {
        if (!panel || !canvas) return;
        var canvasRT = (RectTransform)canvas.transform;

        // 1) 屏幕→Canvas局部
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT, screenPos, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out var mouseLocal);

        var size  = panel.rect.size;
        var pivot = panel.pivot;

        // 2) 默认放在鼠标“右下角”
        Vector2 pos = new Vector2(
            mouseLocal.x + cursorPadding.x + pivot.x * size.x,
            mouseLocal.y - cursorPadding.y - (1f - pivot.y) * size.y
        );

        if (smartFlip)
        {
            Vector2 minEdge = canvasRT.rect.min + new Vector2(edgePadding, edgePadding);
            Vector2 maxEdge = canvasRT.rect.max - new Vector2(edgePadding, edgePadding);

            // 水平翻转
            Vector2 min = pos - pivot * size;
            Vector2 max = pos + (Vector2.one - pivot) * size;
            if (max.x > maxEdge.x)
                pos.x = mouseLocal.x - cursorPadding.x - (1f - pivot.x) * size.x;

            // 再次夹紧左右
            min = pos - pivot * size; max = pos + (Vector2.one - pivot) * size;
            if (min.x < minEdge.x) pos.x += (minEdge.x - min.x);
            else if (max.x > maxEdge.x) pos.x -= (max.x - maxEdge.x);

            // 垂直翻转
            min = pos - pivot * size; max = pos + (Vector2.one - pivot) * size;
            if (min.y < minEdge.y)
                pos.y = mouseLocal.y + cursorPadding.y + pivot.y * size.y;

            // 再次夹紧上下
            min = pos - pivot * size; max = pos + (Vector2.one - pivot) * size;
            if (min.y < minEdge.y) pos.y += (minEdge.y - min.y);
            else if (max.y > maxEdge.y) pos.y -= (max.y - maxEdge.y);
        }

        panel.anchoredPosition = pos;
    }
}
