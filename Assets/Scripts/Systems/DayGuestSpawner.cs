using System.Collections.Generic;
using System.Linq;
using ScreamHotel.Core;
using ScreamHotel.Domain;

namespace ScreamHotel.Systems
{
    public class DayGuestSpawner
    {
        private readonly World _world;
        private readonly System.Random _rng = new();

        public DayGuestSpawner(World w)
        {
            _world = w;
        }
        
        private int _seq = 0;
        
        public int SpawnGuests(int count)
        {
            int spawned = 0;

            // 随机恐惧枚举（保留）
            var fearValues = (FearTag[])System.Enum.GetValues(typeof(FearTag));

            // 从配置数据库获取所有 guestType id
            var guestTypeIds = _world.Config.GuestTypes.Keys.ToList();

            for (int i = 0; i < count; i++)
            {
                string id = $"Guest_{++_seq:0000}";

                // 随机选一个类型ID
                string typeId = guestTypeIds[_rng.Next(guestTypeIds.Count)];

                var guest = new Guest
                {
                    Id = id,
                    Fears = new List<FearTag>
                    {
                        fearValues[_rng.Next(fearValues.Length)]
                    },
                    BarMax = 100f,
                    RequiredPercent = 0.6f,
                    BaseFee = 50,
                    TypeId = typeId
                };

                _world.Guests.Add(guest);
                EventBus.Raise(new GuestSpawnedEvent(id));
                spawned++;
            }

            return spawned;
        }

    }

    public readonly struct GuestSpawnedEvent
    {
        public readonly string Id;

        public GuestSpawnedEvent(string id)
        {
            Id = id;
        }
    }
}