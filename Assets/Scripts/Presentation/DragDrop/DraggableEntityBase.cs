using System;
using System.Linq;
using UnityEngine;
using ScreamHotel.Core;
using ScreamHotel.Systems;

namespace ScreamHotel.Presentation
{
    public interface IDraggableEntity { bool IsGhostEntity { get; } }

    [RequireComponent(typeof(Collider))]
    public abstract class DraggableEntityBase<TView> : MonoBehaviour, IDraggableEntity where TView : Component
    {
        [Header("Binding")] public Core.Game game;

        [Tooltip("拖拽期间把对象切到该层，避免拦截射线（通常为 Ignore Raycast）")]
        public string ignoreRaycastLayer = "Ignore Raycast";

        [Header("Drag (XY plane)")] public int dragMouseButton = 0;
        public float dragPlaneZ = 0f;
        public float followLerp = 30f;
        public bool dragSelf = true;
        
        [Header("Drop Zone Highlight")]
        [Tooltip("拖拽时是否高亮所有可放置区域（而不是仅鼠标下的一个）")]
        public bool highlightAllDropZones = true;
        
        [Header("Drag Bounds (World Space)")]
        public Vector2 dragBoundsMin = new Vector2(-10f, -10f);
        public Vector2 dragBoundsMax = new Vector2(10f, 10f);


        private IDropZone[] _zonesCache; // 拖拽期间缓存所有 DropZone，减少 Find 开销
        public bool IsGhostEntity => IsGhost;

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
            if (Input.GetMouseButtonDown(dragMouseButton) && PointerHitsTopMostSelf())
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
               
                p = ClampToBoundsWorld(p);

                if (dragSelf) transform.position = Vector3.Lerp(transform.position, p, Time.deltaTime * followLerp);

                var z = ZoneUnderPointer();

                if (!highlightAllDropZones)
                {
                    // 旧行为：仅维护和刷新鼠标下的一个 zone
                    if (!ReferenceEquals(z, _hoverZone))
                    {
                        _hoverZone?.ClearFeedback();
                        _hoverZone = z;
                    }
                    _hoverZone?.ShowHoverFeedback(entityId, IsGhost);
                }
                else
                {
                    // 拖拽时全局高亮
                    _hoverZone = z; // 仍保留引用，方便松手时用 TryDrop
                    if (_zonesCache != null)
                    {
                        foreach (var zone in _zonesCache)
                            zone?.ShowHoverFeedback(entityId, IsGhost);
                    }
                }
            }

            // 结束拖拽
            if (_dragging && Input.GetMouseButtonUp(dragMouseButton))
            {
                _dragging = false;

                if (_hoverZone != null)
                {
                    if (_hoverZone.TryDrop(entityId, IsGhost, out var anchor) && anchor != null)
                    {
                        _moveTo?.Invoke(anchor, DropMoveDuration);
                        PinInRoomAfterDrop();
                    }
                    else
                    {
                        HandleNoDropZoneRelease();
                    }
                }
                else
                {
                    HandleNoDropZoneRelease();
                }

                // 统一清理所有高亮
                if (_zonesCache != null)
                {
                    foreach (var zone in _zonesCache)
                        zone?.ClearFeedback(); // 逐个清除
                    _zonesCache = null;
                }

                EndSelfDrag();
                _hoverZone = null;
            }
        }

        // —— 无落点时的处理：在 NightShow 直接“解绑并自由移动”；否则回弹或原逻辑 —— 
        private void HandleNoDropZoneRelease()
        {
            var state = game ? game.State : GameState.Day;
            
            if (state == GameState.NightShow)
            {
                var assign = GetSystem<AssignmentSystem>(game, "_assignmentSystem");
                if (!string.IsNullOrEmpty(entityId))
                    UnassignEntity(assign, entityId);
                Debug.Log($"[{GetType().Name}] NightShow 无 DropZone：已清除 {entityId} 分配并释放移动");
                UnpinForFreeMove(); // 恢复自由移动（不固定在锚点）
                return;
            }
            
            if (state == GameState.Day)
            {
                var assign = GetSystem<AssignmentSystem>(game, "_assignmentSystem");
                if (!string.IsNullOrEmpty(entityId)) UnassignEntity(assign, entityId);
                Debug.Log($"[{GetType().Name}] 白天无 DropZone，已清除 {entityId} 分配（若系统允许）");
                UnpinForFreeMove();
                return;
            }

            // 其他阶段（例如 NightExecute 被锁不会开始拖；这里兜底维持原回弹）
            var tmp = new GameObject($"{GetType().Name}_ReturnTmp").transform;
            tmp.position = _dragStartPos;
            _moveTo?.Invoke(tmp, 0.12f);
            Destroy(tmp.gameObject, 0.2f);
        }


        // —— 拖拽开始/结束：切层 & 刚体保护 —— 
        private void BeginSelfDrag()
        {
            var hover = FindObjectOfType<UI.HoverUIController>();
            if (hover != null)
            {
                hover.ClosePickFearPanelIfActive();
                hover.CloseBuildRoomPanelIfActive();
            }

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
            
            var allBehaviours = FindObjectsOfType<MonoBehaviour>(true);
            _zonesCache = allBehaviours.OfType<IDropZone>().ToArray();
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
        
        private bool PointerHitsTopMostSelf()
        {
            var ray = _cam.ScreenPointToRay(Input.mousePosition);
            var hits = Physics.RaycastAll(ray, 1000f, ~0, QueryTriggerInteraction.Collide);
            if (hits == null || hits.Length == 0) return false;

            // 按距离近->远排序
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            // 先在命中序列里找“可拖拽实体”
            Component topAny = null;
            Component topGhost = null;

            foreach (var h in hits)
            {
                // 在碰撞体的父层级中找“任何可拖拽实体”
                var candidate = h.collider.GetComponentInParent<IDraggableEntity>() as Component;
                if (candidate == null) continue;

                // 记录“最近的任何实体”
                if (topAny == null) topAny = candidate;

                // 记录“最近的鬼”
                var ent = candidate as IDraggableEntity;
                if (ent != null && ent.IsGhostEntity)
                {
                    topGhost = candidate;
                    break; // 已经是最近的鬼，直接结束
                }

                // 注意：不要在这里 break；要继续找有没有更近的“鬼”
            }

            // 选择最终目标：优先鬼，否则最近的任意实体
            var picked = topGhost != null ? topGhost : topAny;
            if (picked == null) return false;

            // 只允许“被选中的那个实体”开始拖拽
            return picked.gameObject == gameObject || picked.transform.IsChildOf(transform);
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
        
        private Vector3 ClampToBoundsWorld(Vector3 worldPos)
        {
            worldPos.x = Mathf.Clamp(worldPos.x, dragBoundsMin.x, dragBoundsMax.x);
            worldPos.y = Mathf.Clamp(worldPos.y, dragBoundsMin.y, dragBoundsMax.y);
            return worldPos;
        }
        
        protected static T GetSystem<T>(object obj, string field) where T : class
        {
            var f = obj?.GetType().GetField(field, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return f?.GetValue(obj) as T;
        }
        
        public void SetEntityId(string id) => entityId = id;
    }
}
