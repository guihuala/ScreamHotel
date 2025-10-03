using UnityEngine;
using ScreamHotel.Systems;

namespace ScreamHotel.Presentation
{
    [RequireComponent(typeof(Collider))]
    public class DraggableGuest : MonoBehaviour
    {
        [Header("Binding")]
        public Core.Game game;
        
        [Header("Preview")]
        public bool cloneFromSelf = true;
        public string ignoreRaycastLayer = "Ignore Raycast";

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
        private GameObject _ghostPreview; // 虚影对象

        void Awake()
        {
            _cam = Camera.main;
            _fixedZ = transform.position.z;
            if (!game) game = FindObjectOfType<Core.Game>();
            _gv = GetComponent<GuestView>();
        }

        public void SetGuestId(string id)
        {
            guestId = id;
            Debug.Log($"DraggableGuest 设置了 guestId: {guestId}");
        }

        void Update()
        {
            // 开始拖拽
            if (Input.GetMouseButtonDown(dragMouseButton) && HitsSelf())
            {
                _dragging = true;

                if (_ghostPreview == null)
                    _ghostPreview = BuildPreviewFromSelfOrPrefab();
                if (_ghostPreview != null)
                {
                    _ghostPreview.transform.position = transform.position;
                    _ghostPreview.transform.rotation = transform.rotation;
                    _ghostPreview.transform.localScale = transform.lossyScale;
                }
            }

            // 拖拽中
            if (_dragging && _ghostPreview != null)
            {
                var p = MouseOnPlaneZ(dragPlaneZ);
                p.z = _fixedZ;
                _ghostPreview.transform.position =
                    Vector3.Lerp(_ghostPreview.transform.position, p, Time.deltaTime * followLerp);

                var z = ZoneUnderPointer();
                if (z != _hoverZone) { _hoverZone?.ClearFeedback(); _hoverZone = z; }
                _hoverZone?.ShowHoverFeedbackGuest();
            }

            // 结束拖拽
            if (_dragging && Input.GetMouseButtonUp(dragMouseButton))
            {
                _dragging = false;

                if (_hoverZone != null)
                {
                    if (_hoverZone.TryDrop(guestId, false,out var anchor))
                    {
                        if (anchor) _gv.MoveTo(anchor, 0.12f);
                    }
                }

                CleanupPreview();
                _hoverZone?.ClearFeedback();
                _hoverZone = null;
            }
        }
        
        private GameObject BuildPreviewFromSelfOrPrefab()
        {
            if (_gv == null) _gv = GetComponent<GuestView>();
            GameObject preview = null;

            if (cloneFromSelf && _gv != null)
            {
                preview = _gv.BuildVisualPreview(ignoreRaycastLayer);
            }
            else
            {
                Debug.LogWarning("[DraggableGuest] 预览失败：既未开启 cloneFromSelf，也未提供 guestPrefab。");
            }

            return preview;
        }
        
        private void CleanupPreview()
        {
            if (_ghostPreview != null)
            {
                Destroy(_ghostPreview);
                _ghostPreview = null;
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

        private static void CopyRendererMaterials(Transform fromRoot, Transform toRoot)
        {
            var fromR = fromRoot.GetComponentsInChildren<Renderer>(true);
            var toR   = toRoot.GetComponentsInChildren<Renderer>(true);
            int n = Mathf.Min(fromR.Length, toR.Length);
            for (int i = 0; i < n; i++)
            {
                toR[i].sharedMaterials = fromR[i].sharedMaterials;
                toR[i].enabled = fromR[i].enabled;
            }
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform) SetLayerRecursively(t.gameObject, layer);
        }
    }
}
