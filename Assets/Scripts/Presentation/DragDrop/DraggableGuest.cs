using UnityEngine;
using ScreamHotel.Systems;

namespace ScreamHotel.Presentation
{
    [RequireComponent(typeof(Collider))]
    public class DraggableGuest : MonoBehaviour
    {
        public string guestId;
        public Core.Game game;
        public int dragMouseButton = 0;
        public float dragPlaneZ = 0f;
        public float followLerp = 30f;

        private Camera _cam; private bool _dragging; private float _fixedZ; private RoomDropZone _hoverZone;
        private AssignmentSystem _assign => GetSys<AssignmentSystem>(game, "_assignmentSystem");

        void Awake(){ _cam = Camera.main; _fixedZ = transform.position.z; if(!game) game = FindObjectOfType<ScreamHotel.Core.Game>(); }

        void Update()
        {
            if (Input.GetMouseButtonDown(dragMouseButton) && HitsSelf()) { _dragging=true; }
            if (_dragging)
            {
                // 跟随
                var p = MouseOnPlaneZ(dragPlaneZ); p.z = _fixedZ;
                transform.position = Vector3.Lerp(transform.position, p, Time.deltaTime*followLerp);

                // 区域检测
                var z = ZoneUnderPointer();
                if (z != _hoverZone){ _hoverZone?.ClearFeedback(); _hoverZone = z; }
                _hoverZone?.ShowHoverFeedbackGuest();

                if (Input.GetMouseButtonUp(dragMouseButton))
                {
                    _dragging=false;
                    if (_hoverZone!=null)
                    {
                        if (_hoverZone.TryDropGuest(guestId, out var anchor))
                        {
                            // 成功就吸附到房间锚点（客人用 index=房间已有客人数）
                            var gv = GetComponent<GuestView>();
                            if (anchor && gv) gv.MoveTo(anchor, 0.12f);
                        }
                        _hoverZone.ClearFeedback();
                        _hoverZone = null;
                    }
                }
            }
        }

        private bool HitsSelf(){ var ray=_cam.ScreenPointToRay(Input.mousePosition); return Physics.Raycast(ray,out var hit,1000f) && (hit.collider.gameObject==gameObject || hit.collider.transform.IsChildOf(transform)); }
        private Vector3 MouseOnPlaneZ(float z0){ var ray=_cam.ScreenPointToRay(Input.mousePosition); if(Mathf.Abs(ray.direction.z)<1e-5f) return transform.position; float t=(z0-ray.origin.z)/ray.direction.z; return ray.origin+ray.direction*t; }
        private RoomDropZone ZoneUnderPointer(){ var ray=_cam.ScreenPointToRay(Input.mousePosition); return Physics.Raycast(ray,out var hit,1000f) ? hit.collider.GetComponentInParent<RoomDropZone>() : null; }
        private static T GetSys<T>(object obj,string f){ var fld=obj.GetType().GetField(f,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance); return (T)fld.GetValue(obj); }
    }
}
