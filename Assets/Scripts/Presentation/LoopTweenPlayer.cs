// CommonLoopMotion.cs
// 需要 DOTween（Tools > Demigiant > DOTween Utility Panel > Setup DOTween...）
using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public class CommonLoopMotion : MonoBehaviour
{
    [Header("全局")]
    [Tooltip("启用时自动播放")]
    public bool playOnEnable = true;
    [Tooltip("开始前的延迟（秒）")]
    public float startDelay = 0f;
    [Tooltip("忽略时间缩放（UI/主界面常用）")]
    public bool useUnscaledTime = true;
    [Tooltip("禁用组件时是否还原到初始 Transform")]
    public bool restoreOnDisable = true;
    [Tooltip("随机相位（每次启用时随机一个起始进度）")]
    public bool randomizePhase = true;

    [Header("位移·漂浮（上下/左右/任意轴来回移动）")]
    public bool enablePositionBob = true;
    [Tooltip("在本地坐标系运动，否则在世界坐标系")]
    public bool positionInLocalSpace = true;
    [Tooltip("漂浮方向（单位向量）")]
    public Vector3 bobDirection = Vector3.up;
    [Tooltip("漂浮幅度（单位距离）")]
    public float bobAmplitude = 0.2f;
    [Tooltip("往返一整个周期所需时间（秒）")]
    public float bobPeriod = 1.6f;
    public Ease bobEase = Ease.InOutSine;

    [Header("旋转·摆动（绕某轴来回旋转）")]
    public bool enableRotationSway = false;
    [Tooltip("摆动轴（欧拉角的量纲方向）")]
    public Vector3 swayAxis = new Vector3(0f, 0f, 1f); // Z轴轻微偏摆
    [Tooltip("摆动角度（度）")]
    public float swayAngle = 8f;
    [Tooltip("往返一整个周期所需时间（秒）")]
    public float swayPeriod = 1.2f;
    public Ease swayEase = Ease.InOutSine;

    [Header("缩放·呼吸（脉动缩放）")]
    public bool enableScalePulse = false;
    [Tooltip("缩放增量（相对 1 的比例，比如 0.05 = ±5%）")]
    public float scaleAmount = 0.05f;
    [Tooltip("往返一整个周期所需时间（秒）")]
    public float scalePeriod = 1.4f;
    public Ease scaleEase = Ease.InOutSine;

    // 基准缓存
    Vector3 _basePosWorld, _basePosLocal, _baseScale, _baseEuler;
    Tween _posTween, _rotTween, _scaleTween;

    void Awake()
    {
        CacheBase();
    }

    void OnEnable()
    {
        if (playOnEnable) Play();
    }

    void OnDisable()
    {
        KillTweens();
        if (restoreOnDisable) RestoreBase();
    }

    [ContextMenu("Play")]
    public void Play()
    {
        KillTweens();
        CacheBase();

        // 位移漂浮
        if (enablePositionBob && bobAmplitude > 0f && bobPeriod > 0.0001f)
        {
            var dir = (bobDirection.sqrMagnitude < 1e-6f ? Vector3.up : bobDirection.normalized);
            var halfCycle = Mathf.Max(0.0001f, bobPeriod * 0.5f);
            var to = dir * bobAmplitude; // 往返幅度

            if (positionInLocalSpace)
            {
                _posTween = transform.DOLocalMove(_basePosLocal + to, halfCycle)
                                     .SetEase(bobEase)
                                     .SetLoops(-1, LoopType.Yoyo)
                                     .SetUpdate(useUnscaledTime)
                                     .SetDelay(startDelay);
            }
            else
            {
                _posTween = transform.DOMove(_basePosWorld + to, halfCycle)
                                     .SetEase(bobEase)
                                     .SetLoops(-1, LoopType.Yoyo)
                                     .SetUpdate(useUnscaledTime)
                                     .SetDelay(startDelay);
            }

            MaybeRandomizePhase(_posTween, bobPeriod);
        }

        // 旋转摆动
        if (enableRotationSway && swayAngle != 0f && swayPeriod > 0.0001f)
        {
            var axis = (swayAxis.sqrMagnitude < 1e-6f ? Vector3.forward : swayAxis.normalized);
            var target = _baseEuler + axis * swayAngle;
            var halfCycle = Mathf.Max(0.0001f, swayPeriod * 0.5f);

            _rotTween = transform.DOLocalRotate(target, halfCycle, RotateMode.Fast)
                                 .SetEase(swayEase)
                                 .SetLoops(-1, LoopType.Yoyo)
                                 .SetUpdate(useUnscaledTime)
                                 .SetDelay(startDelay);

            MaybeRandomizePhase(_rotTween, swayPeriod);
        }

        // 缩放呼吸
        if (enableScalePulse && scaleAmount != 0f && scalePeriod > 0.0001f)
        {
            var target = _baseScale * (1f + Mathf.Abs(scaleAmount));
            var halfCycle = Mathf.Max(0.0001f, scalePeriod * 0.5f);

            _scaleTween = transform.DOScale(target, halfCycle)
                                   .SetEase(scaleEase)
                                   .SetLoops(-1, LoopType.Yoyo)
                                   .SetUpdate(useUnscaledTime)
                                   .SetDelay(startDelay);

            MaybeRandomizePhase(_scaleTween, scalePeriod);
        }
    }

    [ContextMenu("Stop")]
    public void Stop()
    {
        KillTweens();
    }

    [ContextMenu("Rebuild & Play")]
    public void RebuildAndPlay()
    {
        Stop();
        Play();
    }

    [ContextMenu("Pause")]
    public void Pause()
    {
        _posTween?.Pause();
        _rotTween?.Pause();
        _scaleTween?.Pause();
    }

    [ContextMenu("Resume")]
    public void Resume()
    {
        _posTween?.Play();
        _rotTween?.Play();
        _scaleTween?.Play();
    }

    void CacheBase()
    {
        _basePosWorld = transform.position;
        _basePosLocal = transform.localPosition;
        _baseScale    = transform.localScale;
        _baseEuler    = transform.localEulerAngles;
    }

    void RestoreBase()
    {
        transform.position = _basePosWorld;
        transform.localPosition = _basePosLocal;
        transform.localScale = _baseScale;
        transform.localEulerAngles = _baseEuler;
    }

    void KillTweens()
    {
        _posTween?.Kill();
        _rotTween?.Kill();
        _scaleTween?.Kill();
        _posTween = _rotTween = _scaleTween = null;
    }

    void MaybeRandomizePhase(Tween t, float period)
    {
        if (t == null || !randomizePhase) return;
        // 参数说明：andPlay=true 继续播放
        t.Goto(Random.Range(0f, Mathf.Max(0.0001f, period)), true);
    }
}
