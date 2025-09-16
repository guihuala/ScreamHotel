using UnityEngine;
using ScreamHotel.Core;
using ScreamHotel.Data;

namespace ScreamHotel.Dev
{
    /// <summary>
    /// 场景里空空也能跑：自动创建 DataManager + Game + DebugHUD。
    /// 把它挂在任意空物体上（或直接把脚本扔到场景里即可）。
    /// </summary>
    public class QuickStartBootstrap : MonoBehaviour
    {
        [Header("Optional")] public bool createIfMissing = true;

        private void Awake()
        {
            if (!createIfMissing) return;

            var dm = FindObjectOfType<DataManager>();
            if (dm == null)
            {
                var go = new GameObject("DataManager");
                dm = go.AddComponent<DataManager>();
            }

            var game = FindObjectOfType<Game>();
            if (game == null)
            {
                var go = new GameObject("Game");
                game = go.AddComponent<Game>();
                game.dataManager = dm; // 让 Game 使用同一个 DataManager
            }

            if (FindObjectOfType<DebugHUD>() == null)
            {
                var go = new GameObject("DebugHUD");
                go.AddComponent<DebugHUD>().game = game;
            }

            if (FindObjectOfType<SampleDataSeeder>() == null)
            {
                var go = new GameObject("SampleDataSeeder");
                var s = go.AddComponent<SampleDataSeeder>();
                s.game = game;
                s.autoSeedOnStart = true;
            }
        }
    }
}