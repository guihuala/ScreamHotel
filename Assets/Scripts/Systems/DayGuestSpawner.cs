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

    // —— 白天阶段的候选清单（不写入 world）——
    private readonly List<Guest> _pending = new List<Guest>();
    // —— 当天已接受（仍未写入 world，等 NightShow 再落库）——
    private readonly List<Guest> _accepted = new List<Guest>();
    public IReadOnlyList<Guest> Pending => _pending;

    public DayGuestSpawner(World world, ConfigDatabase db)
    {
        _world = world;
        _db = db;
        _seq = (_world?.Guests?.Count ?? 0);
    }
    
    public int GenerateCandidates(int count)
    {
        ClearAll(); // 每天重新生成
        if (count <= 0)
        {
            Debug.LogWarning("[DayGuestSpawner] GenerateCandidates called with count <= 0");
            return 0;
        }

        var types = _db?.GuestTypes?.Values?.ToList();
        if (types == null || types.Count == 0)
        {
            Debug.LogWarning("[DayGuestSpawner] Database.GuestTypes 为空，无法生成客人。");
            return 0;
        }

        int spawned = 0;
        for (int i = 0; i < count; i++)
        {
            // 统一从“全部类型”里均匀随机，不再区分难/易类型
            var typeCfg = types[_rng.Next(types.Count)];
            string id = $"Guest_{++_seq:0000}";
            
            var g = new Guest
            {
                Id = id,
                TypeId = typeCfg.id,
                Fears = (typeCfg.immunities != null && typeCfg.immunities.Count > 0)
                    ? new List<FearTag>(typeCfg.immunities)
                    : new List<FearTag>(),
                BaseFee = typeCfg.baseFee,
                BarMax = typeCfg.barMax,
                RequiredPercent = typeCfg.requiredPercent,
            };

            _pending.Add(g);
            spawned++;
        }

        return spawned;
    }
    
    public bool Accept(string guestId)
    {
        var g = _pending.FirstOrDefault(x => x.Id == guestId);
        if (g == null) return false;
        _pending.Remove(g);
        _accepted.Add(g);
        return true;
    }

    public bool Reject(string guestId)
    {
        var g = _pending.FirstOrDefault(x => x.Id == guestId);
        if (g == null) return false;
        _pending.Remove(g);
        return true;
    }

    /// <summary>NightShow 阶段：将已接受顾客写入 world</summary>
    public int FlushAcceptedToWorld()
    {
        if (_world == null) return 0;
        int n = 0;
        foreach (var g in _accepted)
        {
            if (!_world.Guests.Any(x => x.Id == g.Id))
            {
                _world.Guests.Add(g);
                n++;
            }
        }
        _accepted.Clear();
        _pending.Clear();
        return n;
    }

    public void ClearAll()
    {
        _pending.Clear();
        _accepted.Clear();
    }
}
