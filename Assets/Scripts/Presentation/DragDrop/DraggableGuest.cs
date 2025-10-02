using UnityEngine;
using ScreamHotel.Systems;

namespace ScreamHotel.Presentation
{
    [RequireComponent(typeof(Collider))]
    public class DraggableGuest : MonoBehaviour
    {
        [Header("Binding")]
        public Core.Game game;
        public GameObject guestPrefab;
        
        [Header("Drag (XY plane)")]
        public int dragMouseButton = 0;
        public float dragPlaneZ = 0f;
        public float followLerp = 30f;
        
        private string guestId;
        private GuestView _gv;
        
        private Camera _cam; 
        private bool _dragging; 
        private float _fixedZ; 
        private RoomDropZone _hoverZone;
        private GameObject _ghostPreview; // 虚影对象（预制件）

        void Awake()
        {
            _cam = Camera.main; 
            _fixedZ = transform.position.z; 
            if (!game) game = FindObjectOfType<Core.Game>();
            
            _gv = GetComponent<GuestView>();  
        }

        // 通过 Bind 方法设置 guestId
        public void SetGuestId(string id)
        {
            guestId = id;
            Debug.Log($"DraggableGuest 设置了 guestId: {guestId}");
        }

        void Update()
        {
            // 如果没有虚影，创建虚影预制件
            if (!_ghostPreview && _dragging)
            {
                _ghostPreview = Instantiate(guestPrefab);
                _ghostPreview.transform.position = transform.position;
            }

            if (Input.GetMouseButtonDown(dragMouseButton) && HitsSelf()) 
            { 
                _dragging = true; 
            }

            if (_dragging)
            {
                if (_ghostPreview != null) // 确保虚影对象没有被销毁
                {
                    var p = MouseOnPlaneZ(dragPlaneZ);
                    p.z = _fixedZ;
                    _ghostPreview.transform.position = Vector3.Lerp(_ghostPreview.transform.position, p, Time.deltaTime * followLerp);

                    var z = ZoneUnderPointer();
                    if (z != _hoverZone) 
                    { 
                        _hoverZone?.ClearFeedback(); 
                        _hoverZone = z; 
                    }
                    _hoverZone?.ShowHoverFeedbackGuest();
                }

                if (Input.GetMouseButtonUp(dragMouseButton))
                {
                    _dragging = false;

                    if (_hoverZone != null)
                    {
                        if (_hoverZone.TryDrop(guestId, false, out var anchor))
                        {
                            if (anchor) _gv.MoveTo(anchor, 0.12f);
                        }
                    }

                    if (_ghostPreview != null) // 销毁虚影之前先检查
                    {
                        Destroy(_ghostPreview); // 放置后销毁虚影
                    }
                    _hoverZone?.ClearFeedback();
                    _hoverZone = null;
                }
            }
        }

        private bool HitsSelf()
        {
            var ray = _cam.ScreenPointToRay(Input.mousePosition);
            return Physics.Raycast(ray, out var hit, 1000f) && 
                   (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform));
        }

        private Vector3 MouseOnPlaneZ(float z0)
        {
            var ray = _cam.ScreenPointToRay(Input.mousePosition);
            if (Mathf.Abs(ray.direction.z) < 1e-5f) return transform.position; 
            float t = (z0 - ray.origin.z) / ray.direction.z; 
            return ray.origin + ray.direction * t;
        }

        private RoomDropZone ZoneUnderPointer()
        {
            var ray = _cam.ScreenPointToRay(Input.mousePosition);
            return Physics.Raycast(ray, out var hit, 1000f) ? hit.collider.GetComponentInParent<RoomDropZone>() : null;
        }
    }
}
