using System;
using System.Collections.Generic;
using UnityEngine;
using ScreamHotel.Domain;

[CreateAssetMenu(fileName = "FearIconAtlas", menuName = "ScreamHotel/UI/Fear Icon Atlas")]
public class FearIconAtlas : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public FearTag tag;
        public Sprite icon;
    }

    public List<Entry> entries = new List<Entry>();

    private Dictionary<FearTag, Sprite> _map;

    void OnEnable()
    {
        _map = new Dictionary<FearTag, Sprite>();
        foreach (var e in entries)
            if (e.icon)
                _map[e.tag] = e.icon;
    }

    public Sprite Get(FearTag tag) => (_map != null && _map.TryGetValue(tag, out var s)) ? s : null;
}