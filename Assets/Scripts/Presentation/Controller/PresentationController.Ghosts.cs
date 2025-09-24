using UnityEngine;
using Random = UnityEngine.Random;

namespace ScreamHotel.Presentation
{
    public partial class PresentationController
    {
        private void BuildInitialGhosts()
        {
            var w = game.World;
            foreach (var g in w.Ghosts)
            {
                if (_ghostViews.ContainsKey(g.Id)) continue;
                var pv = Instantiate(ghostPrefab, ghostsRoot);
                pv.BindGhost(g);
                pv.SnapTo(GetRandomGhostSpawnPos());
                _ghostViews[g.Id] = pv;
            }
        }

        private Vector3 GetRandomGhostSpawnPos()
        {
            if (!ghostSpawnRoomRoot) return ghostsRoot ? ghostsRoot.position : Vector3.zero;
            var box = ghostSpawnRoomRoot.GetComponentInChildren<BoxCollider>();
            if (!box) return ghostSpawnRoomRoot.position;

            var size = box.size; var center = box.center;
            float rx = Random.Range(-size.x * 0.5f, size.x * 0.5f);
            float ry = Random.Range(-size.y * 0.5f, size.y * 0.5f);
            var local = new Vector3(center.x + rx, center.y + ry, spawnFixedZ);
            var world = box.transform.TransformPoint(local);
            world.z = spawnFixedZ;
            return world;
        }

        // 待命位（如需）
        public Transform GetStagingTransform(string ghostId) => GetStagingTransform(ghostId, GetIndexForGhost(ghostId));
        public Transform GetStagingTransform(string ghostId, int suggestedIndex)
        {
            if (_stagingByGhost.TryGetValue(ghostId, out var t) && t) return t;
            t = new GameObject($"Staging_{ghostId}").transform;
            t.position = GetStagingPosForIndex(suggestedIndex);
            _stagingByGhost[ghostId] = t;
            return t;
        }

        private Vector3 GetStagingPosForIndex(int index)
        {
            float spacing = 2f;
            var basePos = ghostsRoot ? ghostsRoot.position : Vector3.zero;
            return basePos + new Vector3(index * spacing, 0, 0);
        }

        private int GetIndexForGhost(string ghostId)
        {
            int i = 0;
            foreach (var id in game.World.Ghosts)
            {
                if (id.Id == ghostId) return i;
                i++;
            }
            return _stagingByGhost.Count;
        }
    }
}
