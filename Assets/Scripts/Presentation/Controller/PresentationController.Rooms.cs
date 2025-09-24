using System.Linq;
using UnityEngine;

namespace ScreamHotel.Presentation
{
    public partial class PresentationController
    {
        private void BuildInitialRooms()
        {
            var w = game.World;
            foreach (var r in w.Rooms.OrderBy(x => x.Id))
            {
                if (_roomViews.ContainsKey(r.Id)) continue;
                var rv = Instantiate(roomPrefab, roomsRoot);
                rv.transform.position = GetRoomSpawnPositionById(r.Id);
                rv.Bind(r);
                _roomViews[r.Id] = rv;
            }
        }
    }
}