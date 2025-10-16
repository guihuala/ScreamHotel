using ScreamHotel.Presentation;
using UnityEngine;
using Spine;
using Spine.Unity;

[AddComponentMenu("ScreamHotel/AI/Patrol Walker (Spine Ver.)")]
[RequireComponent(typeof(Rigidbody))]
public class PatrolWalker : MonoBehaviour
{
    [Header("Patrol Range")]
    public bool useWidthFromStart = true;
    public float width = 6f;
    public Transform leftPoint;
    public Transform rightPoint;

    [Header("Movement")]
    public float speed = 2f;                              // 基础速度（随机关闭时使用）
    public bool pauseAtEnds = true;
    public Vector2 pauseRange = new Vector2(0.3f, 0.7f);
    public bool startMovingRight = false;

    [Header("Randomize Movement")]
    public bool randomizeStartDirection = false;          // 开：随机起始朝向
    public bool randomizeSpeed = true;                    // 开：速度抖动
    public Vector2 speedRange = new Vector2(1.2f, 2.6f);  // 速度范围
    public Vector2 shuffleIntervalRange = new Vector2(1.5f, 3.5f);
    public bool randomMidPause = true;
    [Tooltip("每秒发生一次中途停顿的概率")]
    [Range(0f, 1f)] public float midPausePerSecond = 0.12f;
    public Vector2 midPauseRange = new Vector2(0.15f, 0.45f);
    public bool randomTurnWithoutObstacle = true;
    [Tooltip("每秒发生一次“无障碍掉头”的概率")]
    [Range(0f, 1f)] public float turnPerSecond = 0.04f;

    [Header("Facing")]
    public bool rotateYInsteadOfScale = true;
    public Transform visualRoot;

    [Header("Collision Layers")]
    public LayerMask groundMask;
    public LayerMask obstacleMask;

    [Header("Sensors")]
    public float edgeCheckDistance = 0.6f;
    public float wallCheckDistance = 0.3f;
    public Vector3 groundRayOffset = new Vector3(0f, 0.1f, 0f);
    public Vector3 forwardRayOffset = new Vector3(0f, 0.2f, 0f);

    [Header("Stability")]
    public float turnCooldown = 0.3f;

    [Header("Spine Animation")]
    public SkeletonAnimation spineAnim;
    public string walkAnim = "walk";
    public string idleAnim = "idle";

    [Header("Debug")]
    public bool drawGizmos = true;

    Rigidbody rb;
    float _leftX, _rightX;
    bool _movingRight;
    float _pauseTimer;
    float _turnTimer;
    Vector3 _startPos;
    Vector3 _initialScale;
    Quaternion _initialRot;

    // 动画可用性
    bool _hasWalk;
    bool _isWalking;

    // 随机移动状态
    float _curSpeed;
    float _shuffleTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        if (!visualRoot) visualRoot = transform;
        _initialScale = visualRoot.localScale;
        _initialRot = visualRoot.localRotation;

        _hasWalk = GetComponent<GuestView>() != null;
    }

    void Start()
    {
        _startPos = transform.position;

        if (useWidthFromStart)
        {
            float half = Mathf.Abs(width) * 0.5f;
            _leftX = _startPos.x - half;
            _rightX = _startPos.x + half;
        }
        else
        {
            float l = leftPoint ? leftPoint.position.x : _startPos.x - 3f;
            float r = rightPoint ? rightPoint.position.x : _startPos.x + 3f;
            _leftX = Mathf.Min(l, r);
            _rightX = Mathf.Max(l, r);
        }

        _movingRight = randomizeStartDirection ? (Random.value < 0.5f) : startMovingRight;
        ApplyFacing(_movingRight ? +1 : -1);

        // 初始速度&洗牌计时
        _curSpeed = GetNextSpeed();
        _shuffleTimer = GetShuffleInterval();
        
        PlayIdle();
    }

    void FixedUpdate()
    {
        // 处理暂停
        if (_pauseTimer > 0f)
        {
            _pauseTimer -= Time.fixedDeltaTime;
            if (_isWalking) PlayIdleIfAvailable();
            return;
        }

        // 随机速度洗牌
        _shuffleTimer -= Time.fixedDeltaTime;
        if (randomizeSpeed && _shuffleTimer <= 0f)
        {
            _curSpeed = GetNextSpeed();
            _shuffleTimer = GetShuffleInterval();
        }

        _turnTimer -= Time.fixedDeltaTime;
        int dir = _movingRight ? +1 : -1;

        // 区间边界
        if ((_movingRight && transform.position.x >= _rightX) ||
            (!_movingRight && transform.position.x <= _leftX))
        {
            TurnAround(true);
            return;
        }

        // 碰撞检测
        if (_turnTimer <= 0f && ShouldTurn(dir))
        {
            TurnAround(false);
            return;
        }

        // 随机中途停顿
        if (randomMidPause && Random.value < midPausePerSecond * Time.fixedDeltaTime)
        {
            _pauseTimer = Random.Range(midPauseRange.x, midPauseRange.y);
            PlayIdleIfAvailable();
            return;
        }

        // 无障碍随机掉头
        if (randomTurnWithoutObstacle && _turnTimer <= 0f &&
            Random.value < turnPerSecond * Time.fixedDeltaTime)
        {
            TurnAround(false);
            return;
        }

        // 位移
        float useSpeed = randomizeSpeed ? _curSpeed : speed;
        Vector3 move = new Vector3(dir * useSpeed * Time.fixedDeltaTime, 0f, 0f);
        rb.MovePosition(rb.position + move);
        ApplyFacing(dir);

        if (!_isWalking) PlayWalkIfAvailable();
    }

    bool ShouldTurn(int dir)
    {
        Vector3 edgeOrigin = transform.position + groundRayOffset + new Vector3(dir * edgeCheckDistance, 0f, 0f);
        bool hasGround = Physics.Raycast(edgeOrigin, Vector3.down, 1.2f, groundMask);
        if (!hasGround) return true;

        Vector3 wallOrigin = transform.position + forwardRayOffset;
        bool hitWall = Physics.Raycast(wallOrigin, new Vector3(dir, 0f, 0f), wallCheckDistance, obstacleMask);
        return hitWall;
    }

    void TurnAround(bool reachedEnd)
    {
        _movingRight = !_movingRight;
        ApplyFacing(_movingRight ? +1 : -1);
        _turnTimer = turnCooldown;

        if (pauseAtEnds && reachedEnd)
        {
            _pauseTimer = Random.Range(pauseRange.x, pauseRange.y);
            PlayIdleIfAvailable();
        }
    }

    void ApplyFacing(int dir)
    {
        if (rotateYInsteadOfScale)
        {
            visualRoot.localRotation = Quaternion.Euler(
                _initialRot.eulerAngles.x,
                dir == -1 ? 0f : 180f,
                _initialRot.eulerAngles.z
            );
        }
        else
        {
            Vector3 s = _initialScale;
            s.x = Mathf.Abs(s.x) * (dir == -1 ? 1f : -1f);
            visualRoot.localScale = s;
        }
    }

    // ---------- Spine 动画控制 ----------
    void PlayWalkIfAvailable()
    {
        if (!_hasWalk || spineAnim == null) return;
        spineAnim.AnimationState.SetAnimation(0, walkAnim, true);
        _isWalking = true;
    }

    void PlayIdleIfAvailable()
    {
        if (spineAnim == null) { _isWalking = false; return; }
        spineAnim.AnimationState.SetAnimation(0, idleAnim, true);
        _isWalking = false;
    }

    void PlayIdle() => PlayIdleIfAvailable();
    // ---------------------------------------------------

    // 速度/洗牌周期
    float GetNextSpeed()
    {
        if (!randomizeSpeed) return speed;
        float min = Mathf.Min(speedRange.x, speedRange.y);
        float max = Mathf.Max(speedRange.x, speedRange.y);
        return Random.Range(min, max);
    }

    float GetShuffleInterval()
    {
        float min = Mathf.Max(0.05f, Mathf.Min(shuffleIntervalRange.x, shuffleIntervalRange.y));
        float max = Mathf.Max(shuffleIntervalRange.x, shuffleIntervalRange.y);
        return Random.Range(min, max);
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Gizmos.color = Color.yellow;
        float left = _leftX, right = _rightX;
        if (!Application.isPlaying)
        {
            // 编辑器下的可视化预估
            var pos = transform.position;
            if (useWidthFromStart)
            {
                float half = Mathf.Abs(width) * 0.5f;
                left = pos.x - half;
                right = pos.x + half;
            }
            else
            {
                float l = leftPoint ? leftPoint.position.x : pos.x - 3f;
                float r = rightPoint ? rightPoint.position.x : pos.x + 3f;
                left = Mathf.Min(l, r);
                right = Mathf.Max(l, r);
            }
        }
        Gizmos.DrawLine(new Vector3(left, transform.position.y, transform.position.z),
                        new Vector3(right, transform.position.y, transform.position.z));

        int dir = _movingRight ? +1 : -1;
        Vector3 edgeOrigin = transform.position + groundRayOffset + new Vector3(dir * edgeCheckDistance, 0f, 0f);
        Vector3 wallOrigin = transform.position + forwardRayOffset;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(edgeOrigin, edgeOrigin + Vector3.down * 1.2f);
        Gizmos.DrawLine(wallOrigin, wallOrigin + new Vector3(dir * wallCheckDistance, 0f, 0f));
    }
}
