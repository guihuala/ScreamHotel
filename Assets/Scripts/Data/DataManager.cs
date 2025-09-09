using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ScreamHotel.Data
{
    public class DataManager : MonoBehaviour
    {
        public bool useResources = true;
        public string resourcesRoot = "Configs";
        public string jsonFolder = "Configs";

        public ConfigDatabase Database { get; private set; } = new();

        public void Initialize()
        {
            LoadFromScriptableObjects();
            TryOverrideFromJson();
#if UNITY_EDITOR
            Debug.Log("[DataManager] Loaded DB: " + Database);
#endif
        }

        private void LoadFromScriptableObjects()
        {
            if (useResources)
            {
                var ghostSets = Resources.LoadAll<GhostConfig>(resourcesRoot);
                foreach (var g in ghostSets) Database.Ghosts[g.id] = g;

                var guestTypes = Resources.LoadAll<GuestTypeConfig>(resourcesRoot);
                foreach (var gt in guestTypes) Database.GuestTypes[gt.id] = gt;

                var roomPrices = Resources.LoadAll<RoomPriceConfig>(resourcesRoot);
                foreach (var rp in roomPrices) Database.RoomPrices[rp.id] = rp;

                var prog = Resources.Load<ProgressionConfig>($"{resourcesRoot}/Progression");
                if (prog != null) Database.Progression = prog;

                var rules = Resources.Load<GameRuleConfig>($"{resourcesRoot}/GameRules");
                if (rules != null) Database.Rules = rules;
            }
        }

        private void TryOverrideFromJson()
        {
            var path = Path.Combine(Application.streamingAssetsPath, jsonFolder);
            if (!Directory.Exists(path)) return;

            var ghostPath = Path.Combine(path, "ghosts.json");
            if (File.Exists(ghostPath))
            {
                var json = File.ReadAllText(ghostPath);
                var overrides = JsonUtility.FromJson<GhostConfigArray>(json);
                foreach (var g in overrides.items) Database.Ghosts[g.id] = g;
            }

            var guestPath = Path.Combine(path, "guest_types.json");
            if (File.Exists(guestPath))
            {
                var json = File.ReadAllText(guestPath);
                var overrides = JsonUtility.FromJson<GuestTypeConfigArray>(json);
                foreach (var gt in overrides.items) Database.GuestTypes[gt.id] = gt;
            }

            var roomPath = Path.Combine(path, "room_prices.json");
            if (File.Exists(roomPath))
            {
                var json = File.ReadAllText(roomPath);
                var overrides = JsonUtility.FromJson<RoomPriceConfigArray>(json);
                foreach (var rp in overrides.items) Database.RoomPrices[rp.id] = rp;
            }

            var progPath = Path.Combine(path, "progression.json");
            if (File.Exists(progPath))
            {
                var json = File.ReadAllText(progPath);
                Database.Progression = JsonUtility.FromJson<ProgressionConfig>(json);
            }

            var rulePath = Path.Combine(path, "rules.json");
            if (File.Exists(rulePath))
            {
                var json = File.ReadAllText(rulePath);
                Database.Rules = JsonUtility.FromJson<GameRuleConfig>(json);
            }
        }
    }

    [Serializable] public class GhostConfigArray { public GhostConfig[] items; }
    [Serializable] public class GuestTypeConfigArray { public GuestTypeConfig[] items; }
    [Serializable] public class RoomPriceConfigArray { public RoomPriceConfig[] items; }
}
