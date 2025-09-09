using System.Linq;
using ScreamHotel.Domain;

namespace ScreamHotel.Systems
{
    
    // 白天分配鬼怪到房间
    public class AssignmentSystem
    {
        private readonly World _world;
        public AssignmentSystem(World world) { _world = world; }

        public bool TryAssignGhostToRoom(string ghostId, string roomId)
        {
            var g = _world.Ghosts.FirstOrDefault(x => x.Id == ghostId);
            var r = _world.Rooms.FirstOrDefault(x => x.Id == roomId);
            if (g == null || r == null) return false;
            if (g.State is GhostState.Resting or GhostState.Training or GhostState.Injured) return false;
            if (r.AssignedGhostIds.Count >= r.Capacity) return false;

            if (!r.AssignedGhostIds.Contains(ghostId)) r.AssignedGhostIds.Add(ghostId);
            g.State = GhostState.Working;
            return true;
        }

        public void ClearAssignments()
        {
            foreach (var r in _world.Rooms) r.AssignedGhostIds.Clear();
            foreach (var g in _world.Ghosts) if (g.State == GhostState.Working) g.State = GhostState.Idle;
        }
    }
}
