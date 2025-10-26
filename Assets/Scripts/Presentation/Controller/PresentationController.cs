using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ScreamHotel.Core;


namespace ScreamHotel.Presentation
{
    public partial class PresentationController : MonoBehaviour
    {
        // ===== Refs & Prefabs =====
        [Header("Refs")]
        public Game game;
        public Transform roomsRoot;
        public Transform ghostsRoot;
        public Transform guestsRoot;
        public RoomView roomPrefab;
        public PawnView ghostPrefab;
        public GuestView guestPrefab;
        public Transform roofPrefab;
        public ShopSlotView shopSlotPrefab;
        public GameObject shopRerollPrefab;

        // ===== Floor/Room Layout =====
        [Header("Floor/Room Layout (relative to roomsRoot)")]
        public float roomBaseY = 0f;
        public float floorSpacing = 8f;
        public float xInner = 3.0f, xOuter = 6.0f;
        public Transform elevatorPrefab;
        private readonly HashSet<int> _elevatorsSpawned = new();

        // ===== Ghost Spawn Area =====
        [Header("Ghost Spawn Room (relative to ghostsRoot)")]
        public Transform ghostSpawnRoomRoot;
        public float spawnFixedZ = 0f;

        // ===== Guest Queue =====
        [Header("Guest Queue (relative to guestQueueRoot)")]
        public Transform guestQueueRoot;
        public float queueSpacingX = 0.8f;
        public float queueRowHeight = 0.7f;
        public int queueWrapCount = 0;
        public float queueFixedZ = 0f;
        
        [Header("Guest Spawn Walk")]
        [Tooltip("实例化后，沿自身向右移动的世界距离")]
        public float guestSpawnWalkDistance = 0.6f;
        [Tooltip("实例化后，行走到目标所花时间（秒）")]
        public float guestSpawnWalkDuration = 0.25f;
        [Tooltip("Spine 行走动画名（留空则不切换）")]
        public string spineWalkAnim = "walk";
        [Tooltip("Spine 待机动画名（留空则不切换）")]
        public string spineIdleAnim = "idle";

        // ===== Shop (Basement) =====
        [Header("Ghost Shop (Basement)")]
        public Transform shopRoot;
        public float shopSlotSpacingX = 1.6f;
        public int shopSlotsPerRow = 5;
        public float shopFixedZ = 0f;
        
        [Header("Floor Frame Prefab")]
        public Transform floorFramePrefab;

        // ===== Runtime maps =====
        private Transform _currentRoof;
        private readonly Dictionary<string, RoomView> _roomViews = new();
        private readonly Dictionary<string, PawnView> _ghostViews = new();
        private readonly Dictionary<string, GuestView> _guestViews = new();
        private readonly Dictionary<string, Transform> _shopOfferViews = new();

        // ===== Lifecycle =====
        void OnEnable()
        {
            EventBus.Subscribe<GameStateChanged>(OnGameState);
            EventBus.Subscribe<RoomPurchasedEvent>(OnRoomPurchased);
            EventBus.Subscribe<RoomUpgradedEvent>(OnRoomUpgraded);
            EventBus.Subscribe<NightResolved>(OnNightResolved);
            EventBus.Subscribe<FloorBuiltEvent>(OnFloorBuilt);
            EventBus.Subscribe<RoofUpdateNeeded>(OnRoofUpdateNeeded);
            EventBus.Subscribe<GuestsRenderRequested>(OnGuestsRenderRequested);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<GameStateChanged>(OnGameState);
            EventBus.Unsubscribe<RoomPurchasedEvent>(OnRoomPurchased);
            EventBus.Unsubscribe<RoomUpgradedEvent>(OnRoomUpgraded);
            EventBus.Unsubscribe<NightResolved>(OnNightResolved);
            EventBus.Unsubscribe<FloorBuiltEvent>(OnFloorBuilt);
            EventBus.Unsubscribe<RoofUpdateNeeded>(OnRoofUpdateNeeded);
            EventBus.Unsubscribe<GuestsRenderRequested>(OnGuestsRenderRequested);
        }

        private void OnGuestsRenderRequested(GuestsRenderRequested _)
        {
            AudioManager.Instance.PlaySfx("Guest_in");
            StartCoroutine(Co_BuildGuestsThenSync());
        }

        void Start()
        {
            if (!game) game = FindObjectOfType<Game>();
            BuildInitialViews();
        }

        // ===== Entry points =====
        private void BuildInitialViews()
        {
            var w = game.World;
            if (w == null) return;

            BuildInitialRooms();
            BuildInitialGhosts();
            BuildInitialGuests();
            UpdateRoofPosition();
        }

        private void SyncAll()
        {
            // 房间外观刷新
            foreach (var r in game.World.Rooms)
                if (_roomViews.TryGetValue(r.Id, out var rv)) rv.Refresh(r);

            // 队列 & 商店
            SyncGuestsQueue();
            SyncShop();
        }

        // ===== Utils =====
        private static void SafeDestroy(UnityEngine.Object o)
        {
            if (o) Destroy(o);
        }
    }
}
