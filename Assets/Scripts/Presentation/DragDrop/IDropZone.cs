using UnityEngine;

public interface IDropZone
{
    // 是否允许把 id（鬼/客人）放进来
    bool CanAccept(string id, bool isGhost);

    // 投放，若成功返回用于 MoveTo 的锚点
    bool TryDrop(string id, bool isGhost, out Transform targetAnchor);

    // 悬停反馈（统一成一个接口，避免区分 Guest/Ghost 的重载）
    void ShowHoverFeedback(string id, bool isGhost);

    // 清除悬停反馈
    void ClearFeedback();
}