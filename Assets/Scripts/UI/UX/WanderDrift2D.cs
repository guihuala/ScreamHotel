using UnityEngine;

/// <summary>
/// 在 XY 平面内于一个矩形框内做平滑随机游走的“飘动”效果。
/// 挂到实体（含 Rigidbody）的同一物体上；
/// 若刚体在拖拽期间被设为 kinematic（由你的拖拽基类处理），本脚本会自动暂停运动。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class WanderDrift2D : MonoBehaviour
{
    [Header("活动范围（世界坐标，XY 平面）")]
    public Vector2 center = Vector2.zero;
    public Vector2 size = new Vector2(8f, 5f);
    [Tooltip("锁定 Z 坐标")]
    public float zLock = 0f;

    [Header("速度/转向")]
    [Tooltip("基础移动速度（单位/秒）")]
    public float baseSpeed = 1.2f;
    [Tooltip("向目标点转向的平滑度（0~1，越大越灵敏）")]
    [Range(0f, 1f)] public float steering = 0.12f;

    [Header("目标刷新")]
    [Tooltip("抵达目标的判定半径")]
    public float arriveRadius = 0.2f;
    [Tooltip("每次到达后，新的目标将随机在范围内生成")]
    public Vector2 minMaxPause = new Vector2(0.2f, 0.6f);

    [Header("自然抖动（可选）")]
    [Tooltip("Perlin 噪声强度，用于让速度有轻微起伏")]
    public float noiseAmp = 0.25f;
    [Tooltip("Perlin 噪声时间缩放")]
    public float noiseFreq = 0.6f;

    [Header("旋转（可选）")]
    [Tooltip("是否使朝向与移动方向一致（2D/看板式物体可开）")]
    public bool faceMoveDir = false;
    [Tooltip("朝向插值速度")]
    public float rotateLerp = 8f;

    Rigidbody _rb;
    Vector2 _target;
    float _pauseTimer;
    float _seedX, _seedY;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
        _seedX = Random.value * 1000f;
        _seedY = Random.value * 1000f;
        PickNewTarget(true);
    }

    void FixedUpdate()
    {
        // 拖拽期间：你的拖拽基类会把刚体设置为 isKinematic=true，此时自动暂停移动
        if (_rb.isKinematic)
        {
            _rb.velocity = Vector3.zero;
            return;
        }

        // 暂停计时（到达后的小停顿）
        if (_pauseTimer > 0f)
        {
            _pauseTimer -= Time.fixedDeltaTime;
            _rb.velocity = Vector3.zero;
            return;
        }

        Vector2 pos = new Vector2(transform.position.x, transform.position.y);
        Vector2 to = _target - pos;
        float dist = to.magnitude;

        if (dist <= arriveRadius)
        {
            PickNewTarget();
            return;
        }

        // 方向（带平滑转向）
        Vector2 dir = to / Mathf.Max(0.0001f, dist);
        Vector3 curVel = _rb.velocity;
        Vector2 vel2 = Vector2.Lerp(new Vector2(curVel.x, curVel.y), dir * SpeedWithNoise(), steering);

        // 边界反弹（避免贴边慢慢“挤”出去）
        Vector2 half = size * 0.5f;
        Vector2 min = center - half;
        Vector2 max = center + half;

        Vector2 nextPos = pos + vel2 * Time.fixedDeltaTime;
        if (nextPos.x < min.x || nextPos.x > max.x) vel2.x = -vel2.x;
        if (nextPos.y < min.y || nextPos.y > max.y) vel2.y = -vel2.y;

        // 应用速度与 Z 锁定
        _rb.velocity = new Vector3(vel2.x, vel2.y, 0f);
        var p = transform.position;
        if (!Mathf.Approximately(p.z, zLock)) transform.position = new Vector3(p.x, p.y, zLock);

        // 可选：朝向移动方向
        if (faceMoveDir && vel2.sqrMagnitude > 0.0001f)
        {
            var forward = new Vector3(vel2.x, vel2.y, 0f).normalized;
            var rot = Quaternion.LookRotation(Vector3.forward, forward); // Z朝屏幕外的2D看板
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.fixedDeltaTime * rotateLerp);
        }
    }

    void PickNewTarget(bool snapInside = false)
    {
        Vector2 half = size * 0.5f;
        _target = new Vector2(
            Random.Range(center.x - half.x, center.x + half.x),
            Random.Range(center.y - half.y, center.y + half.y)
        );

        if (!snapInside) _pauseTimer = Random.Range(minMaxPause.x, minMaxPause.y);
        else _pauseTimer = 0f;
    }

    float SpeedWithNoise()
    {
        if (noiseAmp <= 0f) return baseSpeed;
        float nx = Mathf.PerlinNoise(Time.time * noiseFreq, _seedX);
        float ny = Mathf.PerlinNoise(Time.time * noiseFreq, _seedY);
        float n = (nx + ny) * 0.5f; // 0..1
        return baseSpeed * (1f + (n - 0.5f) * 2f * noiseAmp);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0,1,1,0.35f);
        Vector3 c = new Vector3(center.x, center.y, zLock);
        Gizmos.DrawWireCube(c, new Vector3(size.x, size.y, 0.01f));
    }
#endif
}
