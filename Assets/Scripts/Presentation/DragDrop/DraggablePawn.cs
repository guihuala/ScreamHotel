using ScreamHotel.Systems;
using UnityEngine;

namespace ScreamHotel.Presentation
{
    [RequireComponent(typeof(Collider))]
    public class DraggablePawn : MonoBehaviour
    {
        [Header("Binding")]
        public string ghostId;
        public Core.Game game;
        public PawnView pawnView;

        [Header("Drag (XY plane)")]
        public int dragMouseButton = 0;
        public float dragPlaneZ = 0f;       // 鼠标投射到 z=常量 的平面
        public float followLerp = 30f;      // 拖拽跟随插值
        
        private Camera _cam;
        private bool _dragging;
        private Vector3 _targetPos;
        private float _fixedZ;
        private Coroutine _returnCoro;
        private RoomDropZone _hoverZone;    // 当前悬停的房间区

        private AssignmentSystem _assign => GetSystem<AssignmentSystem>(game, "_assignmentSystem");

        void Awake()
        {
            _cam = Camera.main;
            if (game == null) game = FindObjectOfType<ScreamHotel.Core.Game>();
            if (pawnView == null) pawnView = GetComponent<PawnView>();
            _fixedZ = transform.position.z; // 锁定 Z
            _targetPos = transform.position;
        }

        void Update()
        {
            // 开始拖拽（点到自己）
            if (Input.GetMouseButtonDown(dragMouseButton) && PointerHitsSelf())
            {
                _dragging = true;
                if (_returnCoro != null) { StopCoroutine(_returnCoro); _returnCoro = null; }

                _targetPos = MouseOnPlaneZ(dragPlaneZ);
                _targetPos.z = _fixedZ;
                transform.position = _targetPos;
            }

            // 拖拽中：跟随 + 悬停高亮
            if (_dragging)
            {
                // 跟随
                _targetPos = MouseOnPlaneZ(dragPlaneZ);
                _targetPos.z = _fixedZ;
                transform.position = Vector3.Lerp(transform.position, _targetPos, Time.deltaTime * followLerp);

                // 悬停检测
                var zone = ZoneUnderPointer();
                if (zone != _hoverZone)
                {
                    if (_hoverZone) _hoverZone.ClearFeedback();
                    _hoverZone = zone;
                }
                if (_hoverZone) _hoverZone.ShowHoverFeedback(ghostId);
            }

            // 结束：投放或回原
            if (_dragging && Input.GetMouseButtonUp(dragMouseButton))
            {
                _dragging = false;
                
                if (_hoverZone != null)
                {
                    if (_hoverZone.TryDrop(ghostId, out var anchor))
                    {
                        // 分配成功：吸附到房间锚点
                        if (anchor) pawnView.MoveTo(anchor, 0.12f);
                    }
                    _hoverZone.ClearFeedback();
                    _hoverZone = null;
                }
            }
        }

        private System.Collections.IEnumerator ReturnTo(Vector3 dest, float dur)
        {
            Vector3 from = transform.position;
            dest.z = _fixedZ;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.001f, dur);
                transform.position = Vector3.Lerp(from, dest, Mathf.SmoothStep(0,1,t));
                yield return null;
            }
            transform.position = dest;
            _returnCoro = null;
        }

        private bool PointerHitsSelf()
        {
            var ray = _cam.ScreenPointToRay(Input.mousePosition);
            return Physics.Raycast(ray, out var hit, 1000f)
                   && (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform));
        }

        private Vector3 MouseOnPlaneZ(float planeZ)
        {
            // 与 z=planeZ 的平面求交：平面法向量为 (0,0,1)
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