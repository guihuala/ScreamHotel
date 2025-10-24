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
    [SerializeField] private CanvasGroup skipHint;   // Skip整组

    [Header("根节点显隐")]
    [SerializeField] private GameObject comicRoot;
    [SerializeField] private bool hideRootOnAwake = true;
    [SerializeField] private bool hideRootWhenDone = true;

    [Header("行为")]
    [SerializeField] private bool skippable = true;
    [SerializeField] private float preDelay = 0.1f;
    [SerializeField] private float postDelay = 0.1f;

    [Header("淡入/淡出")]
    [SerializeField] private CanvasGroup fadeCanvas;   // 黑幕
    [SerializeField] private CanvasGroup comicCanvas;  // 漫画本体（整层或容器）——新增
    [SerializeField] private float fadeInTime = 0.2f;   // 开始前：0→1（黑幕 & 漫画）
    [SerializeField] private float fadeOutTime = 0.2f;  // 结束时：0→1 黑幕

    public bool IsPlaying { get; private set; }

    Action _onComplete;
    bool _requestedSkip;

    void Awake()
    {
        if (skipButton) skipButton.onClick.AddListener(RequestSkip);
        if (director) director.stopped += OnDirectorStopped;

        if (hideRootOnAwake && comicRoot) comicRoot.SetActive(false);
        
        if (skipHint)
        {
            skipHint.alpha = 1f;
            skipHint.blocksRaycasts = true;
        }
        
        if (fadeCanvas)
        {
            fadeCanvas.alpha = 0f;
            fadeCanvas.blocksRaycasts = false;
        }
        if (comicCanvas)
        {
            comicCanvas.alpha = 0f;
            comicCanvas.blocksRaycasts = false;
        }
    }

    void OnDestroy()
    {
        if (skipButton) skipButton.onClick.RemoveListener(RequestSkip);
        if (director) director.stopped -= OnDirectorStopped;
    }

    public void PlayComic(Action onComplete)
    {
        if (director == null || director.playableAsset == null)
        {
            onComplete?.Invoke();
            return;
        }
        if (IsPlaying) return;

        _onComplete = onComplete;
        _requestedSkip = false;

        if (comicRoot && !comicRoot.activeSelf) comicRoot.SetActive(true);
        StartCoroutine(PlayRoutine());
    }

    IEnumerator PlayRoutine()
    {
        IsPlaying = true;

        yield return StartCoroutine(FadeTogetherToOne(fadeInTime));
        yield return new WaitForSeconds(preDelay);
        
        director.time = 0;
        director.Evaluate();
        director.Play();
        
        if (fadeCanvas)
            yield return StartCoroutine(Fade(fadeCanvas, 1f, 0f, fadeInTime));
        
        while (IsPlaying)
        {
            if (_requestedSkip) break;
            yield return null;
        }

        if (_requestedSkip && director.state == PlayState.Playing)
        {
            director.time = director.duration;
            director.Evaluate();
            director.Stop();
        }
        
        if (fadeCanvas) yield return StartCoroutine(Fade(fadeCanvas, 0f, 1f, fadeOutTime));

        yield return new WaitForSeconds(postDelay);

        IsPlaying = false;
        var cb = _onComplete; _onComplete = null;
        cb?.Invoke();

        if (hideRootWhenDone && comicRoot) comicRoot.SetActive(false);
        
        if (fadeCanvas) yield return StartCoroutine(Fade(fadeCanvas, 1f, 0f, 0.001f));
    }

    void OnDirectorStopped(PlayableDirector _) => IsPlaying = false;

    // 仅允许通过按钮跳过
    void RequestSkip()
    {
        if (!skippable) return;
        _requestedSkip = true;
    }
    
    IEnumerator FadeTogetherToOne(float dur)
    {
        if (fadeCanvas)
            yield return StartCoroutine(Fade(fadeCanvas, fadeCanvas.alpha, 1f, dur));
        
        if (comicCanvas)
        {
            comicCanvas.alpha = 1f;
        }
    }
    
    IEnumerator Fade(CanvasGroup cg, float from, float to, float dur)
    {
        if (!cg) yield break;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = dur <= 0 ? 1f : Mathf.Clamp01(t / dur);
            cg.alpha = Mathf.Lerp(from, to, k);
            yield return null;
        }
        cg.alpha = to;
 
        if (cg == fadeCanvas)
            cg.blocksRaycasts = (to > 0.001f);
    }
}
