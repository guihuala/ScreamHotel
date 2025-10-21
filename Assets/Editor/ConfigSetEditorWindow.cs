#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using ScreamHotel.Data;
using ScreamHotel.Domain;

public class ConfigSetEditorWindow : EditorWindow
{
    // 顶部：入口与状态
    [MenuItem("Window/ScreamHotel/Config Editor")]
    public static void Open() => GetWindow<ConfigSetEditorWindow>("ScreamHotel Config");

    SerializedObject _soConfigSet;
    SerializedProperty _spGhosts;
    SerializedProperty _spGuestTypes;
    SerializedProperty _spRules;
    SerializedProperty _spProgression;

    // 额外：初始开局配置（不在 ConfigSet 中，但常用）
    SerializedObject _soInitialSetup;
    InitialSetupConfig _initialSetup;

    // Reorderable lists
    ReorderableList _ghostList;
    ReorderableList _guestTypeList;

    // 状态
    ConfigSet _configSet;
    Vector2 _scroll;
    string _ghostSearch = "";
    string _guestTypeSearch = "";

    // 快捷样式
    GUIStyle _h1, _h2, _wrap, _badge;

    void OnEnable()
    {
        _h1 = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
        _h2 = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
        _wrap = new GUIStyle(EditorStyles.label) { wordWrap = true };
        _badge = new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = Color.white },
            alignment = TextAnchor.MiddleCenter
        };

        // 尝试自动定位一个 ConfigSet
        if (_configSet == null)
        {
            var guids = AssetDatabase.FindAssets("t:ConfigSet");
            if (guids != null && guids.Length > 0)
            {
                _configSet = AssetDatabase.LoadAssetAtPath<ConfigSet>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
        }
        BindConfigSet(_configSet);

        // 尝试找 InitialSetup
        if (_initialSetup == null)
        {
            var guids = AssetDatabase.FindAssets("t:InitialSetupConfig");
            if (guids != null && guids.Length > 0)
            {
                _initialSetup = AssetDatabase.LoadAssetAtPath<InitialSetupConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
                if (_initialSetup) _soInitialSetup = new SerializedObject(_initialSetup);
            }
        }
    }

    void BindConfigSet(ConfigSet set)
    {
        _configSet = set;
        if (_configSet != null)
        {
            _soConfigSet = new SerializedObject(_configSet);
            _spGhosts = _soConfigSet.FindProperty("ghosts");
            _spGuestTypes = _soConfigSet.FindProperty("guestTypes");
            _spRules = _soConfigSet.FindProperty("rules");
            _spProgression = _soConfigSet.FindProperty("progression");
            BuildGhostList();
            BuildGuestTypeList();
        }
        else
        {
            _soConfigSet = null;
            _spGhosts = _spGuestTypes = _spRules = _spProgression = null;
            _ghostList = _guestTypeList = null;
        }
    }

    // ============ GUI ============

    void OnGUI()
    {
        EditorGUILayout.Space(4);
        TitleBar();
        EditorGUILayout.Space(6);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        DrawConfigSetSection();
        EditorGUILayout.Space(8);

        DrawRulesSection();
        EditorGUILayout.Space(8);

        DrawProgressionSection();
        EditorGUILayout.Space(8);

        DrawInitialSetupSection();
        EditorGUILayout.Space(8);

        DrawGhostTableSection();
        EditorGUILayout.Space(8);

        DrawGuestTypeTableSection();

        EditorGUILayout.EndScrollView();

        // 底部保存条
        if ((_soConfigSet != null && _soConfigSet.hasModifiedProperties) || (_soInitialSetup != null && _soInitialSetup.hasModifiedProperties))
        {
            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("保存修改", GUILayout.Height(28), GUILayout.Width(120)))
                {
                    ApplyAndSave();
                }
            }
        }
    }

    void TitleBar()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("ScreamHotel 全局配置面板", _h1);

            GUILayout.FlexibleSpace();
            if (_configSet && GUILayout.Button("定位 ConfigSet 资源", GUILayout.Width(160)))
            {
                Selection.activeObject = _configSet;
                EditorGUIUtility.PingObject(_configSet);
            }
        }

        EditorGUILayout.LabelField("用于集中管理策划配置：游戏规则、难度曲线、开局配置、鬼表与客人类型表。这里做的修改将直接写回对应的 ScriptableObject 资源。", _wrap);
    }

    // ============ ConfigSet 入口 ============

    void DrawConfigSetSection()
    {
        GUILayout.Label("1) 入口：ConfigSet（表与单例）", _h2);
        EditorGUILayout.LabelField("ConfigSet 聚合了【鬼表】【客人类型表】以及【单例：规则 / 进度】等核心资源。", _wrap);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _configSet = (ConfigSet)EditorGUILayout.ObjectField("ConfigSet", _configSet, typeof(ConfigSet), false);
                if (GUILayout.Button("创建", GUILayout.Width(80)))
                {
                    var path = EditorUtility.SaveFilePanelInProject("Create ConfigSet", "ConfigSet", "asset", "选择保存位置");
                    if (!string.IsNullOrEmpty(path))
                    {
                        var asset = ScriptableObject.CreateInstance<ConfigSet>();
                        AssetDatabase.CreateAsset(asset, path);
                        AssetDatabase.SaveAssets();
                        BindConfigSet(asset);
                    }
                }
            }

            if (_soConfigSet == null)
            {
                EditorGUILayout.HelpBox("未绑定 ConfigSet。请拖入已有资源，或点击【创建】生成一个。", MessageType.Warning);
                return;
            }

            _soConfigSet.Update();

            EditorGUILayout.PropertyField(_spRules, new GUIContent("GameRuleConfig（规则单例）"));
            EditorGUILayout.PropertyField(_spProgression, new GUIContent("ProgressionConfig（进度曲线单例）"));
            EditorGUILayout.Space(6);
            EditorGUILayout.PropertyField(_spGhosts, new GUIContent("Ghost 表（可在下方专门编辑）"), includeChildren: false);
            EditorGUILayout.PropertyField(_spGuestTypes, new GUIContent("GuestType 表（可在下方专门编辑）"), includeChildren: false);

            _soConfigSet.ApplyModifiedProperties();
        }
    }

    // ============ 规则 ============

    void DrawRulesSection()
    {
        GUILayout.Label("2) 游戏规则（GameRuleConfig）", _h2);
        EditorGUILayout.LabelField("每日客人数、商店、建造价格、容量与日夜配比等。", _wrap);

        if (_spRules == null || _spRules.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox("尚未指定 GameRuleConfig。到上面的 ConfigSet 区域先指定/创建。", MessageType.Info);
            return;
        }

        using (new EditorGUILayout.VerticalScope("box"))
        {
            var rules = (GameRuleConfig)_spRules.objectReferenceValue;
            var so = new SerializedObject(rules);
            so.Update();

            DrawBadge("Suspicion & Ending");
            EditorGUILayout.PropertyField(so.FindProperty("suspicionThreshold"), new GUIContent("怀疑值上限"));
            EditorGUILayout.PropertyField(so.FindProperty("suspicionPerFailedGuest"), new GUIContent("失败+怀疑/人"));
            EditorGUILayout.PropertyField(so.FindProperty("totalDays"), new GUIContent("总天数"));

            EditorGUILayout.Space(6);
            DrawBadge("Day Spawns");
            EditorGUILayout.PropertyField(so.FindProperty("dayGuestSpawnCount"), new GUIContent("每日客人数"));

            EditorGUILayout.Space(6);
            DrawBadge("Ghost Shop");
            EditorGUILayout.PropertyField(so.FindProperty("ghostShopSlots"), new GUIContent("商店槽位"));
            EditorGUILayout.PropertyField(so.FindProperty("ghostShopPrice"), new GUIContent("鬼统一售价"));
            EditorGUILayout.PropertyField(so.FindProperty("ghostShopRerollCost"), new GUIContent("刷新消耗"));
            EditorGUILayout.PropertyField(so.FindProperty("ghostShopUniqueMains"), new GUIContent("同轮去重主恐惧"));

            EditorGUILayout.Space(6);
            DrawBadge("Build Prices");
            EditorGUILayout.PropertyField(so.FindProperty("floorBuildBaseCost"), new GUIContent("楼层基价"));
            EditorGUILayout.PropertyField(so.FindProperty("floorCostGrowth"), new GUIContent("楼层价格增长"));
            EditorGUILayout.PropertyField(so.FindProperty("roomUpgradeCosts"), new GUIContent("房间升级价格数组（[Lv1→Lv2, Lv2→Lv3]）"));
            EditorGUILayout.PropertyField(so.FindProperty("roomBuyCost"), new GUIContent("购买 Lv1 价格"));
            EditorGUILayout.PropertyField(so.FindProperty("roomUnlockCost"), new GUIContent("解锁 Lv0→Lv1 价格"));
            EditorGUILayout.PropertyField(so.FindProperty("capacityLv1"), new GUIContent("Lv1 容量"));
            EditorGUILayout.PropertyField(so.FindProperty("capacityLv3"), new GUIContent("Lv3 容量"));
            EditorGUILayout.PropertyField(so.FindProperty("lv2HasTag"), new GUIContent("Lv2 是否有恐惧Tag"));

            EditorGUILayout.Space(6);
            DrawBadge("Ghost Training");
            EditorGUILayout.PropertyField(so.FindProperty("ghostTrainingTimeDays"), new GUIContent("训练天数"));

            EditorGUILayout.Space(6);
            DrawBadge("Time Ratios (per day)");
            EditorGUILayout.PropertyField(so.FindProperty("dayRatio"), new GUIContent("白天比例"));
            EditorGUILayout.PropertyField(so.FindProperty("nightShowRatio"), new GUIContent("夜-展示比例"));
            EditorGUILayout.PropertyField(so.FindProperty("nightExecuteRatio"), new GUIContent("夜-执行比例"));
            EditorGUILayout.PropertyField(so.FindProperty("settlementRatio"), new GUIContent("结算比例"));

            // 校验与提醒
            EditorGUILayout.Space(6);
            ValidateRules(rules);

            so.ApplyModifiedProperties();
        }
    }

    void ValidateRules(GameRuleConfig rules)
    {
        if (rules.totalDays <= 0)
            EditorGUILayout.HelpBox("【总天数】应 > 0。", MessageType.Error);

        if (rules.dayGuestSpawnCount <= 0)
            EditorGUILayout.HelpBox("【每日客人数】应 > 0。否则当天面板会空。", MessageType.Warning);

        if (rules.roomUpgradeCosts == null || rules.roomUpgradeCosts.Length < 2)
            EditorGUILayout.HelpBox("【房间升级价格数组】需要至少 2 个元素：Index0=Lv1→Lv2，Index1=Lv2→Lv3。", MessageType.Warning);

        float sum = rules.dayRatio + rules.nightShowRatio + rules.nightExecuteRatio + rules.settlementRatio;
        if (Mathf.Abs(sum - 1f) > 0.001f)
            EditorGUILayout.HelpBox($"【时间配比总和】建议 ≈ 1（当前 = {sum:0.##}）。", MessageType.Info);
    }

    // ============ 难度/进度 ============

    void DrawProgressionSection()
    {
        GUILayout.Label("3) 难度/进度曲线（ProgressionConfig）", _h2);
        EditorGUILayout.LabelField("用 AnimationCurve 表达随时间推进的难度变化（例如影响每日客人数放大系数）。", _wrap);

        if (_spProgression == null || _spProgression.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox("尚未指定 ProgressionConfig。到上面的 ConfigSet 区域先指定/创建。", MessageType.Info);
            return;
        }

        using (new EditorGUILayout.VerticalScope("box"))
        {
            var prog = (ProgressionConfig)_spProgression.objectReferenceValue;
            var so = new SerializedObject(prog);
            so.Update();

            EditorGUILayout.PropertyField(so.FindProperty("guestMixCurve"), new GUIContent("难度曲线"));
            EditorGUILayout.HelpBox("横轴：游戏进程（0=第一天，1=最后一天）。纵轴：难度强度。你可以把这条曲线用于“每日客人数”缩放或其他系统。", MessageType.None);

            so.ApplyModifiedProperties();
        }
    }

    // ============ 开局配置 ============

    void DrawInitialSetupSection()
    {
        GUILayout.Label("4) 开局配置（InitialSetupConfig）", _h2);
        EditorGUILayout.LabelField("开局经济、初始房间数、起始鬼。此资源不在 ConfigSet 内，但强相关。", _wrap);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _initialSetup = (InitialSetupConfig)EditorGUILayout.ObjectField("InitialSetup", _initialSetup, typeof(InitialSetupConfig), false);
                if (GUILayout.Button("创建", GUILayout.Width(80)))
                {
                    var path = EditorUtility.SaveFilePanelInProject("Create InitialSetup", "InitialSetup", "asset", "选择保存位置");
                    if (!string.IsNullOrEmpty(path))
                    {
                        var asset = ScriptableObject.CreateInstance<InitialSetupConfig>();
                        AssetDatabase.CreateAsset(asset, path);
                        AssetDatabase.SaveAssets();
                        _initialSetup = asset;
                        _soInitialSetup = new SerializedObject(_initialSetup);
                    }
                }
            }

            if (_initialSetup == null)
            {
                EditorGUILayout.HelpBox("未绑定 InitialSetup。可在此创建或拖入已有资源。", MessageType.Info);
                return;
            }

            _soInitialSetup.Update();
            EditorGUILayout.PropertyField(_soInitialSetup.FindProperty("startGold"), new GUIContent("开局金币"));
            EditorGUILayout.PropertyField(_soInitialSetup.FindProperty("startRoomCount"), new GUIContent("开局房间数（Lv1）"));
            EditorGUILayout.PropertyField(_soInitialSetup.FindProperty("starterGhosts"), new GUIContent("起始鬼列表"), true);
            _soInitialSetup.ApplyModifiedProperties();
        }
    }

    // ============ 鬼表 ============

    void DrawGhostTableSection()
    {
        GUILayout.Label("5) 鬼表（GhostConfig 列表）", _h2);
        if (_soConfigSet == null) { EditorGUILayout.HelpBox("未绑定 ConfigSet。", MessageType.Warning); return; }

        using (new EditorGUILayout.VerticalScope("box"))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _ghostSearch = EditorGUILayout.TextField("筛选（按 id）", _ghostSearch);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("新建鬼", GUILayout.Width(80))) CreateGhostAsset();
            }

            if (_ghostList != null) _ghostList.DoLayoutList();
        }
    }

    void BuildGhostList()
    {
        _ghostList = new ReorderableList(_soConfigSet, _spGhosts, true, true, true, true);
        _ghostList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Ghost 列表（双击定位资源；右键可删除）");
        _ghostList.onAddCallback = list => CreateGhostAsset();
        _ghostList.onRemoveCallback = list =>
        {
            if (list.index >= 0 && list.index < _spGhosts.arraySize)
            {
                var elem = _spGhosts.GetArrayElementAtIndex(list.index).objectReferenceValue;
                _spGhosts.DeleteArrayElementAtIndex(list.index);
                if (elem) Selection.activeObject = elem;
                _soConfigSet.ApplyModifiedProperties();
            }
        };
        _ghostList.drawElementCallback = (rect, index, active, focused) =>
        {
            var sp = _spGhosts.GetArrayElementAtIndex(index);
            var gc = (GhostConfig)sp.objectReferenceValue;
            if (!PassGhostFilter(gc)) return;

            rect.height = EditorGUIUtility.singleLineHeight;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUI.ObjectField(new Rect(rect.x, rect.y, rect.width - 100, rect.height), sp, GUIContent.none);
            }

            if (gc != null)
            {
                if (GUI.Button(new Rect(rect.x + rect.width - 96, rect.y, 44, rect.height), "Ping"))
                {
                    Selection.activeObject = gc;
                    EditorGUIUtility.PingObject(gc);
                }
                if (GUI.Button(new Rect(rect.x + rect.width - 48, rect.y, 48, rect.height), "Edit"))
                {
                    Selection.activeObject = gc;
                }
            }
        };
        _ghostList.elementHeightCallback = index =>
        {
            var sp = _spGhosts.GetArrayElementAtIndex(index);
            var gc = (GhostConfig)sp.objectReferenceValue;
            return PassGhostFilter(gc) ? EditorGUIUtility.singleLineHeight + 4 : 0.1f;
        };
    }

    bool PassGhostFilter(GhostConfig gc)
    {
        if (gc == null) return true;
        if (string.IsNullOrEmpty(_ghostSearch)) return true;
        return gc.id != null && gc.id.IndexOf(_ghostSearch, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    void CreateGhostAsset()
    {
        var path = EditorUtility.SaveFilePanelInProject("Create Ghost", "Ghost__", "asset", "保存 GhostConfig");
        if (string.IsNullOrEmpty(path)) return;
        var asset = ScriptableObject.CreateInstance<GhostConfig>();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();

        _soConfigSet.Update();
        _spGhosts.arraySize++;
        _spGhosts.GetArrayElementAtIndex(_spGhosts.arraySize - 1).objectReferenceValue = asset;
        _soConfigSet.ApplyModifiedProperties();

        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
    }

    // ============ 客人类型表 ============

    void DrawGuestTypeTableSection()
    {
        GUILayout.Label("6) 客人类型表（GuestTypeConfig 列表）", _h2);
        if (_soConfigSet == null) { EditorGUILayout.HelpBox("未绑定 ConfigSet。", MessageType.Warning); return; }

        using (new EditorGUILayout.VerticalScope("box"))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _guestTypeSearch = EditorGUILayout.TextField("筛选（按 id / 名称）", _guestTypeSearch);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("新建客人类型", GUILayout.Width(110))) CreateGuestTypeAsset();
            }

            if (_guestTypeList != null) _guestTypeList.DoLayoutList();
        }
    }

    void BuildGuestTypeList()
    {
        _guestTypeList = new ReorderableList(_soConfigSet, _spGuestTypes, true, true, true, true);
        _guestTypeList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "GuestType 列表（双击定位资源；右键可删除）");
        _guestTypeList.onAddCallback = list => CreateGuestTypeAsset();
        _guestTypeList.onRemoveCallback = list =>
        {
            if (list.index >= 0 && list.index < _spGuestTypes.arraySize)
            {
                var elem = _spGuestTypes.GetArrayElementAtIndex(list.index).objectReferenceValue;
                _spGuestTypes.DeleteArrayElementAtIndex(list.index);
                if (elem) Selection.activeObject = elem;
                _soConfigSet.ApplyModifiedProperties();
            }
        };
        _guestTypeList.drawElementCallback = (rect, index, active, focused) =>
        {
            var sp = _spGuestTypes.GetArrayElementAtIndex(index);
            var cfg = (GuestTypeConfig)sp.objectReferenceValue;
            if (!PassGuestTypeFilter(cfg)) return;

            rect.height = EditorGUIUtility.singleLineHeight;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUI.ObjectField(new Rect(rect.x, rect.y, rect.width - 160, rect.height), sp, GUIContent.none);
            }

            string label = cfg != null ? cfg.displayName : "(null)";
            GUI.Label(new Rect(rect.x + rect.width - 156, rect.y, 100, rect.height), label, EditorStyles.miniBoldLabel);

            if (cfg != null)
            {
                if (GUI.Button(new Rect(rect.x + rect.width - 56, rect.y, 56, rect.height), "Edit"))
                {
                    Selection.activeObject = cfg;
                }
            }
        };
        _guestTypeList.elementHeightCallback = index =>
        {
            var sp = _spGuestTypes.GetArrayElementAtIndex(index);
            var cfg = (GuestTypeConfig)sp.objectReferenceValue;
            return PassGuestTypeFilter(cfg) ? EditorGUIUtility.singleLineHeight + 4 : 0.1f;
        };
    }

    bool PassGuestTypeFilter(GuestTypeConfig cfg)
    {
        if (cfg == null) return true;
        if (string.IsNullOrEmpty(_guestTypeSearch)) return true;
        bool idMatch = cfg.id != null && cfg.id.IndexOf(_guestTypeSearch, StringComparison.OrdinalIgnoreCase) >= 0;
        bool nameMatch = cfg.displayName != null && cfg.displayName.IndexOf(_guestTypeSearch, StringComparison.OrdinalIgnoreCase) >= 0;
        return idMatch || nameMatch;
    }

    void CreateGuestTypeAsset()
    {
        var path = EditorUtility.SaveFilePanelInProject("Create GuestType", "GuestType__", "asset", "保存 GuestTypeConfig");
        if (string.IsNullOrEmpty(path)) return;
        var asset = ScriptableObject.CreateInstance<GuestTypeConfig>();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();

        _soConfigSet.Update();
        _spGuestTypes.arraySize++;
        _spGuestTypes.GetArrayElementAtIndex(_spGuestTypes.arraySize - 1).objectReferenceValue = asset;
        _soConfigSet.ApplyModifiedProperties();

        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
    }

    void DrawBadge(string text)
    {
        var c = GUI.color;
        GUI.color = new Color(0.15f, 0.45f, 0.9f, 1);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(" " + text + " ", _badge, GUILayout.ExpandWidth(false));
        }
        GUI.color = c;
    }

    void ApplyAndSave()
    {
        _soConfigSet?.ApplyModifiedProperties();
        _soInitialSetup?.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Repaint();
    }
}
#endif
