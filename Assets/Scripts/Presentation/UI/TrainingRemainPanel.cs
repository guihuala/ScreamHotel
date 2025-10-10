using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ScreamHotel.UI
{
    public class TrainingRemainPanel : MonoBehaviour
    {
        [Header("UI Components")]
        public Canvas canvas;
        public RectTransform root;
        public TextMeshProUGUI remainText;
        
        private Camera _mainCamera;
        private Transform _targetSlot;

        void Awake()
        {
            if (!canvas) canvas = GetComponentInParent<Canvas>();
            if (!root) root = GetComponent<RectTransform>();
            _mainCamera = Camera.main;
        }

        private void Start()
        {
            Hide();
        }

        public void Show(int remainDays, Transform targetSlot)
        {
            _targetSlot = targetSlot;
            
            if (remainText != null)
            {
                remainText.text = $"{remainDays} days remain";
            }

            root.gameObject.SetActive(true);
            UpdatePosition();
        }

        void Update()
        {
            // 如果面板显示中，持续更新位置以跟随槽位
            if (root.gameObject.activeInHierarchy && _targetSlot != null)
            {
                UpdatePosition();
            }
        }

        private void UpdatePosition()
        {
            if (!canvas || _targetSlot == null) return;

            // 计算面板的世界位置（槽位位置 + 偏移）
            Vector3 worldPosition = _targetSlot.position;
            
            // 转换为屏幕坐标
            Vector3 screenPosition = _mainCamera.WorldToScreenPoint(worldPosition);
            
            // 转换为UI的局部坐标
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform, screenPosition, canvas.worldCamera, out var localPos);
            
            root.anchoredPosition = localPos;
        }

        public void Hide()
        {
            if (root) root.gameObject.SetActive(false);
            _targetSlot = null;
        }
    }
}