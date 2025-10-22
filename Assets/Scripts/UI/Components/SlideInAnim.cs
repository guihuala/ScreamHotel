using UnityEngine;
using System.Collections;

public class SlideInAnim : MonoBehaviour
{
    [Header("要滑入的对象")]
    [SerializeField] private RectTransform target;

    [Header("位置（使用锚点坐标）")]
    [SerializeField] private Vector2 startAnchoredPos;
    [SerializeField] private Vector2 endAnchoredPos = Vector2.zero;

    [Space(6)]
    [SerializeField] private bool computeStartFromEnd = true;
    [SerializeField] private float startYOffset = 0f;

    [Header("滑入设置")]
    [SerializeField] private float slideDuration = 0.8f;
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("屏幕震动设置")]
    [SerializeField] private float shakeDuration = 0.35f;
    [SerializeField] private float shakeMagnitude = 12f; // 像素
    [SerializeField] private AnimationCurve shakeDamping = AnimationCurve.EaseInOut(0, 1, 1, 0);
    [SerializeField] private Transform customShakeTarget;

    [Header("时间缩放")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("完成后回调")]
    public System.Action OnFinished;

    bool _isPlaying;
    Vector3 _shakeOrigPos;

    public void Play()
    {
        if (_isPlaying || target == null) return;
        StartCoroutine(PlayRoutine());
    }

    IEnumerator PlayRoutine()
    {
        _isPlaying = true;

        // 计算起点（完全不依赖 target 当前初始位置）
        Vector2 startPos = startAnchoredPos;
        if (computeStartFromEnd)
        {
            float offset = startYOffset;
            if (Mathf.Approximately(offset, 0f))
            {
                // 用画布高度作为偏移：保证从屏幕下方“离屏”位置开始
                var canvas = target.GetComponentInParent<Canvas>();
                if (canvas != null && canvas.TryGetComponent<RectTransform>(out var canvasRect))
                    offset = canvasRect.rect.height;
                else
                    offset = 1080f; // 回退值，避免为0
            }
            startPos = endAnchoredPos + Vector2.down * offset;
        }

        // 放到起点
        target.anchoredPosition = startPos;

        // 并行：滑入 + 震动
        var slide = StartCoroutine(SlideIn(startPos, endAnchoredPos));
        var shake = StartCoroutine(ShakeScreen());
        yield return slide;

        // 结束
        _isPlaying = false;
        OnFinished?.Invoke();
    }

    IEnumerator SlideIn(Vector2 from, Vector2 to)
    {
        float t = 0f;
        float dur = Mathf.Max(0.0001f, slideDuration);
        while (t < 1f)
        {
            t += (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) / dur;
            float k = ease.Evaluate(Mathf.Clamp01(t));
            target.anchoredPosition = Vector2.LerpUnclamped(from, to, k);
            yield return null;
        }
        target.anchoredPosition = to;
    }

    IEnumerator ShakeScreen()
    {
        Transform shaker = customShakeTarget != null
            ? customShakeTarget
            : (Camera.main != null ? Camera.main.transform : target.transform);

        _shakeOrigPos = shaker.localPosition;

        float t = 0f;
        float dur = Mathf.Max(0.0001f, shakeDuration);
        while (t < dur)
        {
            t += (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
            float norm = Mathf.Clamp01(t / dur);
            float damper = shakeDamping.Evaluate(norm);

            float x = (Random.value * 2f - 1f) * shakeMagnitude * damper;
            float y = (Random.value * 2f - 1f) * shakeMagnitude * damper;
            shaker.localPosition = _shakeOrigPos + new Vector3(x, y, 0f);

            yield return null;
        }
        shaker.localPosition = _shakeOrigPos;
    }
}
