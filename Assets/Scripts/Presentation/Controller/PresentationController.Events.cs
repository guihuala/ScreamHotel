using System.Linq;
using UnityEngine;
using ScreamHotel.Core;

namespace ScreamHotel.Presentation
{
    public partial class PresentationController
    {
        private void OnGameState(GameStateChanged e)
        {
            if (e.State.Equals(GameState.Day))
            {
                // 进入白天：全量对齐
                SyncAll();
            }
            else if (e.State.Equals(GameState.NightShow))
            {
                // 鬼 → 房间锚点
                var w = game.World;
                foreach (var room in w.Rooms)
                {
                    for (int i = 0; i < room.AssignedGhostIds.Count; i++)
                    {
                        var gid = room.AssignedGhostIds[i];
                        if (_ghostViews.TryGetValue(gid, out var pawn) && _roomViews.TryGetValue(room.Id, out var rv))
                        {
                            var anchor = rv.GetAnchor(i);
                            pawn.MoveTo(anchor, 0.5f);
                        }
                    }
                }

                // 客 → 房间“客人锚点”（无则兜底到鬼锚点）
                foreach (var room in w.Rooms)
                {
                    for (int i = 0; i < room.AssignedGuestIds.Count; i++)
                    {
                        var id = room.AssignedGuestIds[i];
                        if (_guestViews.TryGetValue(id, out var gv) && _roomViews.TryGetValue(room.Id, out var rv))
                        {
                            if (rv.TryGetGuestAnchor(i, out var gAnchor))
                                gv.MoveTo(gAnchor, 0.5f);
                            else
                                gv.MoveTo(rv.GetAnchor(i), 0.5f);
                        }
                    }
                }
            }
        }

        private void OnNightResolved(NightResolved _)
        {
            // 鬼 → 独立房间随机点
            foreach (var kv in _ghostViews)
            {
                var p = GetRandomGhostSpawnPos();
                var tmp = new GameObject("tmpTarget").transform; tmp.position = p;
                kv.Value.MoveTo(tmp, 0.4f);
                SafeDestroy(tmp.gameObject);
            }

            // 客 → 回队列按序站好
            var order = game.World.Guests.Select((g, i) => new { g.Id, Index = i }).ToList();
            foreach (var item in order)
            {
                if (_guestViews.TryGetValue(item.Id, out var gv))
                {
                    var tmp = new GameObject("tmpTarget").transform; tmp.position = GetGuestQueueWorldPos(item.Index);
                    gv.MoveTo(tmp, 0.4f);
                    SafeDestroy(tmp.gameObject);
                }
            }
        }

        private void OnRoomPurchased(RoomPurchasedEvent ev)
        {
            var r = game.World.Rooms.First(x => x.Id == ev.RoomId);
            var rv = Instantiate(roomPrefab, roomsRoot);
            rv.transform.position = GetRoomSpawnPositionById(r.Id);
            rv.Bind(r);
            _roomViews[r.Id] = rv;
        }

        private void OnRoomUpgraded(RoomUpgradedEvent ev)
        {
            if (_roomViews.TryGetValue(ev.RoomId, out var rv))
            {
                var r = game.World.Rooms.First(x => x.Id == ev.RoomId);
                rv.Refresh(r);
            }
        }

        private void OnFloorBuilt(FloorBuiltEvent _)
        {
            BuildInitialRooms();

            // 立刻上移屋顶到最高层之上
            UpdateRoofPosition();
        }
        
        private void OnRoofUpdateNeeded(RoofUpdateNeeded _)
        {
            BuildInitialRooms();
            UpdateRoofPosition();
        }
    }
}
