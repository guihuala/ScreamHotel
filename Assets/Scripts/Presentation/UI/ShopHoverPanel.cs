using UnityEngine;
using UnityEngine.UI;

namespace ScreamHotel.Presentation.Shop
{
    public class ShopHoverPanel : MonoBehaviour
    {
        [Header("UI Components")]
        public Canvas canvas;
        public RectTransform root;     // 该面板的RectTransform
        public Text titleText;
        public Text priceText;

        [Header("Layout")]
        public Vector2 uiOffset = new Vector2(0f, 40f); // 相对目标UI位置的偏移（像素）

        private Camera _mainCamera;
        private Transform _target;     // 要跟随的目标

        void Awake()
        {
            if (!canvas) canvas = GetComponentInParent<Canvas>();
            if (!root)   root   = GetComponent<RectTransform>();
            _mainCamera = Camera.main;
        }

        void Start()
        {
            Hide();
        }

        public void SetContent(Domain.FearTag main, int price)
        {
            if (titleText) titleText.text = $"{main}";
            if (priceText) priceText.text = $"Price:{price}";
        }
        
        public void Show(Domain.FearTag main, int price, Transform target)
        {
            SetContent(main, price);
            _target = target;
            if (root) root.gameObject.SetActive(true);
            UpdatePosition();
        }
        
        public void Show(Domain.FearTag main, int price, Vector3 screenPos)
        {
            SetContent(main, price);
            _target = null; // 不跟随
            if (root)
            {
                if (!canvas) canvas = GetComponentInParent<Canvas>();
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvas.transform as RectTransform, screenPos, canvas.worldCamera, out var localPos);
                root.anchoredPosition = localPos + uiOffset;
                root.gameObject.SetActive(true);
            }
        }

        void Update()
        {
            if (root && root.gameObject.activeInHierarchy && _target != null)
                UpdatePosition();
        }

        private void UpdatePosition()
        {
            if (!canvas || _target == null) return;
            if (_mainCamera == null) _mainCamera = Camera.main;

            // 世界坐标 -> 屏幕坐标
            Vector3 screenPosition = _mainCamera.WorldToScreenPoint(_target.position);

            // 屏幕坐标 -> 画布局部坐标
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform, screenPosition, canvas.worldCamera, out var localPos);

            root.anchoredPosition = localPos + uiOffset;
        }

        public void Hide()
        {
            if (root) root.gameObject.SetActive(false);
            _target = null;
        }
    }
}
