using UnityEngine;
using ScreamHotel.Systems;
using UnityEngine.Serialization;

namespace ScreamHotel.Presentation
{
    [RequireComponent(typeof(Collider))]
    public class DraggablePawn : MonoBehaviour
    {
        [Header("Binding")]
        public Core.Game game;
        public GameObject ghostPrefab;  // 用于创建虚影的预制件

        [Header("Drag (XY plane)")]
        public int dragMouseButton = 0;
        public float dragPlaneZ = 0f;       // 鼠标投射到 z=常量 的平面
        public float followLerp = 30f;      // 拖拽跟随插值
        
        private PawnView _pv;  // 原始鬼怪视图
        private string ghostId;
        
        private Camera _cam;
        private bool _dragging;
        private Vector3 _targetPos;
        private float _fixedZ;
        private Coroutine _returnCoro;
        private RoomDropZone _hoverZone;    // 当前悬停的房间区
        private GameObject _ghostPreview;  // 虚影对象（预制件）

        void Awake()
        {
            _cam = Camera.main;
            _fixedZ = transform.position.z;
            _targetPos = transform.position;
            if (game == null) game = FindObjectOfType<Core.Game>();
            
            if (_pv == null) _pv = GetComponent<PawnView>();  // 获取 PawnView 组件
        }
        
        public void SetGhostId(string id)
        {
            ghostId = id;
            Debug.Log($"DraggablePawn 设置了 ghostId: {ghostId}");
        }

        void Update()
        {
            // 如果没有虚影，创建虚影预制件
            if (!_ghostPreview && _dragging)
            {
                _ghostPreview = Instantiate(ghostPrefab);
                _ghostPreview.transform.position = transform.position;
            }

            // 开始拖拽（点击到自己）
            if (Input.GetMouseButtonDown(dragMouseButton) && PointerHitsSelf())
            {
                _dragging = true;
                if (_returnCoro != null) { StopCoroutine(_returnCoro); _returnCoro = null; }

                _targetPos = MouseOnPlaneZ(dragPlaneZ);
                _targetPos.z = _fixedZ;
            }

            // 拖拽中：虚影跟随 + 悬停高亮
            if (_dragging)
            {
                if (_ghostPreview != null)
                {
                    _targetPos = MouseOnPlaneZ(dragPlaneZ);
                    _targetPos.z = _fixedZ;
                    _ghostPreview.transform.position = Vector3.Lerp(_ghostPreview.transform.position, _targetPos,
                        Time.deltaTime * followLerp);

                    var zone = ZoneUnderPointer();
                    if (zone != _hoverZone)
                    {
                        if (_hoverZone) _hoverZone.ClearFeedback();
                        _hoverZone = zone;
                    }

                    if (_hoverZone) _hoverZone.ShowHoverFeedback(ghostId);
                }
            }

            // 放置时：检查是否可以分配并将虚影附加到锚点
            if (_dragging && Input.GetMouseButtonUp(dragMouseButton))
            {
                _dragging = false;

                if (_hoverZone != null)
                {
                    if (_hoverZone.TryDrop(ghostId, true, out var anchor)) // 传入 true 表示这是鬼怪
                    {
                        if (anchor) _pv.MoveTo(anchor, 0.12f); // 将鬼怪移动到目标锚点
                    }
                    _hoverZone.ClearFeedback();
                    _hoverZone = null;
                }

                // 放置后销毁虚影
                Destroy(_ghostPreview);
            }
        }

        private bool PointerHitsSelf()
        {
            var ray = _cam.ScreenPointToRay(Input.mousePosition);
            return Physics.Raycast(ray, out var hit, 1000f)
                   && (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform));
        }

        private Vector3 MouseOnPlaneZ(float planeZ)
        {
            var ray = _cam.ScreenPointToRay(Input.mousePosition);
            if (Mathf.Abs(ray.direction.z) < 1e-5f)
                return transform.position; // 射线几乎平行于平面时兜底

            float t = (planeZ - ray.origin.z) / ray.direction.z;
            t = Mathf.Max(t, 0f);
            return ray.origin + ray.direction * t;
        }

        private RoomDropZone ZoneUnderPointer()
        {
            var ray = _cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 1000f))
                return hit.collider.GetComponentInParent<RoomDropZone>();
            return null;
        }

        private static T GetSystem<T>(object obj, string field) where T : class
        {
            var f = obj.GetType().GetField(field,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return f?.GetValue(obj) as T;
        }
    }
}
