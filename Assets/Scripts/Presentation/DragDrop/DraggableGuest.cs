using UnityEngine;
using ScreamHotel.Systems;

namespace ScreamHotel.Presentation
{
    [RequireComponent(typeof(Collider))]
    public class DraggableGuest : MonoBehaviour
    {
        [Header("Binding")] public Core.Game game;

        [Tooltip("拖拽期间把对象切到该层，避免拦截射线（通常为 Ignore Raycast）")]
        public string ignoreRaycastLayer = "Ignore Raycast";

        [Header("Preview (fallback)")] public bool cloneFromSelf = true;

        [Header("Drag (XY plane)")] public int dragMouseButton = 0;
        public float dragPlaneZ = 0f;
        public float followLerp = 30f;

        private string guestId;
        private GuestView _gv;

        private Camera _cam;
        private bool _dragging;
        private float _fixedZ;
        private IDropZone _hoverZone;

        // ——— 抵消重力/物理 & 图层还原 ———
        private Rigidbody[] _rbs;
        private bool[] _rbUseGravity, _rbKinematic;
        private Vector3 _dragStartPos;
        private int _origLayer;

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
        }

        void Update()
        {
            // 开始拖拽
            if (Input.GetMouseButtonDown(dragMouseButton) && HitsSelf())
            {
                _dragging = true;
                BeginSelfDrag();
            }

            // 拖拽中
            if (_dragging)
            {
                var p = MouseOnPlaneZ(dragPlaneZ);
                p.z = _fixedZ;

                transform.position = Vector3.Lerp(transform.position, p, Time.deltaTime * followLerp);
              
                // 拖拽中
                var z = ZoneUnderPointer();
                if (!ReferenceEquals(z, _hoverZone))
                {
                    _hoverZone?.ClearFeedback();
                    _hoverZone = z;
                }

                _hoverZone?.ShowHoverFeedback(guestId, /*isGhost*/ false);
            }

            // 结束拖拽
            if (_dragging && Input.GetMouseButtonUp(dragMouseButton))
            {
                _dragging = false;

                // 松手
                if (_hoverZone != null)
                {
                    if (_hoverZone.TryDrop(guestId, /*isGhost*/ false, out var anchor))
                    {
                        if (anchor) _gv?.MoveTo(anchor, 0.12f);
                    }
                    else
                    {
                        var tmp = new GameObject("GuestReturnTmp").transform;
                        tmp.position = _dragStartPos;
                        _gv?.MoveTo(tmp, 0.12f);
                        Destroy(tmp.gameObject, 0.2f);
                    }

                    _hoverZone.ClearFeedback();
                    _hoverZone = null;
                }

                EndSelfDrag();
                _hoverZone?.ClearFeedback();
                _hoverZone = null;
            }
        }

        // === 自身拖拽：开始/结束 ===
        private void BeginSelfDrag()
        {
            _dragStartPos = transform.position;

            // 切层：避免自己挡射线
            _origLayer = gameObject.layer;
            int lyr = LayerMask.NameToLayer(ignoreRaycastLayer);
            if (lyr >= 0) SetLayerRecursively(gameObject, lyr);

            // 抵消重力 & 受力：全部刚体改为运动学
            _rbs = GetComponentsInChildren<Rigidbody>(true);
            if (_rbs != null && _rbs.Length > 0)
            {
                _rbUseGravity = new bool[_rbs.Length];
                _rbKinematic = new bool[_rbs.Length];
                for (int i = 0; i < _rbs.Length; i++)
                {
                    _rbUseGravity[i] = _rbs[i].useGravity;
                    _rbKinematic[i] = _rbs[i].isKinematic;
                    _rbs[i].useGravity = false;
                    _rbs[i].isKinematic = true;
                    _rbs[i].velocity = Vector3.zero;
                    _rbs[i].angularVelocity = Vector3.zero;
                }
            }
        }

        private void EndSelfDrag()
        {
            // 还原层
            SetLayerRecursively(gameObject, _origLayer);
            // 还原刚体设置
            if (_rbs != null)
            {
                for (int i = 0; i < _rbs.Length; i++)
                {
                    if (_rbs[i] == null) continue;
                    _rbs[i].useGravity = _rbUseGravity != null && i < _rbUseGravity.Length
                        ? _rbUseGravity[i]
                        : _rbs[i].useGravity;
                    _rbs[i].isKinematic = _rbKinematic != null && i < _rbKinematic.Length
                        ? _rbKinematic[i]
                        : _rbs[i].isKinematic;
                }
            }
        }

        // === Raycast/Helpers ===
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

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform) SetLayerRecursively(t.gameObject, layer);
        }

        private IDropZone ZoneUnderPointer()
        {
            var ray = _cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 1000f))
                return hit.collider.GetComponentInParent<IDropZone>();
            return null;
        }
    }
}