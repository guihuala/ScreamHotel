// --------------------------------------------------------------
// UIPanelTemplateTools.cs
// 说明：基于现有 BasePanel / UIDatas / UIDataEditor 的工作流，
// 提供一键生成 UI 预制件模板、可选生成派生脚本、批量创建、
// 注册到 UIDatas、校验 UIDatas 的实用工具。
// 放置位置：Assets/Editor/UIPanelTemplateTools.cs （建议）
// Unity 版本：2020+（兼容更高版本）
// --------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class UIPanelTemplateTools : EditorWindow
{
    private const string kDefaultSaveFolder = "Assets/Resources/UI/Panels";

    [Header("创建设置")] [SerializeField] private string panelName = "NewPanel";
    [SerializeField] private string saveFolder = kDefaultSaveFolder;
    [SerializeField] private bool generateDerivedScript = true;
    [SerializeField] private bool addBasePanelIfScriptFail = true;
    [SerializeField] private bool registerToUIDatas = true;
    [SerializeField] private UIDatas uidatasAsset;

    [Header("批量创建（用逗号或换行分隔）")] [SerializeField]
    private string batchNames = string.Empty;

    [MenuItem("Tools/UI Manager/UIPanel Template Tools")]
    public static void ShowWindow()
    {
        GetWindow<UIPanelTemplateTools>("UIPanel Template Tools").minSize = new Vector2(520, 420);
    }

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(saveFolder)) saveFolder = kDefaultSaveFolder;
        if (uidatasAsset == null)
        {
            // 尝试自动定位一个 UIDatas 资源
            var guids = AssetDatabase.FindAssets("t:UIDatas");
            if (guids != null && guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                uidatasAsset = AssetDatabase.LoadAssetAtPath<UIDatas>(path);
            }
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("单个创建", EditorStyles.boldLabel);
        panelName = EditorGUILayout.TextField("Panel 名称", panelName);
        saveFolder = EditorGUILayout.TextField("保存文件夹", saveFolder);
        generateDerivedScript = EditorGUILayout.ToggleLeft("生成派生脚本（继承 BasePanel）", generateDerivedScript);
        addBasePanelIfScriptFail = EditorGUILayout.ToggleLeft("若派生脚本加载失败则添加 BasePanel", addBasePanelIfScriptFail);
        registerToUIDatas = EditorGUILayout.ToggleLeft("创建后注册到 UIDatas", registerToUIDatas);
        uidatasAsset = (UIDatas)EditorGUILayout.ObjectField("UIDatas 资源", uidatasAsset, typeof(UIDatas), false);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("创建单个预制件", GUILayout.Height(30)))
        {
            CreateSingle(panelName);
        }

        if (GUILayout.Button("打开保存文件夹", GUILayout.Height(30)))
        {
            EditorUtility.RevealInFinder(Path.GetFullPath(saveFolder));
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("批量创建", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("用逗号或换行分隔多个 Panel 名称（会自动去除空白）", MessageType.Info);
        batchNames = EditorGUILayout.TextArea(batchNames, GUILayout.MinHeight(70));
        if (GUILayout.Button("批量创建预制件", GUILayout.Height(28)))
        {
            var names = ParseNames(batchNames);
            foreach (var n in names)
                CreateSingle(n);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("其它工具", EditorStyles.boldLabel);
        if (GUILayout.Button("校验 UIDatas（Resources 是否存在，路径是否正确）", GUILayout.Height(28)))
        {
            ValidateUIDatas(uidatasAsset);
        }

        if (GUILayout.Button("从选中对象创建预制件（若是非 UI 物体，会自动添加 RectTransform/CanvasGroup）", GUILayout.Height(28)))
        {
            ConvertSelectionToPanelPrefab();
        }
    }

    // ----------------- Core -----------------

    private void CreateSingle(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            EditorUtility.DisplayDialog("错误", "Panel 名称不能为空", "OK");
            return;
        }

        EnsureFolder(saveFolder);

        // 可选：生成派生脚本
        MonoScript script = null;
        Type derivedType = null;
        if (generateDerivedScript)
        {
            string scriptPath = Path.Combine(saveFolder.Replace("Resources/", ""), name + ".cs");
            // 若保存到 Resources 下会被打进包中，脚本建议与 prefab 同级但不在 Resources：
            // 因此这里将脚本保存到与 saveFolder 同级（若 saveFolder 在 Resources 下，则回退到 Assets/UI/Scripts）
            string scriptFolder = saveFolder.Contains("Resources/") ? "Assets/UI/Scripts" : saveFolder;
            EnsureFolder(scriptFolder);
            scriptPath = Path.Combine(scriptFolder, name + ".cs").Replace("\\", "/");

            if (!File.Exists(scriptPath))
            {
                File.WriteAllText(scriptPath, GetDerivedPanelScriptText(name));
                AssetDatabase.ImportAsset(scriptPath);
                AssetDatabase.Refresh();
            }

            script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
            if (script != null)
            {
                derivedType = script.GetClass();
            }
        }

        // 创建 GameObject
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        StretchFull(rt);

        // 添加 CanvasGroup
        var cg = go.AddComponent<CanvasGroup>();
        cg.alpha = 1f;

        // 添加脚本组件
        if (derivedType != null)
        {
            go.AddComponent(derivedType);
        }
        else if (addBasePanelIfScriptFail)
        {
            // 退化为 BasePanel（脚本热重载后可手动替换）
            var basePanelType = FindTypeByName("BasePanel");
            if (basePanelType != null)
                go.AddComponent(basePanelType);
        }

        // 存成 Prefab
        string prefabPath = Path.Combine(saveFolder, name + ".prefab").Replace("\\", "/");
        var createdPrefab = SaveAsPrefab(go, prefabPath);

        // 可选：注册到 UIDatas
        if (registerToUIDatas && uidatasAsset != null)
        {
            RegisterPanelToUIDatas(uidatasAsset, name, prefabPath);
        }

        // 清理场景对象
        DestroyImmediate(go);

        EditorGUIUtility.PingObject(createdPrefab);
        Debug.Log($"[UIPanelTemplateTools] 预制件创建完成：{prefabPath}");
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string parent = "Assets";
        foreach (var part in folder.Replace("\\", "/").Split('/'))
        {
            if (string.IsNullOrEmpty(part) || part == "Assets") continue;
            string next = parent + "/" + part;
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(parent, part);
            parent = next;
        }
    }

    private static Object SaveAsPrefab(GameObject go, string prefabPath)
    {
        var dir = Path.GetDirectoryName(prefabPath);
        if (!string.IsNullOrEmpty(dir)) EnsureFolder(dir.Replace("\\", "/"));
#if UNITY_2018_3_OR_NEWER
        return PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
#else
        return PrefabUtility.CreatePrefab(prefabPath, go);
#endif
    }

    private static void RegisterPanelToUIDatas(UIDatas data, string panelName, string fullAssetPath)
    {
        if (data.uiDataList == null) data.uiDataList = new List<UIData>();

        string resRelative = GetResourcesRelativePath(fullAssetPath);
        if (string.IsNullOrEmpty(resRelative))
        {
            Debug.LogWarning($"[UIPanelTemplateTools] 预制件不在 Resources 下，无法生成 Resources 相对路径：{fullAssetPath}");
        }

        // 重复检查：若已存在同名，更新路径；否则新增
        var existing = data.uiDataList.Find(u => u.uiName == panelName);
        if (existing != null)
        {
            existing.uiPath = string.IsNullOrEmpty(resRelative) ? fullAssetPath : resRelative;
        }
        else
        {
            data.uiDataList.Add(new UIData
            {
                uiName = panelName,
                uiPath = string.IsNullOrEmpty(resRelative) ? fullAssetPath : resRelative
            });
        }

        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        Debug.Log($"[UIPanelTemplateTools] 已注册到 UIDatas：{panelName} => {resRelative}");
    }

    private static string GetResourcesRelativePath(string fullPath)
    {
        fullPath = fullPath.Replace("\\", "/");
        int idx = fullPath.IndexOf("Resources/", StringComparison.Ordinal);
        if (idx < 0) return null;
        string sub = fullPath.Substring(idx + "Resources/".Length);
        if (sub.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            sub = sub.Substring(0, sub.Length - ".prefab".Length);
        return sub;
    }

    private static Type FindTypeByName(string typeName)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(typeName);
            if (t != null) return t;
        }

        return null;
    }

    private static string GetDerivedPanelScriptText(string className)
    {
        return
            $@"using UnityEngine;\n\npublic class {className} : BasePanel\n{{\n    // 在这里扩展你的面板逻辑（按钮绑定、生命周期等）\n    protected override void Awake()\n    {{\n        base.Awake();\n        // 初始化\n    }}\n\n    public override void OpenPanel(string name)\n    {{\n        base.OpenPanel(name);\n        // 打开时逻辑\n    }}\n\n    public override void ClosePanel()\n    {{\n        // 关闭前逻辑\n        base.ClosePanel();\n    }}\n}}";
    }

    private static List<string> ParseNames(string raw)
    {
        var result = new List<string>();
    
        if (string.IsNullOrEmpty(raw)) 
            return result;
    
        var parts = raw.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
    
        foreach (var p in parts)
        {
            var n = p.Trim();
            if (!string.IsNullOrWhiteSpace(n)) 
                result.Add(n);
        }
    
        return result;
    }

    private void ValidateUIDatas(UIDatas data)
    {
        if (data == null)
        {
            EditorUtility.DisplayDialog("UIDatas 为空", "请先指定 UIDatas 资源", "OK");
            return;
        }

        if (data.uiDataList == null || data.uiDataList.Count == 0)
        {
            EditorUtility.DisplayDialog("没有数据", "UIDatas.uiDataList 为空", "OK");
            return;
        }

        int ok = 0, missing = 0;
        foreach (var item in data.uiDataList)
        {
            string path = item.uiPath;
            GameObject go = null;
            if (!string.IsNullOrEmpty(path))
            {
                // 优先尝试 Resources 相对路径
                go = Resources.Load<GameObject>(path);
                if (go == null && File.Exists(path))
                {
                    // 兼容旧数据：用 AssetDatabase 尝试加载
                    go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }
            }

            if (go != null) ok++;
            else missing++;
        }

        EditorUtility.DisplayDialog("UIDatas 校验结果", $"存在：{ok}\n缺失：{missing}", "OK");
    }

    private void ConvertSelectionToPanelPrefab()
    {
        var sel = Selection.activeGameObject;
        if (sel == null)
        {
            EditorUtility.DisplayDialog("没有选中对象", "请选择一个场景中的对象或层级中的预制体实例", "OK");
            return;
        }

        // 克隆一个临时对象用于保存
        var clone = Instantiate(sel);
        clone.name = sel.name;

        // 确保有 RectTransform / CanvasGroup
        if (clone.GetComponent<RectTransform>() == null)
        {
            clone.AddComponent<RectTransform>();
        }

        StretchFull(clone.GetComponent<RectTransform>());
        if (clone.GetComponent<CanvasGroup>() == null)
        {
            clone.AddComponent<CanvasGroup>();
        }

        // 若没有 BasePanel 或派生脚本，则添加 BasePanel
        var basePanelType = FindTypeByName("BasePanel");
        if (basePanelType != null && clone.GetComponent(basePanelType) == null)
        {
            clone.AddComponent(basePanelType);
        }

        EnsureFolder(saveFolder);
        string prefabPath = Path.Combine(saveFolder, clone.name + ".prefab").Replace("\\", "/");
        var createdPrefab = SaveAsPrefab(clone, prefabPath);
        DestroyImmediate(clone);

        if (registerToUIDatas && uidatasAsset != null)
        {
            RegisterPanelToUIDatas(uidatasAsset, sel.name, prefabPath);
        }

        EditorGUIUtility.PingObject(createdPrefab);
        Debug.Log($"[UIPanelTemplateTools] 已从选中对象创建预制件：{prefabPath}");
    }
    // ----------------- 右键 / 菜单 快捷项 -----------------

    public static class UIPanelTemplateMenuItems
    {
        [MenuItem("GameObject/UI/Create Empty Panel (BasePanel)", false, 10)]
        public static void CreateEmptyPanel()
        {
            var go = new GameObject("NewPanel", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.AddComponent<CanvasGroup>();

            var basePanelType = Type.GetType("BasePanel");
            if (basePanelType != null) go.AddComponent(basePanelType);

            // 放到场景 Canvas 下
            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas != null) go.transform.SetParent(canvas.transform, false);

            Selection.activeGameObject = go;
        }
    }
}