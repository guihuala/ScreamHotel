using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class ParticleDataEditor : EditorWindow
{
    private ParticleDatas targetParticleDatas;
    private string searchPath = "Assets/";
    private Vector2 scroll;
    private bool includeSubfolders = true;
    private string[] prefabExtensions = new[] { ".prefab" };
    private string searchFilter = "";

    [MenuItem("Tools/Particles/ParticleData Configurator")]
    public static void ShowWindow()
    {
        GetWindow<ParticleDataEditor>("ParticleData Configurator");
    }

    void OnGUI()
    {
        GUILayout.Space(6);
        EditorGUILayout.LabelField("ParticleData Configuration Tool", EditorStyles.boldLabel);

        targetParticleDatas = (ParticleDatas)EditorGUILayout.ObjectField("Target ParticleDatas", targetParticleDatas, typeof(ParticleDatas), false);

        GUILayout.Space(8);
        EditorGUILayout.LabelField("Search Settings", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        searchPath = EditorGUILayout.TextField("Search Path", searchPath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string newPath = EditorUtility.OpenFolderPanel("Select Folder to Search", searchPath, "");
            if (!string.IsNullOrEmpty(newPath))
            {
                searchPath = "Assets" + newPath.Replace(Application.dataPath, "");
            }
        }
        EditorGUILayout.EndHorizontal();

        includeSubfolders = EditorGUILayout.Toggle("Include Subfolders", includeSubfolders);

        GUILayout.Space(8);

        if (targetParticleDatas == null)
        {
            EditorGUILayout.HelpBox("Please assign a ParticleDatas asset first.", MessageType.Warning);
            return;
        }

        if (GUILayout.Button("Scan for Particle Prefabs", GUILayout.Height(28)))
        {
            ScanForPrefabs();
        }

        GUILayout.Space(8);

        // 列表显示 + 过滤
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Filter:", GUILayout.Width(40));
        searchFilter = EditorGUILayout.TextField(searchFilter);
        EditorGUILayout.EndHorizontal();

        var list = targetParticleDatas.particleDataList;
        if (list != null && list.Count > 0)
        {
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(320));
            for (int i = 0; i < list.Count; i++)
            {
                var d = list[i];
                if (!string.IsNullOrEmpty(searchFilter)
                    && !d.effectName.ToLower().Contains(searchFilter.ToLower())
                    && !d.effectPath.ToLower().Contains(searchFilter.ToLower()))
                    continue;

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Name", GUILayout.Width(40));
                d.effectName = EditorGUILayout.TextField(d.effectName);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Path", GUILayout.Width(40));
                EditorGUILayout.SelectableLabel(d.effectPath, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Remove", GUILayout.Width(70)))
                {
                    if (EditorUtility.DisplayDialog("Remove Particle Data", $"Remove '{d.effectName}' ?", "Yes", "No"))
                    {
                        list.RemoveAt(i);
                        EditorUtility.SetDirty(targetParticleDatas);
                        AssetDatabase.SaveAssets();
                        break;
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();

            GUILayout.Space(6);
            if (GUILayout.Button("Clear All", GUILayout.Height(24)))
            {
                if (EditorUtility.DisplayDialog("Clear All", "Clear all particle data items?", "Yes", "No"))
                {
                    list.Clear();
                    EditorUtility.SetDirty(targetParticleDatas);
                    AssetDatabase.SaveAssets();
                }
            }
            if (GUILayout.Button("Save Changes", GUILayout.Height(24)))
            {
                EditorUtility.SetDirty(targetParticleDatas);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("Save Complete", "Particle data saved.", "OK");
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No particle data. Click 'Scan for Particle Prefabs'.", MessageType.Info);
        }
    }

    private void ScanForPrefabs()
    {
        if (targetParticleDatas == null) return;

        var searchOption = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var found = new List<ParticleData>();
        int processed = 0, valid = 0;

        foreach (var ext in prefabExtensions)
        {
            var files = Directory.GetFiles(searchPath, "*" + ext, searchOption);
            foreach (var file in files)
            {
                var assetPath = file.Replace("\\", "/");
                if (assetPath.EndsWith(".meta")) continue;
                processed++;

                // 要求 prefab 位于 Resources 下，便于运行时 Resources.Load
                string resPath = GetResourcesRelativePath(assetPath);
                if (string.IsNullOrEmpty(resPath))
                {
                    // 不在 Resources 下的也可记录，但运行时需用 Addressables 或自定义加载
                    // 这里保持与 Audio 的策略：仍记录相对 Assets 路径（去掉扩展）
                    resPath = GetAssetsRelativePathNoExt(assetPath);
                }

                string fileName = Path.GetFileNameWithoutExtension(assetPath);
                found.Add(new ParticleData
                {
                    effectName = fileName,
                    effectPath = resPath
                });
                valid++;
            }
        }

        targetParticleDatas.particleDataList = found;
        EditorUtility.SetDirty(targetParticleDatas);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Scan Complete", $"Processed {processed} files\nFound {valid} prefabs", "OK");
        Debug.Log($"[ParticleDataEditor] Scan: processed={processed}, valid={valid}");
    }

    private string GetResourcesRelativePath(string fullPath)
    {
        int idx = fullPath.IndexOf("Resources/");
        if (idx == -1) return null;

        string rel = fullPath.Substring(idx + "Resources/".Length);
        rel = rel.Replace(".prefab", "").Replace(".meta", "");
        return rel;
    }

    private string GetAssetsRelativePathNoExt(string fullPath)
    {
        if (fullPath.StartsWith("Assets/"))
            return fullPath.Substring("Assets/".Length).Replace(".prefab","").Replace(".meta","");
        return fullPath.Replace(".prefab","").Replace(".meta","");
    }

    // 右键：快速对当前选中的 ParticleDatas 资产执行扫描
    [MenuItem("Assets/Create/Particle Data from Selection", false, 101)]
    public static void CreateParticleDataFromSelection()
    {
        var pd = Selection.activeObject as ParticleDatas;
        if (pd == null)
        {
            Debug.LogWarning("请先选中一个 ParticleDatas 资产");
            return;
        }
        var wnd = GetWindow<ParticleDataEditor>();
        wnd.targetParticleDatas = pd;
        wnd.ScanForPrefabs();
    }

    [MenuItem("Assets/Create/Particle Data from Selection", true)]
    public static bool ValidateCreateParticleDataFromSelection()
    {
        return Selection.activeObject is ParticleDatas;
    }
}