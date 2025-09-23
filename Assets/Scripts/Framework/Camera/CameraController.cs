using UnityEngine;

[DisallowMultipleComponent]
public class CameraController : MonoBehaviour
{
    [Header("移动速度")]
    public float keyboardSpeed = 10f;   // 键盘移动速度（单位/秒）
    public float dragFollowLerp = 20f;  // 拖拽时平滑系数

    [Header("边界 (XY)")]
    public bool enableBounds = true;
    public float minX = -50f;
    public float maxX = 50f;
    public float minY = -30f;
    public float maxY = 30f;

    [Header("拖拽设置")]
    public int dragMouseButton = 1;     // 1=右键，2=中键
    public float dragPlaneZ = 0f;       // 鼠标射线命中的平面 Z 值（用于XY平面移动）

    private Camera _cam;
    private Vector3 _targetPosition;
    private float _fixedZ;
    private bool _dragging = false;
    private Vector3 _dragOriginOnPlane; // 开始拖拽时的鼠标在平面上的世界坐标

    void Awake()
    {
        _cam = GetComponent<Camera>();
        if (_cam == null) _cam = Camera.main;

        _fixedZ = transform.position.z;
        _targetPosition = transform.position;
    }

    void Update()
    {
        HandleKeyboard();
        HandleDrag();

        if (enableBounds)
        {
            _targetPosition.x = Mathf.Clamp(_targetPosition.x, minX, maxX);
            _targetPosition.y = Mathf.Clamp(_targetPosition.y, minY, maxY);
        }

        // 平滑应用位置（只改 XY，锁定 Z）
        Vector3 newPosition = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * dragFollowLerp);
        newPosition.z = _fixedZ; // 保持Z轴固定
        transform.position = newPosition;
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

    private void HandleDrag()
    {
        if (Input.GetMouseButtonDown(dragMouseButton))
        {
            _dragging = true;
            _dragOriginOnPlane = MouseOnPlane(dragPlaneZ);
        }
        if (Input.GetMouseButtonUp(dragMouseButton))
        {
            _dragging = false;
        }
        if (_dragging)
        {
            Vector3 now = MouseOnPlane(dragPlaneZ);
            Vector3 delta = _dragOriginOnPlane - now;
            _targetPosition += new Vector3(delta.x, delta.y, 0f); // 取 XY 方向的位移
            _dragOriginOnPlane = now;
        }
    }

    // 将鼠标位置投射到 z=dragPlaneZ 的XY平面上，返回世界坐标
    private Vector3 MouseOnPlane(float planeZ)
    {
        var ray = _cam.ScreenPointToRay(Input.mousePosition);
        var plane = new Plane(Vector3.forward, new Vector3(0f, 0f, planeZ)); // z=planeZ 的XY平面
        if (plane.Raycast(ray, out float enter))
            return ray.GetPoint(enter);
        // 兜底：回当前目标位置
        return _targetPosition;
    }

#if UNITY_EDITOR
    // 场景视图中画出 XY 边界
    private void OnDrawGizmosSelected()
    {
        if (!enableBounds) return;
        Gizmos.color = Color.cyan;
        
        float z = Application.isPlaying ? _fixedZ : transform.position.z;
        
        // 绘制边界矩形
        Vector3 bottomLeft = new Vector3(minX, minY, z);
        Vector3 bottomRight = new Vector3(maxX, minY, z);
        Vector3 topLeft = new Vector3(minX, maxY, z);
        Vector3 topRight = new Vector3(maxX, maxY, z);
        
        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);
        
        // 绘制对角线便于观察
        Gizmos.DrawLine(bottomLeft, topRight);
        Gizmos.DrawLine(bottomRight, topLeft);
    }
#endif
}