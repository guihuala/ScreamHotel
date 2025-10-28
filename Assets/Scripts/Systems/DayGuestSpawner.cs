using System;
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
        Debug.Log($"[DayGuestSpawner] Generating {count} candidates for today.");
    
        ClearAll(); // 每天重新生成
        if (count <= 0)
        {
            return 0;
        }

        var types = _db?.GuestTypes?.Values?.ToList();
        if (types == null || types.Count == 0)
        {
            return 0;
        }

        int spawned = 0;
        for (int i = 0; i < count; i++)
        {
            var typeCfg = types[_rng.Next(types.Count)];
            string id = $"Guest_{++_seq:0000}";

            int immunityCount = typeCfg.maxImmunitiesCount;
            List<FearTag> generatedImmunities = new List<FearTag>();
            if (immunityCount > 0)
            {
                var allFearTags = Enum.GetValues(typeof(FearTag)).Cast<FearTag>().ToList();
                var availableFears = new List<FearTag>(allFearTags);

                for (int j = 0; j < immunityCount; j++)
                {
                    var randomIndex = _rng.Next(availableFears.Count);
                    generatedImmunities.Add(availableFears[randomIndex]);
                    availableFears.RemoveAt(randomIndex);
                }
            }

            var g = new Guest
            {
                Id = id,
                TypeId = typeCfg.id,
                Immunities = generatedImmunities,
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

    public int FlushAcceptedToWorld()
    {
        if (_world == null) return 0;
        int n = 0;
    
        // 只将已接受的客人加入到 _world.Guests 中
        foreach (var g in _accepted)
        {
            if (!_world.Guests.Any(x => x.Id == g.Id))
            {
                _world.Guests.Add(g);  // 这里是将已接受的客人加入到世界中
                n++;
            }
        }
    
        // 清空待接受和已接受的客人列表
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
