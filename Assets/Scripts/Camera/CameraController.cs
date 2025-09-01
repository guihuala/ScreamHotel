using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CameraController : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 5f;
    public float keyboardMoveSpeed = 10f;
    public bool edgeScrolling = true;
    public float edgeThreshold = 20f;
    
    [Header("缩放设置")]
    public float zoomSpeed = 5f;
    public float minZoom = 5f;
    public float maxZoom = 20f;
    public float zoomLerpSpeed = 10f;
    
    [Header("边界设置")]
    public bool enableBounds = true;
    public Vector2 minBounds = new Vector2(-50f, -50f);
    public Vector2 maxBounds = new Vector2(50f, 50f);
    
    private Camera _camera;
    private Vector3 _targetPosition;
    private float _targetZoom;
    private Vector3 _dragOrigin;
    private bool _isDragging = false;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        if (_camera == null)
        {
            _camera = Camera.main;
        }
        
        _targetPosition = transform.position;
        _targetZoom = _camera.orthographic ? _camera.orthographicSize : _camera.fieldOfView;
    }

    private void Update()
    {
        HandleKeyboardMovement();
        HandleEdgeScrolling();
        HandleMouseDrag();
        HandleZoom();
        
        ApplyMovement();
        ApplyZoom();
        ClampPosition();
    }

    /// <summary>
    /// 处理键盘移动
    /// </summary>
    private void HandleKeyboardMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        if (horizontal != 0f || vertical != 0f)
        {
            Vector3 movement = new Vector3(horizontal, vertical, 0f) * keyboardMoveSpeed * Time.deltaTime;
            _targetPosition += movement;
        }
    }

    /// <summary>
    /// 处理边缘滚动
    /// </summary>
    private void HandleEdgeScrolling()
    {
        if (!edgeScrolling) return;
        
        Vector3 movement = Vector3.zero;
        Vector2 mousePosition = Input.mousePosition;
        
        // 检查屏幕边缘
        if (mousePosition.x < edgeThreshold)
            movement.x -= 1f;
        if (mousePosition.x > Screen.width - edgeThreshold)
            movement.x += 1f;
        if (mousePosition.y < edgeThreshold)
            movement.y -= 1f;
        if (mousePosition.y > Screen.height - edgeThreshold)
            movement.y += 1f;
        
        if (movement != Vector3.zero)
        {
            movement = movement.normalized * moveSpeed * Time.deltaTime;
            _targetPosition += movement;
        }
    }

    /// <summary>
    /// 处理鼠标拖拽
    /// </summary>
    private void HandleMouseDrag()
    {
        // 开始拖拽
        if (Input.GetMouseButtonDown(2)) // 中键拖拽
        {
            _dragOrigin = GetMouseWorldPosition();
            _isDragging = true;
        }
        
        // 结束拖拽
        if (Input.GetMouseButtonUp(2))
        {
            _isDragging = false;
        }
        
        // 拖拽中
        if (_isDragging)
        {
            Vector3 difference = _dragOrigin - GetMouseWorldPosition();
            _targetPosition += difference;
        }
        
        // 右键拖拽（可选）
        if (Input.GetMouseButtonDown(1))
        {
            _dragOrigin = GetMouseWorldPosition();
            _isDragging = true;
        }
        
        if (Input.GetMouseButtonUp(1))
        {
            _isDragging = false;
        }
    }

    /// <summary>
    /// 处理缩放
    /// </summary>
    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            if (_camera.orthographic)
            {
                _targetZoom -= scroll * zoomSpeed;
                _targetZoom = Mathf.Clamp(_targetZoom, minZoom, maxZoom);
            }
            else
            {
                _targetZoom -= scroll * zoomSpeed;
                _targetZoom = Mathf.Clamp(_targetZoom, minZoom, maxZoom);
            }
        }
    }

    /// <summary>
    /// 应用移动
    /// </summary>
    private void ApplyMovement()
    {
        transform.position = Vector3.Lerp(transform.position, _targetPosition, moveSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 应用缩放
    /// </summary>
    private void ApplyZoom()
    {
        if (_camera.orthographic)
        {
            _camera.orthographicSize = Mathf.Lerp(_camera.orthographicSize, _targetZoom, zoomLerpSpeed * Time.deltaTime);
        }
        else
        {
            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, _targetZoom, zoomLerpSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// 限制位置在边界内
    /// </summary>
    private void ClampPosition()
    {
        if (!enableBounds) return;
        
        // 根据当前缩放级别计算实际边界
        float effectiveMinX = minBounds.x + GetCameraWidth() / 2f;
        float effectiveMaxX = maxBounds.x - GetCameraWidth() / 2f;
        float effectiveMinY = minBounds.y + GetCameraHeight() / 2f;
        float effectiveMaxY = maxBounds.y - GetCameraHeight() / 2f;
        
        _targetPosition.x = Mathf.Clamp(_targetPosition.x, effectiveMinX, effectiveMaxX);
        _targetPosition.y = Mathf.Clamp(_targetPosition.y, effectiveMinY, effectiveMaxY);
    }

    /// <summary>
    /// 获取相机视野宽度（世界单位）
    /// </summary>
    private float GetCameraWidth()
    {
        if (_camera.orthographic)
        {
            return _camera.orthographicSize * 2f * _camera.aspect;
        }
        else
        {
            float distance = Mathf.Abs(transform.position.z);
            return 2f * distance * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * _camera.aspect;
        }
    }

    /// <summary>
    /// 获取相机视野高度（世界单位）
    /// </summary>
    private float GetCameraHeight()
    {
        if (_camera.orthographic)
        {
            return _camera.orthographicSize * 2f;
        }
        else
        {
            float distance = Mathf.Abs(transform.position.z);
            return 2f * distance * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        }
    }

    /// <summary>
    /// 获取鼠标在世界坐标中的位置
    /// </summary>
    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = -transform.position.z; // 对于正交相机，z值设为相机z位置的负值
        
        return _camera.ScreenToWorldPoint(mousePosition);
    }

    /// <summary>
    /// 立即跳转到指定位置
    /// </summary>
    public void TeleportTo(Vector3 position)
    {
        _targetPosition = position;
        transform.position = position;
        ClampPosition();
    }

    /// <summary>
    /// 在Scene视图中绘制边界Gizmos
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!enableBounds) return;
        
        Gizmos.color = Color.yellow;
        
        // 绘制边界矩形
        Vector3 center = new Vector3((minBounds.x + maxBounds.x) / 2f, (minBounds.y + maxBounds.y) / 2f, 0f);
        Vector3 size = new Vector3(maxBounds.x - minBounds.x, maxBounds.y - minBounds.y, 0.1f);
        
        Gizmos.DrawWireCube(center, size);
    }
}