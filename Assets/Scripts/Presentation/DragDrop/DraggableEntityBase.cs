using System;
using UnityEngine;
using ScreamHotel.Core;
using ScreamHotel.Systems;

namespace ScreamHotel.Presentation
{
    [RequireComponent(typeof(Collider))]
    public abstract class DraggableEntityBase<TView> : MonoBehaviour where TView : Component
    {
        [Header("Binding")] public Core.Game game;

        [Tooltip("拖拽期间把对象切到该层，避免拦截射线（通常为 Ignore Raycast）")]
        public string ignoreRaycastLayer = "Ignore Raycast";

        [Header("Drag (XY plane)")] public int dragMouseButton = 0;
        public float dragPlaneZ = 0f;
        public float followLerp = 30f;
        public bool dragSelf = true;

        // —— 由子类设置的标识 —— 
        protected string entityId;
        protected abstract bool IsGhost { get; }       // 子类告知是鬼还是客人
        protected abstract float DropMoveDuration { get; }   // 落位动画时长
        protected virtual bool IsExternallyLocked() => false; // 训练槽位锁定（鬼才有）
        protected abstract void UnassignEntity(AssignmentSystem assign, string id);

        // —— 视图与通用状态 —— 
        protected TView _view;
        private Action<Transform, float> _moveTo;      // 反射缓存 MoveTo(anchor, dur)
        private Camera _cam;
        private bool _dragging;
        private float _fixedZ;
        private IDropZone _hoverZone;
        private int _origLayer;
        private Vector3 _dragStartPos;

        // 物理缓存
        private Rigidbody[] _rbs;
        private bool[] _rbUseGravity, _rbKinematic;

        // 锁：夜间执行阶段禁拖
        private bool _dragLocked;

        protected virtual void OnEnable()
        {
            EventBus.Subscribe<GameStateChanged>(OnGameStateChanged);
            if (!game) game = FindObjectOfType<Core.Game>();
            if (game) _dragLocked = (game.State == GameState.NightExecute);
        }

        protected virtual void OnDisable()
        {
            EventBus.Unsubscribe<GameStateChanged>(OnGameStateChanged);
        }

        private void OnGameStateChanged(GameStateChanged e)
        {
            if (e.State is GameState s) _dragLocked = (s == GameState.NightExecute);
        }

        protected virtual void Awake()
        {
            _cam = Camera.main;
            _fixedZ = transform.position.z;
            if (!game) game = FindObjectOfType<Core.Game>();
            _view = GetComponent<TView>();

            // 缓存 MoveTo(anchor, float) 委托（避免每次反射）
            var m = typeof(TView).GetMethod("MoveTo", new Type[] { typeof(Transform), typeof(float) });
            if (m != null)
                _moveTo = (Action<Transform, float>)Delegate.CreateDelegate(typeof(Action<Transform, float>), _view, m, false);
        }

        protected virtual void Update()
        {
            // 开始拖拽
            if (Input.GetMouseButtonDown(dragMouseButton) && PointerHitsSelf())
            {
                if (_dragLocked) { Debug.Log($"[{GetType().Name}] NightExecute 阶段禁止拖拽"); return; }
                if (IsExternallyLocked()) { Debug.Log($"[{GetType().Name}] 实体被外部系统锁定，禁止拖拽"); return; }

                _dragging = true;
                BeginSelfDrag();
            }

            // 拖拽中
            if (_dragging)
            {
                var p = MouseOnPlaneZ(dragPlaneZ);
                p.z = _fixedZ;
                if (dragSelf) transform.position = Vector3.Lerp(transform.position, p, Time.deltaTime * followLerp);

                var z = ZoneUnderPointer();
                if (!ReferenceEquals(z, _hoverZone))
                {
                    _hoverZone?.ClearFeedback();
                    _hoverZone = z;
                }
                _hoverZone?.ShowHoverFeedback(entityId, IsGhost);
            }

            // 结束拖拽
            if (_dragging && Input.GetMouseButtonUp(dragMouseButton))
            {
                _dragging = false;

                if (_hoverZone != null)
                {
                    if (_hoverZone.TryDrop(entityId, IsGhost, out var anchor) && anchor != null)
                    {
                        // MoveTo(anchor)
                        _moveTo?.Invoke(anchor, DropMoveDuration);
                        PinInRoomAfterDrop();
                    }
                    else
                    {
                        HandleNoDropZoneRelease();
                    }
                    _hoverZone.ClearFeedback();
                    _hoverZone = null;
                }
                else
                {
                    HandleNoDropZoneRelease();
                }

                EndSelfDrag();
                _hoverZone?.ClearFeedback();
                _hoverZone = null;
            }
        }

        // —— 无落点时的处理：白天解绑并不回弹；其他时段回弹 —— 
        private void HandleNoDropZoneRelease()
        {
            bool isDay = (game && game.State == GameState.Day);
            if (isDay)
            {
                var assign = GetSystem<AssignmentSystem>(game, "_assignmentSystem");
                if (!string.IsNullOrEmpty(entityId)) UnassignEntity(assign, entityId);
                Debug.Log($"[{GetType().Name}] 白天无 DropZone，已清除 {entityId} 分配");
                UnpinForFreeMove(); // 恢复自由移动
            }
            else
            {
                // 夜/结算：回到起点
                var tmp = new GameObject($"{GetType().Name}_ReturnTmp").transform;
                tmp.position = _dragStartPos;
                _moveTo?.Invoke(tmp, 0.12f);
                Destroy(tmp.gameObject, 0.2f);
            }
        }

        // —— 拖拽开始/结束：切层 & 刚体保护 —— 
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
                    _rbs[i].isKinematic = _rbKinematic  != null && i < _rbKinematic.Length  ? _rbKinematic[i]  : _rbs[i].isKinematic;
                }
            }
        }

        // —— 固定/解锁（解决“放进房间自己走出来/穿墙”的问题） —— 
        protected virtual void PinInRoomAfterDrop()
        {
            var patrol = GetComponent<PatrolWalker>();
            if (patrol) patrol.enabled = false;

            var rb = GetComponent<Rigidbody>();
            if (rb)
            {
                rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
        }

        protected virtual void UnpinForFreeMove()
        {
            var patrol = GetComponent<PatrolWalker>();
            if (patrol) patrol.enabled = true;

            var rb = GetComponent<Rigidbody>();
            if (rb) rb.isKinematic = false;
        }

        // —— Raycast / Helpers —— 
        private bool PointerHitsSelf()
        {
            var ray = _cam.ScreenPointToRay(Input.mousePosition);
            return Physics.Raycast(ray, out var hit, 1000f)
                && (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform));
        }

        private Vector3 MouseOnPlaneZ(float z0)
        {
            var ray = _cam.ScreenPointToRay(Input.mousePosition);
            if (Mathf.Abs(ray.direction.z) < 1e-5f) return transform.position;
            float t = (z0 - ray.origin.z) / ray.direction.z; t = Mathf.Max(t, 0f);
            return ray.origin + ray.direction * t;
        }

        private IDropZone ZoneUnderPointer()
        {
            var ray = _cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 1000f))
                return hit.collider.GetComponentInParent<IDropZone>();
            return null;
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform) SetLayerRecursively(t.gameObject, layer);
        }

        protected static T GetSystem<T>(object obj, string field) where T : class
        {
            var f = obj?.GetType().GetField(field, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return f?.GetValue(obj) as T;
        }

        // —— 提供给外部：统一的 ID 设定 —— 
        public void SetEntityId(string id) => entityId = id;
    }
}
