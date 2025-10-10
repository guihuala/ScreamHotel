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
    public float speed = 2f;
    public bool pauseAtEnds = true;
    public Vector2 pauseRange = new Vector2(0.3f, 0.7f);
    public bool startMovingRight = false;

    [Header("Facing")]
    public bool rotateYInsteadOfScale = true;
    public Transform visualRoot;
    public bool lockOnlyYRotation = true;

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

    bool _isWalking;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        if (!visualRoot) visualRoot = transform;
        _initialScale = visualRoot.localScale;
        _initialRot = visualRoot.localRotation;

        // 自动查找 SkeletonAnimation
        if (!spineAnim)
            spineAnim = GetComponentInChildren<SkeletonAnimation>();
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

        _movingRight = startMovingRight;
        ApplyFacing(_movingRight ? +1 : -1);

        PlayIdle();
    }

    void FixedUpdate()
    {
        if (_pauseTimer > 0f)
        {
            _pauseTimer -= Time.fixedDeltaTime;
            if (_isWalking) PlayIdle();
            return;
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

        // 位移
        Vector3 move = new Vector3(dir * speed * Time.fixedDeltaTime, 0f, 0f);
        rb.MovePosition(rb.position + move);
        ApplyFacing(dir);

        if (!_isWalking) PlayWalk();
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
            PlayIdle();
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
    void PlayWalk()
    {
        if (spineAnim == null) return;
        spineAnim.AnimationState.SetAnimation(0, walkAnim, true);
        _isWalking = true;
    }

    void PlayIdle()
    {
        if (spineAnim == null) return;
        spineAnim.AnimationState.SetAnimation(0, idleAnim, true);
        _isWalking = false;
    }
    // ----------------------------------

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(_leftX, transform.position.y, transform.position.z),
                        new Vector3(_rightX, transform.position.y, transform.position.z));

        int dir = _movingRight ? +1 : -1;
        Vector3 edgeOrigin = transform.position + groundRayOffset + new Vector3(dir * edgeCheckDistance, 0f, 0f);
        Vector3 wallOrigin = transform.position + forwardRayOffset;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(edgeOrigin, edgeOrigin + Vector3.down * 1.2f);
        Gizmos.DrawLine(wallOrigin, wallOrigin + new Vector3(dir * wallCheckDistance, 0f, 0f));
    }
}