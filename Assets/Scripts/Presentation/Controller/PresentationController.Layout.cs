using UnityEngine;

namespace ScreamHotel.Presentation
{
    public partial class PresentationController
    {
        private int GetHighestFloor()
        {
            int maxFloor = 0;
            foreach (var roomId in _roomViews.Keys)
            {
                if (TryParseRoomId(roomId, out int floor, out _))
                    maxFloor = Mathf.Max(maxFloor, floor);
            }
            return maxFloor;
        }

        private void UpdateRoofPosition()
        {
            if (!roofPrefab) return;
            int highest = GetHighestFloor();
            float topY = roomBaseY + highest * floorSpacing;

            if (!_currentRoof) _currentRoof = Instantiate(roofPrefab, roomsRoot);
            _currentRoof.position = roomsRoot ? roomsRoot.TransformPoint(new Vector3(0f, topY, 0f))
                                              : new Vector3(0f, topY, 0f);
        }

        private bool TryParseRoomId(string roomId, out int floor, out string slot)
        {
            floor = 0; slot = null;
            if (string.IsNullOrEmpty(roomId)) return false;
            var fIdx = roomId.IndexOf("_F", System.StringComparison.Ordinal);
            if (fIdx < 0) return false;
            var usIdx = roomId.IndexOf('_', fIdx + 2);
            if (usIdx < 0) return false;
            var num = roomId.Substring(fIdx + 2, usIdx - (fIdx + 2));
            if (!int.TryParse(num, out floor)) return false;
            slot = roomId[(usIdx + 1)..]; // LA/LB/RA/RB
            return true;
        }

        private Vector3 GetRoomSpawnPositionById(string roomId)
        {
            if (!TryParseRoomId(roomId, out var floor, out var slot))
                return roomsRoot ? roomsRoot.position : Vector3.zero;

            float ly = roomBaseY + (floor - 1) * floorSpacing;
            var floorCenterLocal = new Vector3(0f, ly, 0f);

            float lx = slot switch
            {
                "LA" => -xInner,
                "LB" => -xOuter,
                "RA" =>  xInner,
                "RB" =>  xOuter,
                _    =>  0f,
            };

            var local = floorCenterLocal + new Vector3(lx, 0f, 0f);
            TrySpawnElevatorOnce(floor, floorCenterLocal);
            return roomsRoot ? roomsRoot.TransformPoint(local) : local;
        }

        private void TrySpawnElevatorOnce(int floor, Vector3 floorCenterLocal)
        {
            if (!elevatorPrefab || _elevatorsSpawned.Contains(floor)) return;
            var t = Instantiate(elevatorPrefab, roomsRoot);
            t.position = roomsRoot ? roomsRoot.TransformPoint(floorCenterLocal) : floorCenterLocal;
            t.name = $"Elevator_F{floor}";
            _elevatorsSpawned.Add(floor);
        }
    }
}
