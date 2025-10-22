using UnityEngine;

public class MouseDriftCamera : MonoBehaviour
{
    public enum FollowMode { ScreenCenterOffset, MouseDelta }

    [Header("基础")]
    public FollowMode mode = FollowMode.ScreenCenterOffset;
    public bool useUnscaledTime = true;
    public bool ignoreWhenUnfocused = true;

    [Header("启用项")]
    public bool enablePosition = true;
    public bool enableRotation = true;

    [Header("位置微移")]
    [Tooltip("相机可偏移的最大距离（本地单位）")]
    public float maxOffset = 0.5f;
    [Tooltip("屏幕中心偏移模式：单位像素到世界偏移的缩放")]
    public float centerPosSensitivity = 0.003f;
    [Tooltip("鼠标增量模式：单位像素到世界偏移的缩放")]
    public float deltaPosSensitivity = 0.02f;
    [Tooltip("位置平滑时间（秒）")]
    public float positionSmoothTime = 0.12f;

    [Header("旋转（微倾斜）")]
    [Tooltip("最大俯仰角（度），正数，鼠标上↑默认让相机微向上看")]
    public float maxPitch = 2f;
    [Tooltip("最大偏航角（度），正数，鼠标右→默认让相机微向右看")]
    public float maxYaw = 2f;
    [Tooltip("最大滚转角（度），正数，鼠标右→默认让相机右肩下沉一点")]
    public float maxRoll = 1f;

    [Tooltip("屏幕中心偏移模式：像素→角度 的缩放（Pitch/Yaw/Roll 各自乘以该值）")]
    public float centerRotSensitivity = 0.05f;
    [Tooltip("鼠标增量模式：像素→角度 的缩放（Pitch/Yaw/Roll 各自乘以该值）")]
    public float deltaRotSensitivity = 0.2f;

    [Tooltip("旋转平滑时间（秒）")]
    public float rotationSmoothTime = 0.12f;

    [Header("旋转细节")]
    [Tooltip("是否反向俯仰（例如把鼠标上↑变为相机向下看）")]
    public bool invertPitch = false;
    [Tooltip("是否反向偏航")]
    public bool invertYaw = false;
    [Tooltip("是否反向滚转")]
    public bool invertRoll = false;

    [Tooltip("以屏幕中心为参考的死区（0~0.5），避免在中心附近轻微抖动就触发旋转/位移")]
    [Range(0f, 0.5f)] public float centerDeadZone = 0.02f;

    [Header("回中")]
    [Tooltip("在无输入或失焦时缓慢回到初始姿态")]
    public bool returnToOrigin = true;

    // 内部状态
    Vector3 _baseLocalPos;
    Quaternion _baseLocalRot;
    Vector3 _posVelocity;                 // 位置 SmoothDamp
    float _rotVelX, _rotVelY, _rotVelZ;   // 旋转 SmoothDampAngle
    Vector3 _targetOffset;                // 目标位移
    Vector3 _targetAngles;                // 目标角度增量(Euler)，相对 base
    Vector2 _prevMouse;
    Vector2 _mouseDeltaFiltered;

    void OnEnable()
    {
        _baseLocalPos = transform.localPosition;
        _baseLocalRot = transform.localRotation;
        _prevMouse = Input.mousePosition;
        _targetOffset = Vector3.zero;
        _targetAngles = Vector3.zero;
    }

    void Update()
    {
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        if (ignoreWhenUnfocused && !Application.isFocused)
        {
            LerpToTargets(Vector3.zero, Vector3.zero, dt);
            _prevMouse = Input.mousePosition;
            return;
        }

        Vector2 screen = new Vector2(Screen.width, Screen.height);
        Vector2 mouse = (Vector2)Input.mousePosition;

        // —— 计算“方向/增量”基量 ——
        Vector2 fromCenter = (mouse - screen * 0.5f);
        Vector2 normCenter = new Vector2(
            Mathf.Clamp(fromCenter.x / Mathf.Max(1f, screen.x * 0.5f), -1f, 1f),
            Mathf.Clamp(fromCenter.y / Mathf.Max(1f, screen.y * 0.5f), -1f, 1f)
        );

        // 死区（以归一化坐标计）
        if (mode == FollowMode.ScreenCenterOffset)
        {
            normCenter.x = ApplyDeadZone(normCenter.x, centerDeadZone);
            normCenter.y = ApplyDeadZone(normCenter.y, centerDeadZone);
        }

        Vector2 rawDelta = mouse - _prevMouse;
        _mouseDeltaFiltered = Vector2.Lerp(_mouseDeltaFiltered, rawDelta, 0.35f); // 简单低通
        _prevMouse = mouse;

        // —— 计算目标位移 —— 
        if (enablePosition)
        {
            if (mode == FollowMode.ScreenCenterOffset)
            {
                // 像素 → 本地位移
                Vector3 delta = new Vector3(
                    normCenter.x * screen.x * centerPosSensitivity,
                    normCenter.y * screen.y * centerPosSensitivity,
                    0f
                );
                _targetOffset = Vector3.ClampMagnitude(delta, maxOffset);
            }
            else // MouseDelta
            {
                _targetOffset += new Vector3(
                    _mouseDeltaFiltered.x * deltaPosSensitivity,
                    _mouseDeltaFiltered.y * deltaPosSensitivity,
                    0f
                );
                _targetOffset.x = Mathf.Clamp(_targetOffset.x, -maxOffset, maxOffset);
                _targetOffset.y = Mathf.Clamp(_targetOffset.y, -maxOffset, maxOffset);
            }
        }
        else if (returnToOrigin)
        {
            _targetOffset = Vector3.Lerp(_targetOffset, Vector3.zero, 0.1f);
        }

        // —— 计算目标旋转（欧拉角增量）——
        if (enableRotation)
        {
            float sx = (mode == FollowMode.ScreenCenterOffset ? centerRotSensitivity : deltaRotSensitivity);
            // 以“上/右”为正方向，通过是否 invert 反向
            float pitchSign = invertPitch ? 1f : -1f; // 常用“鼠标上→相机向上看”，保持默认 -1
            float yawSign   = invertYaw   ? -1f :  1f;
            float rollSign  = invertRoll  ? -1f :  1f;

            if (mode == FollowMode.ScreenCenterOffset)
            {
                float pitch = Mathf.Clamp(normCenter.y * sx * maxPitch, -maxPitch, maxPitch) * pitchSign;
                float yaw   = Mathf.Clamp(normCenter.x * sx * maxYaw,   -maxYaw,   maxYaw)   * yawSign;
                float roll  = Mathf.Clamp(normCenter.x * sx * maxRoll,  -maxRoll,  maxRoll)  * rollSign;

                _targetAngles = new Vector3(pitch, yaw, roll);
            }
            else // MouseDelta
            {
                _targetAngles.x += Mathf.Clamp(_mouseDeltaFiltered.y * sx, -maxPitch, maxPitch) * pitchSign;
                _targetAngles.y += Mathf.Clamp(_mouseDeltaFiltered.x * sx, -maxYaw,   maxYaw)   * yawSign;
                _targetAngles.z += Mathf.Clamp(_mouseDeltaFiltered.x * sx, -maxRoll,  maxRoll)  * rollSign;

                // 限制整体范围
                _targetAngles.x = Mathf.Clamp(_targetAngles.x, -maxPitch, maxPitch);
                _targetAngles.y = Mathf.Clamp(_targetAngles.y, -maxYaw,   maxYaw);
                _targetAngles.z = Mathf.Clamp(_targetAngles.z, -maxRoll,  maxRoll);
            }
        }
        else if (returnToOrigin)
        {
            _targetAngles = Vector3.Lerp(_targetAngles, Vector3.zero, 0.1f);
        }

        // —— 应用到 Transform（带平滑）——
        LerpToTargets(_targetOffset, _targetAngles, dt);

        // 若需要空闲回中（中心模式下）
        if (returnToOrigin && mode == FollowMode.ScreenCenterOffset && rawDelta.sqrMagnitude < 0.01f)
        {
            _targetOffset = Vector3.Lerp(_targetOffset, Vector3.zero, 0.04f);
            _targetAngles = Vector3.Lerp(_targetAngles, Vector3.zero, 0.04f);
        }
    }

    void LerpToTargets(Vector3 targetOffset, Vector3 targetAngles, float dt)
    {
        // 位置
        Vector3 desiredPos = _baseLocalPos + targetOffset;
        transform.localPosition = Vector3.SmoothDamp(
            transform.localPosition,
            desiredPos,
            ref _posVelocity,
            Mathf.Max(0.0001f, positionSmoothTime),
            Mathf.Infinity,
            dt
        );

        // 旋转（相对 base 的欧拉角叠加，分轴平滑）
        Vector3 baseEuler = _baseLocalRot.eulerAngles;
        float tx = Mathf.Repeat(baseEuler.x + targetAngles.x + 540f, 360f) - 180f;
        float ty = Mathf.Repeat(baseEuler.y + targetAngles.y + 540f, 360f) - 180f;
        float tz = Mathf.Repeat(baseEuler.z + targetAngles.z + 540f, 360f) - 180f;

        Vector3 curEuler = transform.localRotation.eulerAngles;
        float newX = Mathf.SmoothDampAngle(curEuler.x, tx, ref _rotVelX, Mathf.Max(0.0001f, rotationSmoothTime), Mathf.Infinity, dt);
        float newY = Mathf.SmoothDampAngle(curEuler.y, ty, ref _rotVelY, Mathf.Max(0.0001f, rotationSmoothTime), Mathf.Infinity, dt);
        float newZ = Mathf.SmoothDampAngle(curEuler.z, tz, ref _rotVelZ, Mathf.Max(0.0001f, rotationSmoothTime), Mathf.Infinity, dt);

        transform.localRotation = Quaternion.Euler(newX, newY, newZ);
    }

    float ApplyDeadZone(float v, float dz)
    {
        float a = Mathf.Abs(v);
        if (a <= dz) return 0f;
        // 重新映射到 [0,1]
        float n = (a - dz) / (1f - dz);
        return Mathf.Sign(v) * n;
    }
}
