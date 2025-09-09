#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using ScreamHotel.Data;

public static class ConfigMenus
{
    [MenuItem("ScreamHotel/Create Sample Configs")]
    public static void CreateSamples()
    {
        var root = "Assets/Resources/Configs";
        System.IO.Directory.CreateDirectory(root);

        var ghost = ScriptableObject.CreateInstance<GhostConfig>();
        ghost.id = "Ghost_Spirit"; ghost.main = ScreamHotel.Domain.FearTag.Darkness; ghost.baseScare = 35; ghost.recruitCost = 120;
        AssetDatabase.CreateAsset(ghost, $"{root}/Ghost__Spirit.asset");

        var guest = ScriptableObject.CreateInstance<GuestTypeConfig>();
        guest.id = "Coward"; guest.fears = new System.Collections.Generic.List<ScreamHotel.Domain.FearTag>{ ScreamHotel.Domain.FearTag.Darkness, ScreamHotel.Domain.FearTag.Noise};
        guest.barMax = 90; guest.requiredPercent = 0.75f; guest.baseFee = 80; guest.counterChance = 0f;
        AssetDatabase.CreateAsset(guest, $"{root}/GuestType__Coward.asset");

        var price = ScriptableObject.CreateInstance<RoomPriceConfig>();
        price.id = "Floor1"; price.buyCost = 200; price.upgradeToLv2 = 150; price.upgradeToLv3 = 300; price.capacityLv1 = 1; price.capacityLv3 = 2; price.lv2HasTag = true; price.lv3HasTag = true;
        AssetDatabase.CreateAsset(price, $"{root}/RoomPrice__Floor1.asset");

        var prog = ScriptableObject.CreateInstance<ProgressionConfig>();
        AssetDatabase.CreateAsset(prog, $"{root}/Progression.asset");

        var rules = ScriptableObject.CreateInstance<GameRuleConfig>();
        AssetDatabase.CreateAsset(rules, $"{root}/GameRules.asset");

        AssetDatabase.SaveAssets();
        Debug.Log("Sample configs created under Resources/Configs");
    }
}
#endif
