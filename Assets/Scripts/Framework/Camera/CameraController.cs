using UnityEngine;
using ScreamHotel.Core;
using System.Collections;

[DisallowMultipleComponent]
public class CameraController : MonoBehaviour
{
    [Header("移动")]
    public float keyboardSpeed = 10f;
    public float positionLerp = 12f;

    [Header("边界 (XY)")]
    public bool enableBounds = true;
    public float minX = -50f;
    public float maxX = 50f;
    public float minY = -30f;
    public float maxY = 30f;

    [Header("屏幕边缘移动")]
    public bool enableEdgePan = true;
    public float edgeZonePixels = 24f;
    public float edgeSpeed = 10f;
    public AnimationCurve edgeAccel = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public bool ignoreEdgeWhenUnfocused = true;

    [Header("其它")]
    public bool lockZ = true;

    [Header("震动效果")]
    public float shakeDuration = 1.5f;
    public float shakeStrength = 0.3f;
    public float shakeFrequency = 20f;

    private Camera _cam;
    private Vector3 _targetPosition;
    private float _fixedZ;

    private Coroutine _shakeRoutine;
    private Vector3 _originalPos;

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
        if (evt.State is GameState state && state == GameState.NightExecute)
        {
            // 进入夜间执行时震动相机
            if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
            _shakeRoutine = StartCoroutine(ShakeCamera());
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

            // 随机方向震动
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

        if (enableBounds)
        {
            _targetPosition.x = Mathf.Clamp(_targetPosition.x, minX, maxX);
            _targetPosition.y = Mathf.Clamp(_targetPosition.y, minY, maxY);
        }

        Vector3 desired = _targetPosition;
        if (lockZ) desired.z = _fixedZ;

        if (positionLerp > 0f)
            transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * positionLerp);
        else
            transform.position = desired;
    }

    private void HandleKeyboard()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
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

        float leftT = Mathf.Clamp01((zone - mouse.x) / zone);
        float rightT = Mathf.Clamp01((mouse.x - (w - zone)) / zone);
        float bottomT = Mathf.Clamp01((zone - mouse.y) / zone);
        float topT = Mathf.Clamp01((mouse.y - (h - zone)) / zone);

        leftT = edgeAccel.Evaluate(leftT);
        rightT = edgeAccel.Evaluate(rightT);
        bottomT = edgeAccel.Evaluate(bottomT);
        topT = edgeAccel.Evaluate(topT);

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
