using UnityEngine;

[DisallowMultipleComponent]
public class CameraController : MonoBehaviour
{
    [Header("移动速度")]
    public float keyboardSpeed = 10f;   // 键盘移动速度（单位/秒）
    public float dragFollowLerp = 20f;  // 拖拽时平滑系数

    [Header("边界 (X)")]
    public bool enableBounds = true;
    public float minX = -50f;
    public float maxX = 50f;

    [Header("拖拽设置(透视与正交通用)")]
    public int dragMouseButton = 1;     // 1=右键，2=中键
    public float dragPlaneY = 0f;       // 鼠标射线命中的水平面 y 值

    private Camera _cam;
    private float _targetX;
    private float _fixedY, _fixedZ;
    private bool _dragging = false;
    private Vector3 _dragOriginOnPlane; // 开始拖拽时的鼠标在平面上的世界坐标

    void Awake()
    {
        _cam = GetComponent<Camera>();
        if (_cam == null) _cam = Camera.main;

        _fixedY = transform.position.y;
        _fixedZ = transform.position.z;
        _targetX = transform.position.x;
    }

    void Update()
    {
        HandleKeyboard();
        HandleDrag();

        if (enableBounds) _targetX = Mathf.Clamp(_targetX, minX, maxX);

        // 平滑应用位置（只改 X，锁定 Y/Z）
        float newX = Mathf.Lerp(transform.position.x, _targetX, Time.deltaTime * dragFollowLerp);
        transform.position = new Vector3(newX, _fixedY, _fixedZ);
    }

    private void HandleKeyboard()
    {
        float h = Input.GetAxisRaw("Horizontal"); // A/D 或 ←/→
        if (Mathf.Abs(h) > 0.0001f)
        {
            _targetX += h * keyboardSpeed * Time.deltaTime;
        }
    }

    private void HandleDrag()
    {
        if (Input.GetMouseButtonDown(dragMouseButton))
        {
            _dragging = true;
            _dragOriginOnPlane = MouseOnPlane(dragPlaneY);
        }
        if (Input.GetMouseButtonUp(dragMouseButton))
        {
            _dragging = false;
        }
        if (_dragging)
        {
            Vector3 now = MouseOnPlane(dragPlaneY);
            Vector3 delta = _dragOriginOnPlane - now;
            _targetX += delta.x; // 只取 X 方向的位移
            _dragOriginOnPlane = now; // 连续拖拽：更新参考点
        }
    }

    // 将鼠标位置投射到 y=planeY 的水平面上，返回世界坐标
    private Vector3 MouseOnPlane(float planeY)
    {
        var ray = _cam.ScreenPointToRay(Input.mousePosition);
        var plane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f)); // y=planeY
        if (plane.Raycast(ray, out float enter))
            return ray.GetPoint(enter);
        // 兜底：回当前相机位置（只会影响极端情况）
        return new Vector3(_targetX, _fixedY, _fixedZ);
    }

#if UNITY_EDITOR
    // 场景视图中画出 X 边界
    private void OnDrawGizmosSelected()
    {
        if (!enableBounds) return;
        Gizmos.color = Color.cyan;
        var y = Application.isPlaying ? _fixedY : transform.position.y;
        var z = Application.isPlaying ? _fixedZ : transform.position.z;
        Gizmos.DrawLine(new Vector3(minX, y, z), new Vector3(maxX, y, z));
    }
#endif
}