using ScreamHotel.Data;
using ScreamHotel.Domain;
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
                
                var pos = GetRandomGhostSpawnPos();
                
                var pv = Instantiate(ghostPrefab, pos, Quaternion.identity, ghostsRoot);
                pv.BindGhost(g);

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
    }
}
