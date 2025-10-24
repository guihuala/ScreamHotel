using UnityEngine;
using UnityEngine.UI;

namespace ScreamHotel.Presentation.Shop
{
    public class ShopRerollHoverPanel : MonoBehaviour
    {
        [Header("UI Components")]
        public Canvas canvas;
        public RectTransform root;
        public Text titleText;  // “Reroll”
        public Text costText;   // “Price: -X”

        [Header("Layout")]
        public Vector2 uiOffset = new Vector2(0f, 40f);

        private Camera _mainCamera;
        private Transform _target;

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
        
        public void Show(int cost, Transform target)
        {
            _target = target;

            if (titleText) titleText.text = "Reroll";
            if (costText)  costText.text  = $"Price:{cost}";

            if (root) root.gameObject.SetActive(true);
            UpdatePosition();
        }
        
        public void Show(int cost, Vector3 screenPos)
        {
            _target = null;

            if (titleText) titleText.text = "Reroll";
            if (costText)  costText.text  = $"Price:{cost}";

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

            Vector3 screenPosition = _mainCamera.WorldToScreenPoint(_target.position);
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
