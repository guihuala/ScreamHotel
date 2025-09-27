using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ScreamHotel.Domain;

public class BattleSandbox : MonoBehaviour
{
    [Serializable] public class BattleGhost { public FearTag Main; public bool hasSub; public FearTag Sub; }
    
    [Header("Room Config")]
    public bool hasRoomTag;
    public FearTag roomTag;

    [Header("Guest Preset")]
    public GuestClass guestClass = GuestClass.Normal; // 普通/胆大/恐怖爱好者
    public int baseFee = 40;                          // 会随 guestClass 自动更新
    public int immuneCount = 0;                       // 会随 guestClass 自动更新
    public List<FearTag> immunities = new();          // 实际免疫集合（可随机）

    [Header("Ghosts")]
    public List<BattleGhost> ghosts = new();

    [Header("Debug")]
    public int lastHits;
    public int lastGold;
    public string lastDetail = "";

    // --------- 可视化配置 ---------
    [Header("Visualize (Runtime-only)")]
    public bool visualize = true;
    public Transform vizRoot;             // 舞台根（为空时自动创建）
    public float stageY = 0f;             // 舞台高度
    public float ghostSpacing = 1.2f;     // 鬼之间的间距
    public float ghostScale   = 0.8f;
    public float roomScale    = 1.2f;
    public float guestScale   = 1.0f;
    public Vector3 roomPos    = new Vector3(0, 0, 0);
    public Vector3 guestPos   = new Vector3(0, 0, 4f);
    public Vector3 ghostsStart= new Vector3(0, 0, -3f);

    // 命中可视：画线/Gizmos
    public Color hitLineColor = new Color(0.1f, 1f, 0.2f, 0.9f);
    public Color missLineColor= new Color(1f, 0.25f, 0.25f, 0.6f);

    // 运行期缓存
    private readonly FearTag[] _tags = (FearTag[])Enum.GetValues(typeof(FearTag));
    private Vector2 _scroll;
    private readonly List<Transform> _spawned = new();   // 可视节点
    private readonly List<(Vector3 from, Vector3 to, bool hit)> _lines = new();

    private void Reset()
    {
        SetGuestClass(GuestClass.Normal);
        ghosts.Clear();
        ghosts.Add(new BattleGhost { Main = FearTag.Darkness });
        ghosts.Add(new BattleGhost { Main = FearTag.Blood });
    }

    private void Awake()
    {
        EnsureVizRoot();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying) return;
        // 参数变更时，轻量刷新舞台
        if (visualize) RebuildVisual(lastComputedEffective, lastComputedVulnerable);
    }
#endif

    // —— 调试 GUI —— //
    private void OnGUI()
    {
        var box = new Rect(10, 10, 420, Screen.height - 20);
        GUILayout.BeginArea(box, GUI.skin.window);
        _scroll = GUILayout.BeginScrollView(_scroll);

        GUILayout.Label("<b>Battle Sandbox</b>");
        GUILayout.Space(4);

        // Room
        GUILayout.Label("<b>Room</b>");
        GUILayout.BeginHorizontal();
        hasRoomTag = GUILayout.Toggle(hasRoomTag, " Room Tag");
        GUI.enabled = hasRoomTag;
        roomTag = (FearTag)GUILayout.SelectionGrid((int)roomTag,
            _tags.Select(t => t.ToString()).ToArray(), 5);
        GUI.enabled = true;
        GUILayout.EndHorizontal();
        GUILayout.Space(6);

        // Ghosts
        GUILayout.Label("<b>Ghosts</b>");
        for (int i = 0; i < ghosts.Count; i++)
        {
            var g = ghosts[i];
            GUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label($"#{i+1}", GUILayout.Width(30));

            GUILayout.Label("Main:", GUILayout.Width(40));
            g.Main = (FearTag)GUILayout.SelectionGrid((int)g.Main, _tags.Select(t=>t.ToString()).ToArray(), 5);

            g.hasSub = GUILayout.Toggle(g.hasSub, " Sub");
            GUI.enabled = g.hasSub;
            g.Sub = (FearTag)GUILayout.SelectionGrid((int)g.Sub, _tags.Select(t=>t.ToString()).ToArray(), 5);
            GUI.enabled = true;

            if (GUILayout.Button("X", GUILayout.Width(24))) { ghosts.RemoveAt(i); i--; }
            GUILayout.EndHorizontal();
        }
        if (GUILayout.Button("+ Add Ghost")) ghosts.Add(new BattleGhost());

        GUILayout.Space(6);

        // Guest
        GUILayout.Label("<b>Guest</b>");
        GUILayout.BeginHorizontal();
        var newClass = (GuestClass)GUILayout.Toolbar((int)guestClass, new[] { "Normal", "Brave", "Enthusiast" });
        if (newClass != guestClass) SetGuestClass(newClass);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Base Fee: {baseFee}", GUILayout.Width(120));
        GUILayout.Label($"Immunity: {immuneCount}", GUILayout.Width(120));
        if (GUILayout.Button("Randomize Immunities")) RandomizeImmunities();
        GUILayout.EndHorizontal();

        GUILayout.Label("Immunity Set:");
        GUILayout.BeginHorizontal();
        foreach (var t in _tags)
        {
            bool has = immunities.Contains(t);
            bool newHas = GUILayout.Toggle(has, t.ToString(), GUILayout.Width(80));
            if (newHas != has)
            {
                if (newHas) { if (immunities.Count < immuneCount) immunities.Add(t); }
                else immunities.Remove(t);
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        if (GUILayout.Button("Run Combat", GUILayout.Height(32)))
        {
            RunCombat();
        }

        GUILayout.Space(6);
        GUILayout.Label($"Result: Hits = {lastHits}, Gold = {lastGold}");
        GUILayout.TextArea(lastDetail, GUILayout.MinHeight(80));

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void SetGuestClass(GuestClass cls)
    {
        guestClass = cls;
        switch (cls)
        {
            case GuestClass.Normal:     baseFee = 40; immuneCount = 0; break;
            case GuestClass.Brave:      baseFee = 60; immuneCount = 1; break;
            case GuestClass.Enthusiast: baseFee = 150; immuneCount = 2; break;
        }
        // 修剪/补全免疫集合长度
        while (immunities.Count > immuneCount) immunities.RemoveAt(immunities.Count - 1);
        while (immunities.Count < immuneCount && immunities.Count < _tags.Length)
        {
            foreach (var t in _tags) if (!immunities.Contains(t)) { immunities.Add(t); break; }
        }
    }

    private void RandomizeImmunities()
    {
        immunities.Clear();
        var pool = _tags.ToList();
        var rng = new System.Random();
        for (int i = 0; i < immuneCount && pool.Count > 0; i++)
        {
            int k = rng.Next(pool.Count);
            immunities.Add(pool[k]);
            pool.RemoveAt(k);
        }
    }

    // —— 战斗计算 —— //
    private List<FearTag> lastComputedEffective = new();
    private HashSet<FearTag> lastComputedVulnerable = new();

    private void RunCombat()
    {
        // 1) 收集鬼与房间的恐惧属性（去重）
        var set = new HashSet<FearTag>();
        foreach (var g in ghosts)
        {
            set.Add(g.Main);
            if (g.hasSub) set.Add(g.Sub);
        }
        if (hasRoomTag) set.Add(roomTag);

        // 2) 扣掉客人免疫
        var effective = set.Where(t => !immunities.Contains(t)).ToList();

        // 3) 命中与收益
        lastHits = effective.Count;
        lastGold = (lastHits >= 1) ? baseFee * lastHits : 0;

        // 4) 详情文本
        var sAll = string.Join(", ", set.Select(x => x.ToString()));
        var sImm = string.Join(", ", immunities.Select(x => x.ToString()));
        var sEff = string.Join(", ", effective.Select(x => x.ToString()));
        lastDetail =
            $"All Tags (Ghosts ∪ Room): {sAll}\n" +
            $"Immunities: {sImm}\n" +
            $"Effective: {sEff}\n" +
            $"Hits >= 1 => Success; Gold = BaseFee * Hits";

        // 5) 可视化
        lastComputedEffective = effective;
        lastComputedVulnerable = new HashSet<FearTag>(_tags.Except(immunities)); // 易感=非免疫
        if (visualize) RebuildVisual(effective, lastComputedVulnerable);
    }

    // ===== 可视化实现 =====
    private void EnsureVizRoot()
    {
        if (!vizRoot)
        {
            var go = new GameObject("Sandbox_Stage");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0, stageY, 0);
            vizRoot = go.transform;
        }
    }

    private void ClearViz()
    {
        foreach (var t in _spawned) if (t) DestroyImmediateSafe(t.gameObject);
        _spawned.Clear();
        _lines.Clear();
    }

    private void DestroyImmediateSafe(GameObject go)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) DestroyImmediate(go);
        else Destroy(go);
#else
        Destroy(go);
#endif
    }

    private void RebuildVisual(List<FearTag> effective, HashSet<FearTag> vulnerable)
    {
        EnsureVizRoot();
        ClearViz();

        // 房间（Cube）与客人（Sphere）
        var room = GameObject.CreatePrimitive(PrimitiveType.Cube);
        room.name = "Room";
        room.transform.SetParent(vizRoot, false);
        room.transform.localPosition = roomPos;
        room.transform.localScale = Vector3.one * roomScale;
        _spawned.Add(room.transform);

        if (hasRoomTag) Tint(room, ColorFor(roomTag));

        var guest = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        guest.name = "Guest";
        guest.transform.SetParent(vizRoot, false);
        guest.transform.localPosition = guestPos;
        guest.transform.localScale = Vector3.one * guestScale;
        Tint(guest, new Color(0.9f, 0.9f, 0.9f, 0.95f));
        _spawned.Add(guest.transform);

        // 鬼（Capsule 列队）
        var start = ghostsStart;
        for (int i = 0; i < ghosts.Count; i++)
        {
            var g = ghosts[i];
            var cap = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            cap.name = $"Ghost_{i+1}";
            cap.transform.SetParent(vizRoot, false);
            cap.transform.localPosition = start + new Vector3((i - (ghosts.Count-1)/2f) * ghostSpacing, 0, 0);
            cap.transform.localScale = Vector3.one * ghostScale;
            Tint(cap, Mix(ColorFor(g.Main), g.hasSub ? ColorFor(g.Sub) : (Color?)null));
            _spawned.Add(cap.transform);

            // 画线：鬼 → 客人（命中：绿色；未命中：红色半透明）
            var tags = new HashSet<FearTag> { g.Main };
            if (g.hasSub) tags.Add(g.Sub);
            foreach (var t in tags)
            {
                bool hit = effective.Contains(t);
                _lines.Add((cap.transform.position + Vector3.up * 0.8f, guest.transform.position + Vector3.up * 0.3f, hit));
            }
        }

        // 免疫标记：在客人顶上方摆小球（红色）
        var immStart = guest.transform.position + new Vector3(-1.2f, 1.2f, 0);
        int idx = 0;
        foreach (var t in immunities)
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            s.name = $"IMM_{t}";
            s.transform.SetParent(vizRoot, false);
            s.transform.position = immStart + new Vector3(idx * 0.4f, 0, 0);
            s.transform.localScale = Vector3.one * 0.2f;
            Tint(s, new Color(1f, 0.3f, 0.3f, 0.95f));
            _spawned.Add(s.transform);
            idx++;
        }

        // 有效命中标记：在房间顶上摆方块（绿色）
        var effStart = room.transform.position + new Vector3(-1.2f, 1.2f, 0);
        idx = 0;
        foreach (var t in effective.Distinct())
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Cube);
            q.name = $"HIT_{t}";
            q.transform.SetParent(vizRoot, false);
            q.transform.position = effStart + new Vector3(idx * 0.4f, 0, 0);
            q.transform.localScale = Vector3.one * 0.2f;
            Tint(q, ColorFor(t));
            _spawned.Add(q.transform);
            idx++;
        }
    }

    private void Tint(GameObject go, Color c)
    {
        var r = go.GetComponent<Renderer>();
        if (!r) return;
        if (r.material.HasProperty("_Color")) r.material.color = c;
    }
    private void Tint(GameObject go, Color? mix)
    {
        if (mix.HasValue) Tint(go, mix.Value);
    }

    private Color Mix(Color a, Color? bNullable)
    {
        if (!bNullable.HasValue) return a;
        var b = bNullable.Value;
        return new Color((a.r+b.r)/2f, (a.g+b.g)/2f, (a.b+b.b)/2f, 1f);
    }

    private Color ColorFor(FearTag t)
    {
        // 你可以按项目实际映射；这里提供一个稳定的色表
        int i = (int)t;
        UnityEngine.Random.State bak = UnityEngine.Random.state;
        UnityEngine.Random.InitState(i * 9973);
        var col = UnityEngine.Random.ColorHSV(0,1, 0.55f,0.95f, 0.75f,1f);
        UnityEngine.Random.state = bak;
        return col;
    }

    private void OnDrawGizmos()
    {
        if (!visualize || _lines == null) return;
        foreach (var (from, to, hit) in _lines)
        {
            Gizmos.color = hit ? hitLineColor : missLineColor;
            Gizmos.DrawLine(from, to);
        }
    }

    public enum GuestClass { Normal, Brave, Enthusiast }
}
