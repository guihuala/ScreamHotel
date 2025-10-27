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

    private Camera _cam;
    private Vector3 _targetPosition;
    private float _fixedZ;

    private Coroutine _shakeRoutine;
    private bool _playerControlEnabled = true;
    
    public void SetPlayerControl(bool enabled)
    {
        _playerControlEnabled = enabled;
    }

    void Awake()
    {
        _cam = GetComponent<Camera>();
        if (_cam == null) _cam = Camera.main;

        _fixedZ = transform.position.z;
        _targetPosition = transform.position;

        // 监听游戏阶段变化（保留）
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

        }
    }

    void Update()
    {
        if (!_playerControlEnabled)
        {
            return;
        }

        HandleKeyboard();
        if (enableEdgePan && (!ignoreEdgeWhenUnfocused || Application.isFocused))
        {
            HandleEdgePan();
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