using UnityEngine;
using ScreamHotel.Core;
using System.Collections;

[DisallowMultipleComponent]
public class CameraController : MonoBehaviour
{
    [Header("移动")]
    [Tooltip("键盘移动速度（单位/秒）")]
    public float keyboardSpeed = 10f;
    [Tooltip("位置插值速度（越大越跟手）；<=0 表示直接赋值不插值")]
    public float positionLerp = 12f;

    [Header("边界 (XY)")]
    public bool enableBounds = true;
    public float minX = -50f;
    public float maxX = 50f;
    public float minY = -30f;
    public float maxY = 30f;

    [Header("屏幕边缘移动")]
    public bool enableEdgePan = true;
    [Tooltip("边缘范围（像素），鼠标进入该范围开始触发移动")]
    public float edgeZonePixels = 24f;
    [Tooltip("卷屏基础速度（单位/秒）")]
    public float edgeSpeed = 10f;
    [Tooltip("按接近边缘的程度进行加速的曲线（0=刚进入边缘，1=贴边）")]
    public AnimationCurve edgeAccel = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Tooltip("应用未聚焦时是否忽略边缘输入")]
    public bool ignoreEdgeWhenUnfocused = true;

    [Header("其它")]
    [Tooltip("是否锁定并维持初始 Z 值")]
    public bool lockZ = true;

    [Header("震动效果")]
    public float shakeDuration = 1.5f;
    public float shakeStrength = 0.3f;
    public float shakeFrequency = 20f;

    [Header("执行阶段（NightExecute）相机控制")]
    [Tooltip("进入 NightExecute 时是否回正到指定位置")]
    public bool recenterOnExecute = true;
    [Tooltip("NightExecute 阶段的相机焦点（回正到这个位置）")]
    public Vector3 executeFocusPosition = new Vector3(0f, 0f, -10f);
    [Tooltip("回正插值速度；<=0 则瞬间跳转到焦点")]
    public float executeRecenterLerp = 12f;
    [Tooltip("NightExecute 期间是否禁止水平移动（仅允许上下）")]
    public bool restrictHorizontalInExecute = true;

    private Camera _cam;
    private Vector3 _targetPosition;
    private float _fixedZ;

    private Coroutine _shakeRoutine;
    private Vector3 _originalPos;

    // 执行阶段标记
    private bool _inExecute;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        if (_cam == null) _cam = Camera.main;

        _fixedZ = transform.position.z;
        _targetPosition = transform.position;
        _originalPos = transform.localPosition;

        // 监听游戏阶段变化
        EventBus.Subscribe<GameStateChanged>(OnGameStateChanged);
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<GameStateChanged>(OnGameStateChanged);
    }

    private void OnGameStateChanged(GameStateChanged evt)
    {
        if (evt.State is GameState state)
        {
            _inExecute = (state == GameState.NightExecute);

            if (_inExecute)
            {
                // 进入夜间执行时震动相机
                if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
                _shakeRoutine = StartCoroutine(ShakeCamera());

                // 回正到配置位置
                if (recenterOnExecute)
                {
                    // 目标位置以焦点为准；Z 仍由 lockZ 控制
                    _targetPosition = new Vector3(
                        executeFocusPosition.x,
                        executeFocusPosition.y,
                        lockZ ? _fixedZ : executeFocusPosition.z
                    );

                    if (executeRecenterLerp <= 0f)
                    {
                        // 瞬间回正
                        var desired = _targetPosition;
                        if (lockZ) desired.z = _fixedZ;
                        transform.position = desired;
                    }
                    // 若 executeRecenterLerp > 0，则交由 Update 中插值至 _targetPosition
                }
            }
            else
            {
                // 离开执行阶段：恢复正常（不做额外处理，Update/输入逻辑自动恢复）
            }
        }
    }

    private IEnumerator ShakeCamera()
    {
        float timer = 0f;
        Vector3 basePos = transform.localPosition;

        while (timer < shakeDuration)
        {
            timer += Time.deltaTime;
            float t = timer / shakeDuration;

            // 衰减强度
            float strength = shakeStrength * (1f - t);

            // 随机方向震动（Perlin）
            float offsetX = Mathf.PerlinNoise(Time.time * shakeFrequency, 0f) - 0.5f;
            float offsetY = Mathf.PerlinNoise(0f, Time.time * shakeFrequency) - 0.5f;
            Vector3 offset = new Vector3(offsetX, offsetY, 0f) * strength;

            transform.localPosition = basePos + offset;
            yield return null;
        }

        // 恢复原位
        transform.localPosition = basePos;
        _shakeRoutine = null;
    }

    void Update()
    {
        HandleKeyboard();
        if (enableEdgePan && (!ignoreEdgeWhenUnfocused || Application.isFocused))
        {
            HandleEdgePan();
        }

        // 执行阶段锁水平：强制目标X为焦点X（即使输入尝试改变）
        if (_inExecute && restrictHorizontalInExecute)
        {
            _targetPosition.x = executeFocusPosition.x;
        }

        // 约束到边界
        if (enableBounds)
        {
            _targetPosition.x = Mathf.Clamp(_targetPosition.x, minX, maxX);
            _targetPosition.y = Mathf.Clamp(_targetPosition.y, minY, maxY);
        }

        // 应用位置（插值或直接设置）
        Vector3 desired = _targetPosition;
        if (lockZ) desired.z = _fixedZ;

        float lerpSpeed = positionLerp;

        // 若刚进入执行阶段希望更快/更稳地回正，可用单独的插值速度
        if (_inExecute && recenterOnExecute && executeRecenterLerp > 0f)
            lerpSpeed = executeRecenterLerp;

        if (lerpSpeed > 0f)
        {
            transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * lerpSpeed);
        }
        else
        {
            transform.position = desired;
        }
    }

    private void HandleKeyboard()
    {
        float h = Input.GetAxisRaw("Horizontal"); // A/D 或 ←/→
        float v = Input.GetAxisRaw("Vertical");   // W/S 或 ↑/↓

        // 执行阶段禁止水平输入
        if (_inExecute && restrictHorizontalInExecute) h = 0f;

        if (Mathf.Abs(h) > 0.0001f || Mathf.Abs(v) > 0.0001f)
        {
            Vector3 move = new Vector3(h, v, 0f) * keyboardSpeed * Time.deltaTime;
            _targetPosition += move;
        }
    }

    private void HandleEdgePan()
    {
        Vector2 mouse = Input.mousePosition;
        float w = Screen.width;
        float h = Screen.height;
        float zone = Mathf.Max(1f, edgeZonePixels);

        // 计算每个方向的“贴边程度”[0..1]
        float leftT   = Mathf.Clamp01((zone - mouse.x) / zone);
        float rightT  = Mathf.Clamp01((mouse.x - (w - zone)) / zone);
        float bottomT = Mathf.Clamp01((zone - mouse.y) / zone);
        float topT    = Mathf.Clamp01((mouse.y - (h - zone)) / zone);

        // 加速曲线
        leftT   = edgeAccel.Evaluate(leftT);
        rightT  = edgeAccel.Evaluate(rightT);
        bottomT = edgeAccel.Evaluate(bottomT);
        topT    = edgeAccel.Evaluate(topT);

        // 合成方向（可同时对角）
        float xDir = rightT - leftT;
        float yDir = topT - bottomT;

        // 执行阶段禁止水平方向的边缘卷屏
        if (_inExecute && restrictHorizontalInExecute) xDir = 0f;

        if (Mathf.Abs(xDir) > 0.0001f || Mathf.Abs(yDir) > 0.0001f)
        {
            Vector3 dir = new Vector3(xDir, yDir, 0f);
            float strength = Mathf.Clamp01(Mathf.Max(Mathf.Abs(xDir), Mathf.Abs(yDir)));
            if (dir.sqrMagnitude > 1e-6f) dir.Normalize();

            Vector3 move = dir * (edgeSpeed * strength) * Time.deltaTime;
            _targetPosition += move;
        }
    }
}
