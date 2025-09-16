using UnityEngine;
using System.Linq;
using ScreamHotel.Domain;
using ScreamHotel.Core;

namespace ScreamHotel.Dev
{
    /// <summary>
    /// 注入最小可玩数据（2 个鬼 + 1~2 间房 + 金币）。
    /// </summary>
    public class SampleDataSeeder : MonoBehaviour
    {
        public ScreamHotel.Core.Game game;
        public bool autoSeedOnStart = true;

        private void Start()
        {
            if (autoSeedOnStart) SeedIfNeeded();
        }

        [ContextMenu("Seed Now")]
        public void SeedIfNeeded()
        {
            if (game == null) game = FindObjectOfType<ScreamHotel.Core.Game>();
            if (game == null || game.World == null) { Debug.LogWarning("[Seeder] Game/World not ready."); return; }

            var w = game.World;

            // 鬼怪：若不足 2 个，补齐 2 个
            if (w.Ghosts.Count < 2)
            {
                if (!w.Ghosts.Any(g => g.Id == "G1"))
                    w.Ghosts.Add(new Ghost { Id = "G1", Main = FearTag.Darkness, BaseScare = 35, Fatigue = 0, State = GhostState.Idle });
                if (!w.Ghosts.Any(g => g.Id == "G2"))
                    w.Ghosts.Add(new Ghost { Id = "G2", Main = FearTag.Blood, BaseScare = 40, Fatigue = 0, State = GhostState.Idle });
            }

            // 房间：若没有，放一间 Lv1；若只有 1 间，补一间 Lv3（便于测试容量）
            if (!w.Rooms.Any(r => r.Id == "Room_01"))
                w.Rooms.Add(new Room { Id = "Room_01", Level = 1, Capacity = 1, RoomTag = null });
            if (w.Rooms.Count < 2 && !w.Rooms.Any(r => r.Id == "Room_02"))
                w.Rooms.Add(new Room { Id = "Room_02", Level = 3, Capacity = 2, RoomTag = FearTag.Darkness });

            // 金币：至少 500，便于测试买房升级
            if (w.Economy.Gold < 500)
                w.Economy.Gold = 500;

            EventBus.Raise(new GoldChanged(w.Economy.Gold));
            Debug.Log("[Seeder] Seeded: Ghosts=" + w.Ghosts.Count + ", Rooms=" + w.Rooms.Count + ", Gold=" + w.Economy.Gold);
        }
    }
}