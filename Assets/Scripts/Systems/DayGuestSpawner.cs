using System.Collections.Generic;
using System.Linq;
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
        if (count <= 0)
        {
            Debug.LogWarning("[DayGuestSpawner] SpawnGuests called with count <= 0");
            return 0;
        }

        var typeIds = _db?.GuestTypes?.Keys?.ToList();
        if (typeIds == null || typeIds.Count == 0)
        {
            Debug.LogWarning("[DayGuestSpawner] Database.GuestTypes 为空，无法生成客人。");
            return 0;
        }

        Debug.Log($"[DayGuestSpawner] 准备生成 {count} 位客人，可用客人类型数量: {typeIds.Count}");

        int spawned = 0;
        for (int i = 0; i < count; i++)
        {
            string id = $"Guest_{++_seq:0000}";
            string typeId = typeIds[_rng.Next(typeIds.Count)];

            _db.GuestTypes.TryGetValue(typeId, out var typeCfg);

            var g = new Guest
            {
                Id = id,
                TypeId = typeId,
                Fears = (typeCfg != null && typeCfg.immunities != null && typeCfg.immunities.Count > 0)
                    ? new List<FearTag>(typeCfg.immunities)
                    : new List<FearTag>(),
                BaseFee = typeCfg != null ? typeCfg.baseFee : 50,
                BarMax = typeCfg != null ? typeCfg.barMax : 100f,
                RequiredPercent = typeCfg != null ? typeCfg.requiredPercent : 0.8f,
            };

            _world.Guests.Add(g);
            spawned++;

            // 打印详细调试信息
            string fearList = g.Fears != null && g.Fears.Count > 0
                ? string.Join(", ", g.Fears)
                : "(无)";
            Debug.Log($"[DayGuestSpawner] 已生成客人 {g.Id} | 类型: {g.TypeId} | 恐惧标签: {fearList} | 基础费用: {g.BaseFee}");
        }

        Debug.Log($"[DayGuestSpawner] 成功生成 {spawned} 位客人，总数现为 {_world.Guests.Count}");
        return spawned;
    }
}
