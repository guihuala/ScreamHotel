using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using System.Collections;

public enum GameScene
{
    MainMenu = 0,
    Game = 1,
}

public class SceneLoader : SingletonPersistent<SceneLoader>
{
    [Header("遮罩面板（整块 UI 放这里）")]
    [Tooltip("整块转场 UI 的根物体（会在加载时启/停）")]
    [SerializeField] private GameObject maskPanelRoot;

    [Header("遮罩控制")]
    [Tooltip("做开合动画的遮罩 RectTransform（一般是带 Mask/RectMask2D 的 Image）")]
    [SerializeField] private RectTransform startMask;
    [Tooltip("遮罩完全打开时的尺寸（正方形更自然）")]
    [SerializeField] private float openSize = 4000f;
    [Tooltip("开场：遮罩从 0 → openSize 的时长")]
    [SerializeField] private float openTime = 0.5f;
    [Tooltip("收场：遮罩从 openSize → 0 的时长")]
    [SerializeField] private float closeTime = 0.5f;
    [Tooltip("忽略 Time.timeScale（转场常用真实时间）")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("其它")]
    [Tooltip("最小加载时间（避免闪屏）")]
    [SerializeField] private float minLoadingTime = 1.0f;

    private AsyncOperation loadingOperation;
    private bool isLoading = false;

    // 记录初始遮罩尺寸，便于多次复用
    private Vector2 _maskInitSize;

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);

        if (maskPanelRoot) maskPanelRoot.SetActive(false);
        if (startMask) _maskInitSize = startMask.sizeDelta;
    }

    public bool IsLoading() => isLoading;

    public void LoadScene(GameScene scene, Action onComplete = null)
    {
        if (isLoading) return;
        StartCoroutine(LoadSceneRoutine(scene.ToString(), onComplete));
    }

    public void ReloadCurrentScene(Action onComplete = null)
    {
        if (isLoading) return;
        StartCoroutine(LoadSceneRoutine(SceneManager.GetActiveScene().name, onComplete));
    }

    private IEnumerator LoadSceneRoutine(string sceneName, Action onComplete)
    {
        isLoading = true;
        float startTime = Time.time;

        // === 1) 打开遮罩面板 & 开场动画（0 → openSize） ===
        if (maskPanelRoot) maskPanelRoot.SetActive(true);
        if (startMask)
        {
            startMask.sizeDelta = Vector2.zero;
            yield return StartCoroutine(AnimateMaskSize(Vector2.zero, new Vector2(openSize, openSize), openTime));
        }

        // === 2) 异步加载（在遮罩打开状态下进行） ===
        loadingOperation = SceneManager.LoadSceneAsync(sceneName);
        loadingOperation.allowSceneActivation = false;

        while (!loadingOperation.isDone)
        {
            // 进度到 0.9 且最小时长满足后激活
            if (loadingOperation.progress >= 0.9f &&
                Time.time - startTime >= minLoadingTime)
            {
                loadingOperation.allowSceneActivation = true;
            }
            yield return null;
        }
        yield return null; // 确保切换完成

        // === 3) 收场动画（openSize → 0），并关闭面板 ===
        if (startMask)
        {
            yield return StartCoroutine(AnimateMaskSize(new Vector2(openSize, openSize), Vector2.zero, closeTime));
            startMask.sizeDelta = _maskInitSize; // 复原初始值，方便下次
        }
        if (maskPanelRoot) maskPanelRoot.SetActive(false);

        isLoading = false;
        onComplete?.Invoke();
    }

    // —— 工具：协程插值遮罩的 sizeDelta —— //
    private IEnumerator AnimateMaskSize(Vector2 from, Vector2 to, float duration)
    {
        if (!startMask || duration <= 0f)
        {
            if (startMask) startMask.sizeDelta = to;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
            float k = Mathf.Clamp01(t / duration);
            // 可替换为更平滑的缓动：k = Mathf.SmoothStep(0f, 1f, k);
            startMask.sizeDelta = Vector2.LerpUnclamped(from, to, k);
            yield return null;
        }
        startMask.sizeDelta = to;
    }
}
