using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ScreamHotel.Domain;

namespace ScreamHotel.UI
{
    public class PickFearPanel : MonoBehaviour
    {
        [Header("UI")]
        public Canvas canvas;
        public RectTransform root;
    
        [Tooltip("按钮容器（Vertical/Horizontal/Grid 均可）")]
        public Transform buttonContainer;
    
        [Header("Atlas & Button")]
        [Tooltip("Tag→Sprite 的映射表；和 FearIconsPanel 用法一致")]
        public FearIconAtlas fearIconAtlas;
    
        [Tooltip("按钮的边长（仅当容器没有自动布局时有用）")]
        public float buttonSize = 80f;
    
        [Tooltip("按钮内是否附带小文字标签（找不到图标时会强制显示文字）")]
        public bool showLabel = true;
    
        [Header("Panel Position")]
        [Tooltip("面板相对目标的世界偏移")]
        public Vector3 panelWorldOffset = new Vector3(0f, 2f, 0f);
    
        private Action<string, FearTag, int> _onPick;
        private string _ghostId;
        private int _slotIndex;
    
        // 锚定目标（例如 RoomView.transform）
        private Transform _targetTransform;
        private Camera _mainCam;
    
        void Awake()
        {
            if (!canvas) canvas = GetComponentInParent<Canvas>();
            if (!root) root = GetComponent<RectTransform>();
            _mainCam = Camera.main;
        }
    
        private void Start()
        {
            Hide();
        }
    
        /// <summary>
        /// 固定在 targetTransform 上方的选择面板
        /// </summary>
        public void Init(string ghostId, int slotIndex, Transform targetTransform, Action<string, FearTag, int> onPick)
        {
            _ghostId = ghostId;
            _slotIndex = slotIndex;
            _onPick = onPick;
            _targetTransform = targetTransform;
    
            Show();
            GenerateFearButtons();
            PlacePanelAtTarget();
        }
    
        void Update()
        {
            // 若面板显示中且有目标，就持续锚定到目标
            if (root != null && root.gameObject.activeInHierarchy && _targetTransform != null)
            {
                PlacePanelAtTarget();
            }
        }
    
        private void PlacePanelAtTarget()
        {
            if (canvas == null || _targetTransform == null) return;
    
            Vector3 worldPos = _targetTransform.position + panelWorldOffset;
            Vector3 screenPos = _mainCam != null ? _mainCam.WorldToScreenPoint(worldPos) : worldPos;
    
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform, screenPos, canvas.worldCamera, out var localPos);
            root.anchoredPosition = localPos;
        }
    
        // ---------- 生成按钮：先清理再按 Tag→Sprite 创建 ----------
        private void GenerateFearButtons()
        {
            EnsureContainer();
    
            // 清理旧的按钮（避免重复创建）
            for (int i = buttonContainer.childCount - 1; i >= 0; i--)
            {
                var child = buttonContainer.GetChild(i);
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
    
            var fearTags = (FearTag[])Enum.GetValues(typeof(FearTag));
            foreach (var fearTag in fearTags)
                CreateFearButton(fearTag);
        }
    
        private void EnsureContainer()
        {
            if (buttonContainer != null) return;
    
            // 如果没有指定容器，创建一个带纵向布局的容器
            var containerObj = new GameObject("ButtonContainer", typeof(RectTransform));
            containerObj.transform.SetParent(root, false);
            buttonContainer = containerObj.transform;
    
            var layoutGroup = containerObj.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
    
            var contentFitter = containerObj.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    
        private void CreateFearButton(FearTag fearTag)
        {
            // 容器下创建按钮
            var buttonObj = new GameObject(fearTag.ToString(), typeof(RectTransform));
            buttonObj.transform.SetParent(buttonContainer, false);
    
            // 组件
            var button = buttonObj.AddComponent<Button>();
            var image  = buttonObj.AddComponent<Image>(); // 显示 Atlas 里的图标
            var btnRect = buttonObj.GetComponent<RectTransform>();
    
            // 如果没有自动布局，给一个固定大小
            if (buttonContainer.GetComponent<HorizontalOrVerticalLayoutGroup>() == null &&
                buttonContainer.GetComponent<GridLayoutGroup>() == null)
            {
                btnRect.sizeDelta = new Vector2(buttonSize, buttonSize);
            }
    
            // 从 Atlas 拿图标（仿照 FearIconsPanel 的做法）
            Sprite icon = fearIconAtlas != null ? fearIconAtlas.Get(fearTag) : null;
            if (icon != null)
            {
                image.sprite = icon;
                image.preserveAspect = true;
                image.color = Color.white;
            }
            else
            {
                // 没图时用深灰底兜底
                image.sprite = null;
                image.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            }
    
            // 可选：小文字标签（找不到图标时强制显示）
            if (showLabel || icon == null)
            {
                var textObj = new GameObject("Label", typeof(RectTransform));
                textObj.transform.SetParent(buttonObj.transform, false);
                var text = textObj.AddComponent<Text>();
                text.text = fearTag.ToString();
                text.color = icon != null ? new Color(1f, 1f, 1f, 0.85f) : Color.white;
                text.alignment = TextAnchor.MiddleCenter;
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    
                var textRect = text.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
            }
    
            // 点击
            button.onClick.AddListener(() => OnFearTagSelected(fearTag));
    
            // 可访问性：避免挡住外层交互
            image.raycastTarget = true;
        }
    
        private void OnFearTagSelected(FearTag fearTag)
        {
            _onPick?.Invoke(_ghostId, fearTag, _slotIndex);
            Hide();
        }
    
        public void Show()
        {
            if (root) root.gameObject.SetActive(true);
            if (_targetTransform != null) PlacePanelAtTarget();
        }
    
        public void Hide()
        {
            if (root) root.gameObject.SetActive(false);
            _targetTransform = null;
        }
    }
}