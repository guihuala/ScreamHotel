using System.Linq;
using ScreamHotel.Domain;
using UnityEngine;
using ScreamHotel.Core;

namespace ScreamHotel.Systems
{
    public class AssignmentSystem
    {
        private readonly World _world;
        private readonly Game _game;

        public AssignmentSystem(World world, Game game)
        {
            _world = world;
            _game  = game;
        }

        private bool IsAssignPhase()
        {
            if (_game == null) return false;
            if (_game.State != GameState.NightShow)
            {
                Debug.LogWarning("[Assign] 仅在 NightShow 阶段允许分配/撤销分配");
                return false;
            }
            return true;
        }

        public bool TryAssignGhostToRoom(string ghostId, string roomId)
        {
            if (!IsAssignPhase()) return false;

            var g = _world.Ghosts.FirstOrDefault(x => x.Id == ghostId);
            var r = _world.Rooms.FirstOrDefault(x => x.Id == roomId);
            
            if (g == null || r == null)
            {
                return false;
            }

            if (g.State is GhostState.Training)
            {
                return false;
            }

            if (r.AssignedGhostIds.Count >= r.Capacity)
            {
                Debug.LogWarning($"房间 {roomId} 已满，无法分配鬼怪 {ghostId}！");
                return false;
            }

            foreach (var room in _world.Rooms)
                room.AssignedGhostIds.Remove(ghostId);

            if (!r.AssignedGhostIds.Contains(ghostId))
            {
                r.AssignedGhostIds.Add(ghostId);
                g.State = GhostState.Working;
                return true;
            }

            return false;
        }
        
        public bool TryAssignGuestToRoom(string guestId, string roomId)
        {
            if (!IsAssignPhase()) return false;

            var g = _world.Guests.FirstOrDefault(x => x.Id == guestId);
            var r = _world.Rooms.FirstOrDefault(x => x.Id == roomId);
            

            if (g == null || r == null)
            {
                return false;
            }

            foreach (var rr in _world.Rooms) rr.AssignedGuestIds.Remove(guestId);

            if (!r.AssignedGuestIds.Contains(guestId))
            {
                r.AssignedGuestIds.Add(guestId);
                return true;
            }

            return false;
        }

        public bool UnassignGhost(string ghostId)
        {
            if (!IsAssignPhase()) return false;

            var g = _world.Ghosts.FirstOrDefault(x => x.Id == ghostId);
            if (g == null)
            {
                Debug.LogWarning($"鬼怪 {ghostId} 未找到！");
                return false;
            }

            var removed = false;
            foreach (var r in _world.Rooms)
                removed |= r.AssignedGhostIds.Remove(ghostId);

            if (g.State == GhostState.Working) g.State = GhostState.Idle;

            if (removed)
                Debug.Log($"成功移除鬼怪 {ghostId} 的分配！");
            else
                Debug.LogWarning($"鬼怪 {ghostId} 没有被分配到任何房间！");
            
            return removed;
        }
        
        public bool UnassignGuest(string guestId)
        {
            if (!IsAssignPhase()) return false;

            var g = _world.Guests.FirstOrDefault(x => x.Id == guestId);
            if (g == null)
            {
                Debug.LogWarning($"客人 {guestId} 未找到！");
                return false;
            }

            var removed = false;
            foreach (var r in _world.Rooms) removed |= r.AssignedGuestIds.Remove(guestId);

            if (removed)
                Debug.Log($"成功移除客人 {guestId} 的分配！");
            else
                Debug.LogWarning($"客人 {guestId} 没有被分配到任何房间！");

            return removed;
        }

        public void ClearAssignments()
        {
            if (!IsAssignPhase()) return;

            foreach (var r in _world.Rooms) r.AssignedGhostIds.Clear();
            foreach (var g in _world.Ghosts) if (g.State == GhostState.Working) g.State = GhostState.Idle;

            Debug.Log("所有分配已清除！");
        }
    }
}
