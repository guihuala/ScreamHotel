using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TimelineComicPlayer : MonoBehaviour
{
    [Header("绑定")]
    [SerializeField] private PlayableDirector director;
    [SerializeField] private Button skipButton;
    [SerializeField] private CanvasGroup skipHint;

    [Header("根节点显隐")]
    [SerializeField] private GameObject comicRoot;
    [SerializeField] private bool hideRootOnAwake = true;
    [SerializeField] private bool hideRootWhenDone = true;

    [Header("行为")]
    [SerializeField] private bool skippable = true;
    [SerializeField] private float preDelay = 0.1f;
    [SerializeField] private float postDelay = 0.1f;

    [Header("淡入/淡出")]
    [SerializeField] private CanvasGroup fadeCanvas; // 纯黑遮罩
    [SerializeField] private float fadeInTime = 0.2f;
    [SerializeField] private float fadeOutTime = 0.2f;

    public bool IsPlaying { get; private set; }

    Action _onComplete;
    bool _requestedSkip;

    void Awake()
    {
        if (skipButton) skipButton.onClick.AddListener(RequestSkip);
        if (director) director.stopped += OnDirectorStopped;

        if (hideRootOnAwake && comicRoot) comicRoot.SetActive(false);
        if (skipHint) { skipHint.alpha = 0f; skipHint.blocksRaycasts = false; }
        if (fadeCanvas) { fadeCanvas.alpha = 0f; fadeCanvas.blocksRaycasts = false; }
    }

    void OnDestroy()
    {
        if (skipButton) skipButton.onClick.RemoveListener(RequestSkip);
        if (director) director.stopped -= OnDirectorStopped;
    }

    /// <summary>
    /// 播放漫画；回调仅在“播放自然结束或跳过并完成收尾”后触发
    /// </summary>
    public void PlayComic(Action onComplete)
    {
        if (director == null || director.playableAsset == null)
        {
            onComplete?.Invoke();
            return;
        }
        // 若已在播放，直接忽略（也可改为队列/覆盖）
        if (IsPlaying) return;

        _onComplete = onComplete;
        _requestedSkip = false;
        
        if (comicRoot && !comicRoot.activeSelf) comicRoot.SetActive(true);
        StartCoroutine(PlayRoutine());
    }

    IEnumerator PlayRoutine()
    {
        IsPlaying = true;

        // 准备阶段：黑场→就绪
        if (fadeCanvas) yield return StartCoroutine(Fade(fadeCanvas, 0f, 1f, 0f));
        yield return new WaitForSeconds(preDelay);

        director.Play();

        if (fadeCanvas) yield return StartCoroutine(Fade(fadeCanvas, 1f, 0f, fadeInTime));
        if (skipHint) StartCoroutine(Fade(skipHint, skipHint.alpha, 1f, 0.25f));

        director.time = 0;
        director.Evaluate();
  

        // 播放期间轮询跳过
        while (IsPlaying)
        {
            if (skippable && CheckSkipInputs()) RequestSkip();
            if (_requestedSkip)
            {
                if (skipHint) StartCoroutine(Fade(skipHint, skipHint.alpha, 0f, 0.15f));
                break;
            }
            yield return null;
        }

        // 真·跳过：把时间推到末尾以触发末尾事件/Signal，再停
        if (_requestedSkip && director.state == PlayState.Playing)
        {
            director.time = director.duration;
            director.Evaluate();
            director.Stop();
        }

        // 收尾：淡入黑场
        if (fadeCanvas) yield return StartCoroutine(Fade(fadeCanvas, 0f, 1f, fadeOutTime));
        if (skipHint) skipHint.alpha = 0f;

        yield return new WaitForSeconds(postDelay);

        IsPlaying = false;

        // **修正点3：仅此处（播放真正结束/跳过收尾之后）才调用回调**
        var cb = _onComplete; _onComplete = null;
        cb?.Invoke();

        if (hideRootWhenDone && comicRoot) comicRoot.SetActive(false);

        // 清场：避免遮罩残留
        if (fadeCanvas) yield return StartCoroutine(Fade(fadeCanvas, 1f, 0f, 0.001f));
    }

    void OnDirectorStopped(PlayableDirector _) => IsPlaying = false;

    bool CheckSkipInputs()
    {
        return Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);
    }

    void RequestSkip()
    {
        if (!skippable) return;
        _requestedSkip = true;
    }

    IEnumerator Fade(CanvasGroup cg, float from, float to, float dur)
    {
        if (!cg) yield break;
        float t = 0f;
        cg.blocksRaycasts = (to > 0.001f);
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = dur <= 0 ? 1f : Mathf.Clamp01(t / dur);
            cg.alpha = Mathf.Lerp(from, to, k);
            yield return null;
        }
        cg.alpha = to;
        cg.blocksRaycasts = (to > 0.001f);
    }
}
