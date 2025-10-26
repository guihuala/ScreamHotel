using UnityEngine;
using Spine.Unity;

[RequireComponent(typeof(Rigidbody))]
public class QueueAutoWalker : MonoBehaviour
{
    [Header("Target")]
    public float targetX;
    public float stopEpsilon = 0.02f;

    [Header("Movement")]
    public float speed = 2.0f;

    [Header("Spine Animation")]
    public SkeletonAnimation spineAnim;
    public string walkAnim = "walk";
    public string idleAnim = "idle";

    private Rigidbody rb;
    private bool _arrived;
    private bool _isWalking;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // 确保任何情况下都不会发生旋转
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationY |
                         RigidbodyConstraints.FreezeRotationZ;

        //（可选）插值更顺滑
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (spineAnim == null)
            spineAnim = gameObject.GetComponentInChildren<SkeletonAnimation>();
    }

    /// <summary>
    /// 初始化目标 x 与速度（速度>0）
    /// </summary>
    public void Init(float targetX, float speed)
    {
        this.targetX = targetX;
        this.speed   = Mathf.Max(0.01f, speed);
        _arrived     = false;

        // 出生先 Idle，下一帧开始移动
        PlayIdle();
    }

    void FixedUpdate()
    {
        if (_arrived) return;

        float dx   = targetX - transform.position.x;
        float dist = Mathf.Abs(dx);

        if (dist <= stopEpsilon)
        {
            // 对齐到目标并停下
            Vector3 p = transform.position;
            p.x = targetX;
            rb.MovePosition(p);
            rb.velocity = Vector3.zero;

            PlayIdle();
            _arrived = true;
            return;
        }

        // 沿 X 轴向目标匀速前进（不改变任何旋转）
        float step = Mathf.Sign(dx) * speed * Time.fixedDeltaTime;
        // 防止跨越：若一步超过剩余距离，直接对齐
        if (Mathf.Abs(step) > dist) step = Mathf.Sign(dx) * dist;

        rb.MovePosition(rb.position + new Vector3(step, 0f, 0f));

        if (!_isWalking) PlayWalk();
    }

    private void PlayWalk()
    {
        if (spineAnim != null && !string.IsNullOrEmpty(walkAnim))
            spineAnim.AnimationState.SetAnimation(0, walkAnim, true);
        _isWalking = true;
    }

    private void PlayIdle()
    {
        if (spineAnim != null && !string.IsNullOrEmpty(idleAnim))
            spineAnim.AnimationState.SetAnimation(0, idleAnim, true);
        _isWalking = false;
    }
}

