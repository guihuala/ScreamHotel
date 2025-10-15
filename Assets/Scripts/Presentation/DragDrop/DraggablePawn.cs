using ScreamHotel.Core;
using UnityEngine;

namespace ScreamHotel.Presentation
{
    [RequireComponent(typeof(Collider))]
    public class DraggablePawn : MonoBehaviour
    {
        public Core.Game game;
        public bool dragSelf = true;
        public string ignoreRaycastLayer = "Ignore Raycast";
        public int dragMouseButton = 0;
        public float dragPlaneZ = 0f;
        public float followLerp = 30f;

        private PawnView _pv;
        private string ghostId;
        private Camera _cam;
        private bool _dragging;
        private Vector3 _targetPos;
        private float _fixedZ;
        private IDropZone _hoverZone;
        private int _origLayer;

        private Rigidbody[] _rbs;
        private bool[] _rbUseGravity, _rbKinematic;
        private Vector3 _dragStartPos;
        
        private bool _dragLocked;

        void OnEnable()
        {
            EventBus.Subscribe<GameStateChanged>(OnGameStateChanged);
            if (!game) game = FindObjectOfType<Core.Game>();
            if (game) _dragLocked = (game.State == GameState.NightExecute); // 初始化
        }
        void OnDisable()
        {
            EventBus.Unsubscribe<GameStateChanged>(OnGameStateChanged);
        }

        private void OnGameStateChanged(GameStateChanged e)
        {
            if (e.State is GameState s) _dragLocked = (s == GameState.NightExecute);
        }
        
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
            // 开始拖拽分支（夜间锁判断）
            if (Input.GetMouseButtonDown(dragMouseButton) && PointerHitsSelf())
            {
                if (_dragLocked) { Debug.Log("[DraggablePawn] NightExecute 阶段禁止拖拽"); return; }
                if (IsLockedByTrainingSlot()) { Debug.Log($"[DraggablePawn] {ghostId} 已固定在训练槽位"); return; }
                _dragging = true;
                BeginSelfDrag();
            }

            if (_dragging)
            {
                _targetPos = MouseOnPlaneZ(dragPlaneZ); _targetPos.z = _fixedZ;
                if (dragSelf) transform.position = Vector3.Lerp(transform.position, _targetPos, Time.deltaTime * followLerp);

                var zone = ZoneUnderPointer();
                if (!ReferenceEquals(zone, _hoverZone))
                {
                    _hoverZone?.ClearFeedback();
                    _hoverZone = zone;
                }
                _hoverZone?.ShowHoverFeedback(ghostId, true);
            }

            if (_dragging && Input.GetMouseButtonUp(dragMouseButton))
            {
                _dragging = false;

                if (_hoverZone != null)
                {
                    if (_hoverZone.TryDrop(ghostId, true, out var anchor) && anchor != null)
                    {
                        // 先硬贴到锚点，完全消除偏移/飞走
                        var rb = GetComponent<Rigidbody>();
                        if (rb) { rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
                        transform.position = anchor.position;

                        // 再做一个非常短的 MoveTo（若你的 PawnView 需要）
                        _pv?.MoveTo(anchor, 0.08f);
                    }
                    else
                    {
                        // 回到起点
                        var tmp = new GameObject("PawnReturnTmp").transform; tmp.position = _dragStartPos;
                        _pv?.MoveTo(tmp, 0.12f);
                        Destroy(tmp.gameObject, 0.2f);
                    }

                    _hoverZone.ClearFeedback();
                    _hoverZone = null;
                }

                EndSelfDrag();
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
            float t = (planeZ - ray.origin.z) / ray.direction.z; t = Mathf.Max(t, 0f);
            return ray.origin + ray.direction * t;
        }

        private IDropZone ZoneUnderPointer()
        {
            var ray = _cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 1000f))
                return hit.collider.GetComponentInParent<IDropZone>();
            return null;
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
                    _rbs[i].useGravity = _rbUseGravity[i];
                    _rbs[i].isKinematic = _rbKinematic[i];
                }
            }
        }

        // 被训练槽位固定则拒绝拖拽
        private bool IsLockedByTrainingSlot()
        {
            if (string.IsNullOrEmpty(ghostId)) return false;
            var slots = FindObjectsOfType<TrainingSlot>(true);
            foreach (var s in slots)
                if (s && s.IsOccupied && s.GhostId == ghostId)
                    return true;
            return false;
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform) SetLayerRecursively(t.gameObject, layer);
        }
    }
}
