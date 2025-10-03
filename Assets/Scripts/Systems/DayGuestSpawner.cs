using System.Collections.Generic;
using System.Linq;
using ScreamHotel.Core;
using UnityEngine;
using ScreamHotel.Domain;
using ScreamHotel.Data;

public class DayGuestSpawner
{
    private readonly World _world;
    private readonly ConfigDatabase _db;
    private readonly System.Random _rng = new System.Random();
    private int _seq;

    public DayGuestSpawner(World world, ConfigDatabase db)
    {
        _world = world;
        _db = db;
        _seq = _world?.Guests?.Count ?? 0;
    }
    
    public int SpawnGuests(int count)
    {
        if (count <= 0) return 0;

        // 1) 取所有类型 id（key）
        var typeIds = _db?.GuestTypes?.Keys?.ToList();
        if (typeIds == null || typeIds.Count == 0)
        {
            Debug.LogWarning("[DayGuestSpawner] Database.GuestTypes 为空，无法生成客人。");
            return 0;
        }

        // 2) 备用：随机弱点（当前结算依赖 g.Fears）
        var fearValues = (FearTag[])System.Enum.GetValues(typeof(FearTag));

        int spawned = 0;
        for (int i = 0; i < count; i++)
        {
            string id = $"Guest_{++_seq:0000}";
            string typeId = typeIds[_rng.Next(typeIds.Count)];

            // 3) 查类型配置（可为空）
            _db.GuestTypes.TryGetValue(typeId, out var typeCfg);

            // 4) 生成 Guest，并尽可能从配置带出数值
            var g = new Guest
            {
                Id = id,
                TypeId = typeId,

                // 结算所需：给一个弱点
                Fears = new List<FearTag> { fearValues[_rng.Next(fearValues.Length)] },

                // 数值从配置带出
                BaseFee = typeCfg != null ? typeCfg.baseFee : 50,
                BarMax = typeCfg != null ? typeCfg.barMax : 100f,
                RequiredPercent = typeCfg != null ? typeCfg.requiredPercent : 0.8f,
            };

            // 5) 免疫从配置带出
            if (typeCfg != null && typeCfg.immunities != null && typeCfg.immunities.Count > 0)
            {
                g.Fears = new List<FearTag>(typeCfg.immunities);
            }

            _world.Guests.Add(g);
            spawned++;
        }

        return spawned;
    }
}
