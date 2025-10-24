using TMPro;
using UnityEngine;

namespace ScreamHotel.UI
{
    public class TooltipMousePanel : MonoBehaviour
    {
        [Header("Bind")]
        public Canvas canvas;                 // 放在同一画布下
        public RectTransform root;            // 自身 RectTransform
        public TextMeshProUGUI contentText;   // 显示文字

        [Header("Smart Placement")]
        [Tooltip("鼠标指针与面板之间的像素间距 (x=水平, y=垂直)")]
        public Vector2 cursorPadding = new Vector2(18f, 18f);
        [Tooltip("距离屏幕边缘的最小留白像素")]
        public float edgePadding = 12f;
        [Tooltip("是否根据边界智能翻转到指针另一侧")]
        public bool smartFlip = true;

        void Awake()
        {
            if (!canvas) canvas = GetComponentInParent<Canvas>();
            if (!root) root = GetComponent<RectTransform>();
            if (root) root.gameObject.SetActive(false);
        }

        public void Show(string msg)
        {
            if (!root) return;
            if (contentText) contentText.text = msg;
            root.gameObject.SetActive(true);
            UpdatePosition(Input.mousePosition);
        }

        public void UpdatePosition(Vector3 screenPos)
        {
            if (!root || !canvas) return;
            
            UIPosUtil.PlacePanelAtScreenPoint(
                root,               // 要摆放的面板
                canvas,             // 所在 Canvas
                screenPos,          // 鼠标屏幕坐标
                cursorPadding,      // 鼠标与面板的间距
                edgePadding,        // 屏幕边缘留白
                smartFlip
            );
        }

        public void Hide()
        {
            if (root) root.gameObject.SetActive(false);
        }
    }
}