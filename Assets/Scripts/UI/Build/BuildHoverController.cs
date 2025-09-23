using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using ScreamHotel.Core;
using ScreamHotel.Systems;

namespace ScreamHotel.UI
{
    public class BuildHoverController : MonoBehaviour
    {
        [Header("Refs")]
        public Camera mainCamera;
        public RoomHoverPanel roomPanel;    // 屏幕空间UI
        public RoofHoverPanel roofPanel;    // 屏幕空间UI

        [Header("Raycast")]
        public LayerMask interactMask = ~0; // 默认全打
        public float rayMaxDistance = 500f;
        
        private Game _game;
        private string _lastRoomId; // 记录上一次悬停的房间ID

        void Awake()
        {
            if (!mainCamera) mainCamera = Camera.main;
            _game = FindObjectOfType<Game>();
            if (roomPanel) roomPanel.Hide();
            if (roofPanel) roofPanel.Hide();
        }

        void Update()
        {
            // 鼠标在别的UI上时，不进行射线
            if (EventSystem.current && EventSystem.current.IsPointerOverGameObject())
            { HideAll(); return; }

            var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit, rayMaxDistance, interactMask))
            { HideAll(); return; }

            // 先判楼顶
            var roof = hit.collider.GetComponentInParent<RoofBuildZone>();
            if (roof != null)
            {
                var nextFloor = roof.GetNextFloor();
                var cost = roof.GetNextFloorCost();
                roofPanel.Show(nextFloor, cost, Input.mousePosition);

                // 取消房间面板的显示
                roomPanel?.Hide();
                _lastRoomId = null;
                return;
            }

            // 再判房间
            string roomId = null;
            var rv = hit.collider.GetComponentInParent<ScreamHotel.Presentation.RoomView>();
            if (rv != null) roomId = rv.roomId;
            else
            {
                var drop = hit.collider.GetComponentInParent<ScreamHotel.Presentation.RoomDropZone>();
                if (drop != null) roomId = drop.roomId;
            }

            if (!string.IsNullOrEmpty(roomId))
            {
                // 只有当进入新房间时才更新显示（避免频繁刷新）
                if (roomId != _lastRoomId)
                {
                    // 获取房间的世界位置（使用RoomView的位置）
                    Vector3 roomWorldPosition = GetRoomWorldPosition(roomId);
                    roomPanel.Show(roomId, roomWorldPosition);
                    _lastRoomId = roomId;
                }
                roofPanel?.Hide();
                return;
            }

            HideAll();
            _lastRoomId = null;
        }

        private Vector3 GetRoomWorldPosition(string roomId)
        {
            // 查找RoomView获取房间的实际位置
            var roomViews = FindObjectsOfType<ScreamHotel.Presentation.RoomView>();
            var roomView = roomViews.FirstOrDefault(rv => rv.roomId == roomId);
            return roomView != null ? roomView.transform.position : Vector3.zero;
        }

        private void HideAll()
        {
            roomPanel?.Hide();
            roofPanel?.Hide();
            _lastRoomId = null;
        }
    }
}