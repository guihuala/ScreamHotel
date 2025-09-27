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
            
            var fearValues = (FearTag[])System.Enum.GetValues(typeof(FearTag));
            var typeValues =
                (GuestType[])System.Enum.GetValues(typeof(GuestType));

            for (int i = 0; i < count; i++)
            {
                string id = $"Guest_{++_seq:0000}";
                
                var guest = new ScreamHotel.Domain.Guest
                {
                    Id = id,
                    // 给一点基础数据，避免后续空引用
                    Fears = new System.Collections.Generic.List<FearTag>
                    {
                        fearValues[_rng.Next(fearValues.Length)]
                    },
                    BarMax = 100f,
                    RequiredPercent = 0.6f,
                    BaseFee = 50,
                    Type = typeValues[_rng.Next(typeValues.Length)]
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