using UnityEngine;

public class DriftSpawner : MonoBehaviour
{
    [Header("预制体")]
    public GameObject draggablePrefab;

    [Header("数量与范围")]
    public int count = 6;
    public Vector2 center = Vector2.zero;
    public Vector2 size = new Vector2(8f, 5f);
    public float zLock = 0f;

    [Header("漂移动画默认参数")]
    public float baseSpeed = 1.2f;
    public float noiseAmp = 0.25f;

    void Start()
    {
        if (!draggablePrefab || count <= 0) return;
        Vector2 half = size * 0.5f;

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(center.x - half.x, center.x + half.x),
                Random.Range(center.y - half.y, center.y + half.y),
                zLock
            );

            var go = Instantiate(draggablePrefab, pos, Quaternion.identity, transform);

            // 加上 WanderDrift2D（若尚未添加）
            var drift = go.GetComponent<WanderDrift2D>();
            if (!drift) drift = go.AddComponent<WanderDrift2D>();

            drift.center = center;
            drift.size = size;
            drift.zLock = zLock;
            drift.baseSpeed = baseSpeed;
            drift.noiseAmp = noiseAmp;

            // 确保有刚体（拖拽基类在拖拽时会把刚体设为 kinematic，自动暂停游走）
            var rb = go.GetComponent<Rigidbody>();
            if (!rb) rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1,1,0,0.3f);
        Gizmos.DrawWireCube(new Vector3(center.x, center.y, zLock), new Vector3(size.x, size.y, 0.01f));
    }
#endif
}