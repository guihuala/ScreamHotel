using UnityEngine;
using ScreamHotel.Systems;

namespace ScreamHotel.Presentation
{
    [RequireComponent(typeof(Collider))]
    public class DraggablePawn : MonoBehaviour
    {
        [Header("Binding")]
        public Core.Game game;
        
        [Header("Drag Mode")]
        public bool dragSelf = true;
        public string ignoreRaycastLayer = "Ignore Raycast";

        [Header("Preview (fallback)")]
        public bool cloneFromSelf = true;

        [Header("Drag (XY plane)")]
        public int dragMouseButton = 0;
        public float dragPlaneZ = 0f;
        public float followLerp = 30f;

        private PawnView _pv;
        private string ghostId;

        private Camera _cam;
        private bool _dragging;
        private Vector3 _targetPos;
        private float _fixedZ;
        private Coroutine _returnCoro;
        private RoomDropZone _hoverZone;
        private GameObject _ghostPreview;  // 虚影对象（仅 dragSelf=false 时用）

        // 物理/图层状态
        private Rigidbody[] _rbs;
        private bool[] _rbUseGravity, _rbKinematic;
        private Vector3 _dragStartPos;
        private int _origLayer;

        void Awake()
        {
            _cam = Camera.main;
            _fixedZ = transform.position.z;
            _targetPos = transform.position;
            if (game == null) game = FindObjectOfType<Core.Game>();
            if (_pv == null) _pv = GetComponent<PawnView>();
        }

        public void SetGhostId(string id) { ghostId = id; }

        void Update()
        {
            // 开始
            if (Input.GetMouseButtonDown(dragMouseButton) && PointerHitsSelf())
            {
                _dragging = true;
                if (_returnCoro != null) { StopCoroutine(_returnCoro); _returnCoro = null; }

                if (dragSelf) BeginSelfDrag();
                else
                {
                    if (_ghostPreview == null) _ghostPreview = BuildPreviewFromSelfOrPrefab();
                    if (_ghostPreview != null)
                    {
                        _ghostPreview.transform.position = transform.position;
                        _ghostPreview.transform.rotation = transform.rotation;
                        _ghostPreview.transform.localScale = transform.lossyScale;
                    }
                }
            }

            // 拖拽中
            if (_dragging)
            {
                _targetPos = MouseOnPlaneZ(dragPlaneZ); _targetPos.z = _fixedZ;

                if (dragSelf)
                    transform.position = Vector3.Lerp(transform.position, _targetPos, Time.deltaTime * followLerp);
                else if (_ghostPreview != null)
                    _ghostPreview.transform.position =
                        Vector3.Lerp(_ghostPreview.transform.position, _targetPos, Time.deltaTime * followLerp);

                var zone = ZoneUnderPointer();
                if (zone != _hoverZone) { _hoverZone?.ClearFeedback(); _hoverZone = zone; }
                _hoverZone?.ShowHoverFeedback(ghostId);
            }

            // 结束
            if (_dragging && Input.GetMouseButtonUp(dragMouseButton))
            {
                _dragging = false;

                if (_hoverZone != null)
                {
                    if (_hoverZone.TryDrop(ghostId, true, out var anchor))
                    {
                        if (anchor) _pv?.MoveTo(anchor, 0.12f);
                    }
                    else if (dragSelf)
                    {
                        var tmp = new GameObject("PawnReturnTmp").transform; tmp.position = _dragStartPos;
                        _pv?.MoveTo(tmp, 0.12f);
                        Destroy(tmp.gameObject, 0.2f);
                    }
                    _hoverZone.ClearFeedback();
                    _hoverZone = null;
                }

                if (dragSelf) EndSelfDrag();
                CleanupPreview();
            }
        }

        private void BeginSelfDrag()
        {
            _dragStartPos = transform.position;

            _origLayer = gameObject.layer;
            int lyr = LayerMask.NameToLayer(ignoreRaycastLayer);
            if (lyr >= 0) SetLayerRecursively(gameObject, lyr);

            _rbs = GetComponentsInChildren<Rigidbody>(true);
            if (_rbs != null && _rbs.Length > 0)
            {
                _rbUseGravity = new bool[_rbs.Length];
                _rbKinematic  = new bool[_rbs.Length];
                for (int i = 0; i < _rbs.Length; i++)
                {
                    _rbUseGravity[i] = _rbs[i].useGravity;
                    _rbKinematic[i]  = _rbs[i].isKinematic;
                    _rbs[i].useGravity = false;
                    _rbs[i].isKinematic = true;
                    _rbs[i].velocity = Vector3.zero;
                    _rbs[i].angularVelocity = Vector3.zero;
                }
            }
        }

        private void EndSelfDrag()
        {
            SetLayerRecursively(gameObject, _origLayer);
            if (_rbs != null)
            {
                for (int i = 0; i < _rbs.Length; i++)
                {
                    if (_rbs[i] == null) continue;
                    _rbs[i].useGravity = _rbUseGravity != null && i < _rbUseGravity.Length ? _rbUseGravity[i] : _rbs[i].useGravity;
                    _rbs[i].isKinematic = _rbKinematic != null && i < _rbKinematic.Length ? _rbKinematic[i] : _rbs[i].isKinematic;
                }
            }
        }

        // 预览体兜底
        private GameObject BuildPreviewFromSelfOrPrefab()
        {
            if (_pv == null) _pv = GetComponent<PawnView>();
            if (cloneFromSelf && _pv != null) return _pv.BuildVisualPreview(ignoreRaycastLayer);
            Debug.LogWarning("[DraggablePawn] 预览失败：既未开启 cloneFromSelf，也未提供 ghostPrefab。");
            return null;
        }
        private void CleanupPreview() { if (_ghostPreview != null) { Destroy(_ghostPreview); _ghostPreview = null; } }

        private bool PointerHitsSelf()
        {
            var ray = _cam.ScreenPointToRay(Input.mousePosition);
            return Physics.Raycast(ray, out var hit, 1000f)
                   && (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform));
        }
        private Vector3 MouseOnPlaneZ(float planeZ)
        {
            var ray = _cam.ScreenPointToRay(Input.mousePosition);
            if (Mathf.Abs(ray.direction.z) < 1e-5f) return transform.position;
            float t = (planeZ - ray.origin.z) / ray.direction.z; t = Mathf.Max(t, 0f);
            return ray.origin + ray.direction * t;
        }
        private RoomDropZone ZoneUnderPointer()
        {
            var ray = _cam.ScreenPointToRay(Input.mousePosition);
            return Physics.Raycast(ray, out var hit, 1000f) ? hit.collider.GetComponentInParent<RoomDropZone>() : null;
        }
        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform) SetLayerRecursively(t.gameObject, layer);
        }
    }
}