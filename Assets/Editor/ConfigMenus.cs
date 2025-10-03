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
        ghost.id = "Ghost_Spirit"; ghost.main = ScreamHotel.Domain.FearTag.Darkness;
        AssetDatabase.CreateAsset(ghost, $"{root}/Ghost__Spirit.asset");

        var guest = ScriptableObject.CreateInstance<GuestTypeConfig>();
        guest.id = "Coward"; guest.immunities = new System.Collections.Generic.List<ScreamHotel.Domain.FearTag>{ ScreamHotel.Domain.FearTag.Darkness, ScreamHotel.Domain.FearTag.Noise};
        guest.barMax = 90; guest.requiredPercent = 0.75f; guest.baseFee = 80;
        AssetDatabase.CreateAsset(guest, $"{root}/GuestType__Coward.asset");
        
        var prog = ScriptableObject.CreateInstance<ProgressionConfig>();
        AssetDatabase.CreateAsset(prog, $"{root}/Progression.asset");

        var rules = ScriptableObject.CreateInstance<GameRuleConfig>();
        AssetDatabase.CreateAsset(rules, $"{root}/GameRules.asset");

        AssetDatabase.SaveAssets();
        Debug.Log("Sample configs created under Resources/Configs");
    }
}
#endif
