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
    public Vector2 panelOffset = new Vector2(100f, 16f); // 面板相对于鼠标位置的屏幕偏移

    private Action<string, FearTag, int> _onPick;
    private string _ghostId;
    private int _slotIndex;

    void Awake()
    {
        if (!canvas) canvas = GetComponentInParent<Canvas>();
        if (!root) root = GetComponent<RectTransform>();
    }

    private void Start()
    {
        Hide();
    }

    public void Init(string ghostId, int slotIndex, Action<string, FearTag, int> onPick)
    {
        _ghostId = ghostId;
        _slotIndex = slotIndex;
        _onPick = onPick;

        // 显示面板并设置位置
        Show();
        
        // 自动生成按钮
        GenerateFearButtons();
    }

    void Update()
    {
        // 如果面板显示中，持续更新位置以跟随鼠标
        if (root.gameObject.activeInHierarchy)
        {
            PlacePanelAtMouse();
        }
    }

    private void PlacePanelAtMouse()
    {
        if (!canvas) return;

        // 计算面板的屏幕位置（鼠标位置 + 偏移）
        Vector2 screenPosition = (Vector2)Input.mousePosition + panelOffset;
        
        // 转换为UI的局部坐标
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform, screenPosition, canvas.worldCamera, out var localPos);
        
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
            
            // 添加布局组件
            var layoutGroup = containerObj.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            
            var contentFitter = containerObj.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        var fearTags = Enum.GetValues(typeof(FearTag)) as FearTag[];
        if (fearTags == null) return;

        foreach (var fearTag in fearTags)
        {
            CreateFearButton(fearTag);
        }
    }

    private void CreateFearButton(FearTag fearTag)
    {
        // 创建按钮对象
        var buttonObj = new GameObject(fearTag.ToString());
        buttonObj.transform.SetParent(buttonContainer, false);
        
        var button = buttonObj.AddComponent<Button>();
        var image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        
        // 创建按钮文本
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        var text = textObj.AddComponent<Text>();
        text.text = fearTag.ToString();
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        
        // 设置文本的RectTransform
        var textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        // 设置按钮大小
        var buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 100f);
        
        // 添加点击事件
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
        PlacePanelAtMouse();
    }

    public void Hide()
    {
        if (root) root.gameObject.SetActive(false);
    }
}