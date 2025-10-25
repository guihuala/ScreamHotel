using UnityEngine;
using UnityEngine.UI;

public class GuidePanel : BasePanel
{
    [Header("操作指南组件")]
    public Text titleText;
    public Text pageText;
    public Button closeButton;
    public Button nextButton;
    public Button prevButton;
    public Transform content;

    [Header("指南内容")]
    public string[] pageTitles; // 页面标题

    public GameObject[] pagePrefabs; // 存储每一页对应的预制件

    private int currentPage = 0;

    protected override void Awake()
    {
        base.Awake();

        // 初始化按钮事件
        closeButton.onClick.AddListener(OnCloseClick);
        nextButton.onClick.AddListener(OnNextClick);
        prevButton.onClick.AddListener(OnPrevClick);
    }

    public override void OpenPanel(string name)
    {
        base.OpenPanel(name);

        // 初始化内容
        currentPage = 0;
        UpdateContent();

        // 设置初始交互状态
        SetInteractable(true);
    }

    private void Update()
    {
        // 快捷键支持
        if (canvasGroup.alpha > 0.9f && canvasGroup.interactable)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.H))
            {
                OnCloseClick();
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                OnNextClick();
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                OnPrevClick();
            }
        }
    }

    private void UpdateContent()
    {
        if (pageTitles.Length == 0) return;

        // 更新标题和文本内容
        titleText.text = pageTitles[currentPage];
        pageText.text = $"{currentPage + 1}/{pageTitles.Length}";

        // 显示当前页面的预制件（如果有）
        ShowPagePrefab(currentPage);

        // 更新按钮状态
        prevButton.interactable = currentPage > 0;
        nextButton.interactable = currentPage < pageTitles.Length - 1;
    }

    private void ShowPagePrefab(int pageIndex)
    {
        // 如果页面有对应的预制件，则展示
        if (pagePrefabs != null && pagePrefabs.Length > pageIndex && pagePrefabs[pageIndex] != null)
        {
            // 删除之前的预制件（如果有）
            foreach (Transform child in content)
            {
                if (child != titleText.transform && child != pageText.transform)
                {
                    Destroy(child.gameObject);
                }
            }

            // 实例化当前页面的预制件
            Instantiate(pagePrefabs[pageIndex], content);
        }
    }

    private void OnNextClick()
    {
        if (currentPage < pageTitles.Length - 1)
        {
            currentPage++;
            UpdateContent();
        }
    }

    private void OnPrevClick()
    {
        if (currentPage > 0)
        {
            currentPage--;
            UpdateContent();
        }
    }

    private void OnCloseClick()
    {
        UIManager.Instance.ClosePanel(panelName);
    }
}