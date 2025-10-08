using UnityEngine;
using System.Linq;
using ScreamHotel.Domain;
using ScreamHotel.Systems;

namespace ScreamHotel.Presentation
{
    [RequireComponent(typeof(Collider))]
    public class RoomDropZone : MonoBehaviour, IDropZone
    {
        [Header("Binding")] 
        public MeshRenderer plate;
        public Color canColor = new Color(0.3f, 1f, 0.3f, 1f);
        public Color fullColor = new Color(1f, 0.3f, 0.3f, 1f);

        private Core.Game game;
        private RoomView _rv;  // 引用 RoomView
        private string roomId;  // 房间ID（从RoomView获取）
        private AssignmentSystem _assign => GetSystem<AssignmentSystem>(game, "_assignmentSystem");
        private Color _origColor;

        void Awake()
        {
            if (plate != null)
            {
                _origColor = plate.sharedMaterial.color;
            }

            if (game == null) game = FindObjectOfType<Core.Game>();

            // 获取 RoomView 组件
            _rv = GetComponent<RoomView>();
            if (_rv != null)
            {
                // 等待 Bind 方法设置 roomId
                Debug.Log("等待 RoomView 完成绑定...");
            }
        }

        // 用于在 RoomView 绑定完成后设置 roomId
        public void SetRoomId(string newRoomId)
        {
            roomId = newRoomId;
            Debug.Log($"RoomDropZone 设置 roomId 为 {roomId}");
        }

        public bool CanAccept(string id, bool isGhost = true)
        {
            var w = game.World;
            var r = w.Rooms.FirstOrDefault(x => x.Id == roomId);
            if (r == null)
            {
                Debug.LogWarning($"房间 {roomId} 未找到！");
                return false;
            }

            // 判断是鬼怪还是客人，分别处理
            if (isGhost)
            {
                var g = w.Ghosts.FirstOrDefault(x => x.Id == id);
                if (g == null)
                {
                    Debug.LogWarning($"鬼怪 {id} 未找到！");
                    return false;
                }

                if (g.State is GhostState.Training)
                {
                    Debug.LogWarning($"鬼怪 {id} 处于不可分配状态！");
                    return false;
                }

                if (r.AssignedGhostIds.Count >= r.Capacity)
                {
                    Debug.LogWarning($"房间 {roomId} 已满，无法分配鬼怪 {id}！");
                    return false;
                }
            }
            else
            {
                var guest = w.Guests.FirstOrDefault(x => x.Id == id);
                if (guest == null)
                {
                    Debug.LogWarning($"客人 {id} 未找到！");
                    return false;
                }

                if (r.AssignedGuestIds.Count >= r.Capacity)
                {
                    Debug.LogWarning($"房间 {roomId} 已满，无法分配客人 {id}！");
                    return false;
                }
            }

            return true;
        }

        public bool TryDrop(string id, bool isGhost, out Transform targetAnchor)
        {
            targetAnchor = null;
            var w = game.World;
            var r = w.Rooms.FirstOrDefault(x => x.Id == roomId);
            if (r == null)
            {
                Debug.LogWarning($"房间 {roomId} 未找到！");
                return false;
            }

            Debug.Log($"开始尝试放置 {id} 到房间 {roomId}");

            if (!CanAccept(id, isGhost))
            {
                Debug.LogWarning($"无法放置 {id} 到房间 {roomId}");
                Flash(fullColor);
                return false;
            }

            if (isGhost)
            {
                if (_assign.TryAssignGhostToRoom(id, roomId))
                {
                    var index = r.AssignedGhostIds.IndexOf(id);
                    var rv = GetComponent<RoomView>();
                    targetAnchor = rv != null ? rv.GetAnchor(index) : transform;
                    Debug.Log($"鬼怪 {id} 成功分配到房间 {roomId}");
                    Flash(canColor);
                    return true;
                }
            }
            else
            {
                if (_assign.TryAssignGuestToRoom(id, roomId))
                {
                    var idx = Mathf.Max(0, r.AssignedGuestIds.IndexOf(id));
                    var rv = GetComponent<RoomView>();
                    targetAnchor = rv != null ? rv.GetGuestAnchor(idx) : transform;
                    Debug.Log($"客人 {id} 成功分配到房间 {roomId}");
                    Flash(canColor);
                    return true;
                }
            }

            Flash(fullColor);
            Debug.LogWarning($"放置失败，无法将 {id} 放置到房间 {roomId}");
            return false;
        }

        public void ShowHoverFeedbackGuest()
        {
            if (plate == null) return;
            plate.material.color = canColor;
        }

        public void ShowHoverFeedback(string ghostId)
        {
            if (plate == null) return;
            var c = CanAccept(ghostId, true) ? canColor : fullColor;
            plate.material.color = Color.Lerp(plate.material.color, c, 0.5f);
        }

        public void ClearFeedback()
        {
            if (plate == null) return;
            plate.material.color = _origColor;
        }
        
        // 统一的反馈接口
        public void ShowHoverFeedback(string id, bool isGhost)
        {
            if (isGhost) ShowHoverFeedback(id);
            else ShowHoverFeedbackGuest();
        }

        private static T GetSystem<T>(object obj, string field) where T : class
        {
            var f = obj.GetType().GetField(field, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return f?.GetValue(obj) as T;
        }

        private void Flash(Color c)
        {
            if (plate == null) return;
            plate.material.color = c;
            CancelInvoke(nameof(Revert));
            Invoke(nameof(Revert), 0.25f);
        }

        private void Revert()
        {
            if (plate != null) plate.material.color = _origColor;
        }
    }
}