using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ScreamHotel.Domain;

public class PickFearPanel : MonoBehaviour
{
    [Header("UI")]
    public Canvas canvas;
    public RectTransform root;
    public Transform buttonContainer;  // 按钮容器

    [Header("Panel Position")]
    public Vector3 panelWorldOffset = new Vector3(0f, 2f, 0f); // 面板相对目标的世界偏移

    private Action<string, FearTag, int> _onPick;
    private string _ghostId;
    private int _slotIndex;

    // 锚定目标（例如 TrainingSlot.transform）
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

    private void GenerateFearButtons()
    {
        if (buttonContainer == null)
        {
            // 如果没有指定容器，创建一个
            var containerObj = new GameObject("ButtonContainer");
            containerObj.transform.SetParent(root, false);
            buttonContainer = containerObj.transform;

            // 布局
            var layoutGroup = containerObj.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;

            var contentFitter = containerObj.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        var fearTags = (FearTag[])Enum.GetValues(typeof(FearTag));
        foreach (var fearTag in fearTags)
            CreateFearButton(fearTag);
    }

    private void CreateFearButton(FearTag fearTag)
    {
        var buttonObj = new GameObject(fearTag.ToString());
        buttonObj.transform.SetParent(buttonContainer, false);

        var button = buttonObj.AddComponent<Button>();
        var image  = buttonObj.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        var text = textObj.AddComponent<Text>();
        text.text = fearTag.ToString();
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        var buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 100f);

        button.onClick.AddListener(() => OnFearTagSelected(fearTag));
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
