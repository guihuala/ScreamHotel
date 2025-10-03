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
        
        [Header("Preview")]
        public bool cloneFromSelf = true;
        public string ignoreRaycastLayer = "Ignore Raycast";

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
        private GameObject _ghostPreview;  // 虚影对象

        void Awake()
        {
            _cam = Camera.main;
            _fixedZ = transform.position.z;
            _targetPos = transform.position;
            if (game == null) game = FindObjectOfType<Core.Game>();
            if (_pv == null) _pv = GetComponent<PawnView>();
        }

        public void SetGhostId(string id)
        {
            ghostId = id;
            Debug.Log($"DraggablePawn 设置了 ghostId: {ghostId}");
        }

        void Update()
        {
            // 开始拖拽
            if (Input.GetMouseButtonDown(dragMouseButton) && PointerHitsSelf())
            {
                _dragging = true;
                if (_returnCoro != null) { StopCoroutine(_returnCoro); _returnCoro = null; }

                _targetPos = MouseOnPlaneZ(dragPlaneZ);
                _targetPos.z = _fixedZ;

                // —— 构建虚影（优先克隆自身外观） ——
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
                _targetPos = MouseOnPlaneZ(dragPlaneZ);
                _targetPos.z = _fixedZ;
                _ghostPreview.transform.position =
                    Vector3.Lerp(_ghostPreview.transform.position, _targetPos, Time.deltaTime * followLerp);

                var zone = ZoneUnderPointer();
                if (zone != _hoverZone)
                {
                    if (_hoverZone) _hoverZone.ClearFeedback();
                    _hoverZone = zone;
                }
                if (_hoverZone) _hoverZone.ShowHoverFeedback(ghostId);
            }

            // 结束拖拽
            if (_dragging && Input.GetMouseButtonUp(dragMouseButton))
            {
                _dragging = false;

                if (_hoverZone != null)
                {
                    if (_hoverZone.TryDrop(ghostId, true, out var anchor))
                    {
                        if (anchor) _pv.MoveTo(anchor, 0.12f);
                    }
                    _hoverZone.ClearFeedback();
                    _hoverZone = null;
                }

                CleanupPreview();
            }
        }
        
        private GameObject BuildPreviewFromSelfOrPrefab()
        {
            if (_pv == null) _pv = GetComponent<PawnView>();
            GameObject preview = null;

            if (cloneFromSelf && _pv != null)
            {
                preview = _pv.BuildVisualPreview(ignoreRaycastLayer);
            }
            else
            {
                Debug.LogWarning("[DraggablePawn] 预览失败：既未开启 cloneFromSelf，也未提供 ghostPrefab。");
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

        // —— 工具：把 fromRoot 里的渲染器材质复制到 toRoot（用于 prefab 回退方案） ——
        private static void CopyRendererMaterials(Transform fromRoot, Transform toRoot)
        {
            var fromR = fromRoot.GetComponentsInChildren<Renderer>(true);
            var toR   = toRoot.GetComponentsInChildren<Renderer>(true);
            int n = Mathf.Min(fromR.Length, toR.Length);
            for (int i = 0; i < n; i++)
            {
                // 为避免实例化开支，这里用 sharedMaterials；如需可改成 materials
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
