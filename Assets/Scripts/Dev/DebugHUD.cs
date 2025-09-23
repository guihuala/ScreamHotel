using UnityEngine;
using System.Linq;
using System.Text;
using ScreamHotel.Domain;
using ScreamHotel.Systems;
using ScreamHotel.Core;

namespace ScreamHotel.Dev
{
    public class DebugHUD : MonoBehaviour
    {
        public ScreamHotel.Core.Game game;

        private AssignmentSystem _assign => GetPrivateField<AssignmentSystem>(game, "_assignmentSystem");
        private NightExecutionSystem _exec => GetPrivateField<NightExecutionSystem>(game, "_executionSystem");
        private BuildSystem _build => GetPrivateField<BuildSystem>(game, "_buildSystem");

        private Rect _rect = new Rect(10, 10, 460, 420);
        private Vector2 _scroll;

        private void OnEnable()
        {
            EventBus.Subscribe<NightResolved>(OnNightResolved);
            EventBus.Subscribe<GoldChanged>(_ => { });
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<NightResolved>(OnNightResolved);
            EventBus.Unsubscribe<GoldChanged>(_ => { });
        }

        private void OnGUI()
        {
            if (game == null) game = FindObjectOfType<ScreamHotel.Core.Game>();
            if (game == null || game.World == null) return;

            _rect = GUILayout.Window(GetInstanceID(), _rect, DrawWindow, "Scream Hotel — Debug HUD");
        }

        private void DrawWindow(int num)
        {
            var w = game.World;

            GUILayout.BeginVertical();

            // 行 1：快速种子 & 金币
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Seed Sample Data", GUILayout.Height(28)))
                FindObjectOfType<SampleDataSeeder>()?.SeedIfNeeded();
            if (GUILayout.Button("+100 Gold", GUILayout.Height(28)))
            {
                w.Economy.Gold += 100;
                EventBus.Raise(new GoldChanged(w.Economy.Gold));
            }
            GUILayout.EndHorizontal();

            // 行 2：买房 / 升级
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Buy Room (Lv1)", GUILayout.Height(28)))
            {
                if (_build.TryBuyRoom(out var id)) Log($"Bought new room: {id}");
                else Log("Not enough gold to buy room.");
            }
            if (GUILayout.Button("Upgrade First Upgradable", GUILayout.Height(28)))
            {
                var rid = w.Rooms.FirstOrDefault(r => r.Level < 3)?.Id;
                if (rid == null) Log("No upgradeable room.");
                else
                {
                    // Lv2 时设置一个示例标签：Darkness
                    if (_build.TryUpgradeRoom(rid, FearTag.Darkness))
                        Log($"Upgraded {rid} to next level.");
                    else Log("Upgrade failed (gold or max level).");
                }
            }
            GUILayout.EndHorizontal();
            
            // 行 2.5：建造新楼层
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Build Next Floor", GUILayout.Height(28)))
            {
                if (_build.TryBuildNextFloor(out var floor))
                {
                    Log($"Built Floor {floor} (Rooms: {string.Join(",", _build.EnumerateRoomIdsOnFloor(floor))})");
                }
                else
                {
                    Log("Not enough gold to build next floor.");
                }
            }
            GUILayout.EndHorizontal();

            // 行 3：简单指派（G1/G2 -> Room_01）
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Assign G1 -> Room_01", GUILayout.Height(24)))
            {
                if (!_assign.TryAssignGhostToRoom("G1", "Room_01")) Log("Assign failed (state/capacity).");
            }
            if (GUILayout.Button("Assign G2 -> Room_01", GUILayout.Height(24)))
            {
                if (!_assign.TryAssignGhostToRoom("G2", "Room_01")) Log("Assign failed (state/capacity).");
            }
            GUILayout.EndHorizontal();

            // 行 4：跑一晚
            if (GUILayout.Button("Run One Night (Execute)", GUILayout.Height(30)))
            {
                // 模拟：跳过 NightShow，直接执行
                var results = _exec.ResolveNight(Random.Range(1, int.MaxValue));
                EventBus.Raise(new NightResolved(results)); // 通知 UI/演出（这里主要给日志）
                _build.ApplySettlement(results);
                game.GoToDay();
            }

            GUILayout.Space(6);
            GUILayout.Label($"Day: {game.DayIndex}   Gold: {w.Economy.Gold}   Rooms: {w.Rooms.Count}   Ghosts: {w.Ghosts.Count}");

            // 列表信息（可滚动）
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(220));
            GUILayout.Label(BuildSnapshotText());
            GUILayout.EndScrollView();

            if (GUILayout.Button("Close", GUILayout.Height(22)))
                enabled = false;

            GUILayout.EndVertical();

            GUI.DragWindow();
        }

        private string BuildSnapshotText()
        {
            var w = game.World;
            var sb = new StringBuilder();

            sb.AppendLine("== Rooms ==");
            foreach (var r in w.Rooms.OrderBy(x => x.Id))
            {
                var tag = r.RoomTag.HasValue ? r.RoomTag.Value.ToString() : "-";
                sb.AppendLine($"{r.Id}  Lv{r.Level}  Cap:{r.Capacity}  Tag:{tag}  Assigned:{string.Join(",", r.AssignedGhostIds)}");
            }

            sb.AppendLine();
            sb.AppendLine("== Ghosts ==");
            foreach (var g in w.Ghosts.OrderBy(x => x.Id))
                sb.AppendLine($"{g.Id}  Main:{g.Main}  Sub:{(g.Sub.HasValue ? g.Sub.Value.ToString() : "-")}  Fatigue:{g.Fatigue:0.00}  State:{g.State} RestDays:{g.DaysForcedRest}");

            return sb.ToString();
        }

        private void OnNightResolved(NightResolved e)
        {
            var res = (ScreamHotel.Systems.NightResults)e.Results;
            Log($"=== Night Results (TotalGold={res.TotalGold}) ===");
            foreach (var rr in res.RoomDetails)
            {
                Log($"Room {rr.RoomId} vs {rr.GuestTypeId} | Total={rr.TotalScare:0.0} / Req={rr.Required:0.0} | Gold={rr.Gold} | Counter={rr.Counter}");
            }
        }

        private void Log(string s) => Debug.Log("[DebugHUD] " + s);

        private T GetPrivateField<T>(object obj, string field)
        {
            var f = obj.GetType().GetField(field,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (T)f.GetValue(obj);
        }
    }
}
