using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-click asset creator that generates everything the runtime needs to
/// spawn blocks and grinders:
///
/// <list type="bullet">
///   <item><b>Prefabs</b> — one per shape / grinder width, with <see cref="BlockView"/> or
///     <see cref="GrinderView"/> on the root, materials assigned, and serialized
///     fields (<c>colorRenderer</c>, <c>visualRoot</c>) wired.</item>
///   <item><b>Definitions</b> — one SO per shape / grinder width, with shape id, cell
///     offsets, display name, and <c>meshPrefab</c> pointing at the generated prefab.</item>
///   <item><b>Catalogs</b> — <see cref="BlockDefinitionCatalog"/> and
///     <see cref="GrinderDefinitionCatalog"/> with every definition pre-populated.</item>
/// </list>
///
/// Run via <c>BlockFlow → Create All Definitions</c>. Idempotent — re-running
/// overwrites existing assets so GUIDs stay stable.
/// </summary>
public static class BlockFlowDefinitionCreator
{
    private const string MeshRoot    = "Assets/Art/Meshes/";
    private const string MatRoot     = "Assets/Art/Materials/";
    private const string DefRoot     = "Assets/_Project/ScriptableObjects/";
    private const string PrefabRoot  = "Assets/_Project/Prefabs/Gameplay/";

    private struct ShapeEntry
    {
        public string ShapeId;
        public string MeshFileName;
        public string DisplayName;
        public GridCoord[] Offsets;
    }

    private struct GrinderEntry
    {
        public int Width;
        public string MeshFileName;
        public string DisplayName;
    }

    [MenuItem("BlockFlow/Create All Definitions")]
    private static void CreateAll()
    {
        EnsureFolder(DefRoot + "Blocks");
        EnsureFolder(DefRoot + "Grinders");
        EnsureFolder(PrefabRoot + "Blocks");
        EnsureFolder(PrefabRoot + "Grinders");

        var blockBaseMat  = AssetDatabase.LoadAssetAtPath<Material>(MatRoot + "M_Block_Base.mat");
        var grinderBodyMat = AssetDatabase.LoadAssetAtPath<Material>(MatRoot + "M_Grinder_Body.mat");

        if (blockBaseMat == null)  Debug.LogWarning("[DefinitionCreator] M_Block_Base.mat not found at " + MatRoot);
        if (grinderBodyMat == null) Debug.LogWarning("[DefinitionCreator] M_Grinder_Body.mat not found at " + MatRoot);

        // ---- Blocks ----
        var shapes = GetShapeEntries();
        var blockDefs = new List<BlockDefinition>();

        foreach (var entry in shapes)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(MeshRoot + entry.MeshFileName + ".fbx");
            if (fbx == null)
            {
                Debug.LogWarning($"[DefinitionCreator] FBX not found: {entry.MeshFileName}.fbx — skipping.");
                continue;
            }

            // --- prefab: reuse existing, only create if missing ---
            var prefabPath = PrefabRoot + "Blocks/Block_" + entry.ShapeId + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                prefab = CreateBlockPrefab(fbx, prefabPath, blockBaseMat, entry.ShapeId);

            // --- create / update definition ---
            var def = CreateOrLoad<BlockDefinition>(DefRoot + "Blocks/BlockDef_" + entry.ShapeId + ".asset");
            var so = new SerializedObject(def);
            so.FindProperty("shapeId").stringValue = entry.ShapeId;
            so.FindProperty("displayName").stringValue = entry.DisplayName;

            var offsetsProp = so.FindProperty("cellOffsets");
            offsetsProp.arraySize = entry.Offsets.Length;
            for (int i = 0; i < entry.Offsets.Length; i++)
            {
                var elem = offsetsProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("x").intValue = entry.Offsets[i].x;
                elem.FindPropertyRelative("y").intValue = entry.Offsets[i].y;
            }

            so.FindProperty("meshPrefab").objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
            blockDefs.Add(def);
        }

        // Block catalog
        var blockCatalog = CreateOrLoad<BlockDefinitionCatalog>(DefRoot + "BlockDefinitionCatalog.asset");
        SetArray(blockCatalog, "definitions", blockDefs);

        // ---- Grinders ----
        var grinders = GetGrinderEntries();
        var grinderDefs = new List<GrinderDefinition>();

        foreach (var entry in grinders)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(MeshRoot + entry.MeshFileName + ".fbx");
            if (fbx == null)
            {
                Debug.LogWarning($"[DefinitionCreator] FBX not found: {entry.MeshFileName}.fbx — skipping.");
                continue;
            }

            var prefabPath = PrefabRoot + "Grinders/Grinder_" + entry.Width + "X.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                prefab = CreateGrinderPrefab(fbx, prefabPath, grinderBodyMat, entry.Width);

            var def = CreateOrLoad<GrinderDefinition>(DefRoot + "Grinders/GrinderDef_" + entry.Width + "X.asset");
            var so = new SerializedObject(def);
            so.FindProperty("width").intValue = entry.Width;
            so.FindProperty("displayName").stringValue = entry.DisplayName;
            so.FindProperty("meshPrefab").objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
            grinderDefs.Add(def);
        }

        var grinderCatalog = CreateOrLoad<GrinderDefinitionCatalog>(DefRoot + "GrinderDefinitionCatalog.asset");
        SetArray(grinderCatalog, "definitions", grinderDefs);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[DefinitionCreator] Created {blockDefs.Count} block prefabs + definitions, " +
                  $"{grinderDefs.Count} grinder prefabs + definitions, and both catalogs under {DefRoot}.");
    }

    // ---- prefab builders ----

    /// <summary>
    /// Creates a block prefab with structure:
    ///   Block_ShapeId (root — BlockView)
    ///     └── MeshChild (FBX instance — Renderer with M_Block_Base)
    /// </summary>
    private static GameObject CreateBlockPrefab(GameObject fbx, string path, Material mat, string shapeId)
    {
        // Build a temporary scene hierarchy.
        var root = new GameObject("Block_" + shapeId);

        var meshInstance = Object.Instantiate(fbx, root.transform);
        meshInstance.name = fbx.name;

        // Assign material to every renderer in the mesh.
        if (mat != null)
        {
            foreach (var renderer in meshInstance.GetComponentsInChildren<Renderer>())
            {
                var mats = renderer.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                renderer.sharedMaterials = mats;
            }
        }

        // Add BlockView and wire serialized fields.
        var view = root.AddComponent<BlockView>();
        var renderer0 = meshInstance.GetComponentInChildren<Renderer>();

        var viewSo = new SerializedObject(view);
        viewSo.FindProperty("colorRenderer").objectReferenceValue = renderer0;
        viewSo.FindProperty("visualRoot").objectReferenceValue = meshInstance.transform;
        viewSo.ApplyModifiedPropertiesWithoutUndo();

        // Save to disk.
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    /// <summary>
    /// Creates a grinder prefab with structure:
    ///   Grinder_NX (root — GrinderView)
    ///     └── MeshChild (FBX instance — Renderer with M_Grinder_Body)
    /// </summary>
    private static GameObject CreateGrinderPrefab(GameObject fbx, string path, Material mat, int width)
    {
        var root = new GameObject("Grinder_" + width + "X");

        var meshInstance = Object.Instantiate(fbx, root.transform);
        meshInstance.name = fbx.name;

        if (mat != null)
        {
            foreach (var renderer in meshInstance.GetComponentsInChildren<Renderer>())
            {
                var mats = renderer.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                renderer.sharedMaterials = mats;
            }
        }

        var view = root.AddComponent<GrinderView>();
        var renderer0 = meshInstance.GetComponentInChildren<Renderer>();

        var viewSo = new SerializedObject(view);
        viewSo.FindProperty("colorRenderer").objectReferenceValue = renderer0;
        viewSo.FindProperty("visualRoot").objectReferenceValue = meshInstance.transform;
        viewSo.ApplyModifiedPropertiesWithoutUndo();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    // ---- shape data ----

    private static ShapeEntry[] GetShapeEntries() => new[]
    {
        new ShapeEntry
        {
            ShapeId = "Cube_S", MeshFileName = "SM_Block_Cube_S_01",
            DisplayName = "Cube Small (1x1)",
            Offsets = new[] { C(0,0) }
        },
        new ShapeEntry
        {
            ShapeId = "Cube_SL", MeshFileName = "SM_Block_Cube_SL_01",
            DisplayName = "Cube Small-Long (2x1)",
            Offsets = new[] { C(0,0), C(1,0) }
        },
        new ShapeEntry
        {
            ShapeId = "Line_S", MeshFileName = "SM_Block_Line_S_01",
            DisplayName = "Line Small (2x1)",
            Offsets = new[] { C(0,0), C(1,0) }
        },
        new ShapeEntry
        {
            ShapeId = "Line_L", MeshFileName = "SM_Block_Line_L_01",
            DisplayName = "Line Large (3x1)",
            Offsets = new[] { C(0,0), C(1,0), C(2,0) }
        },
        new ShapeEntry
        {
            ShapeId = "L_S", MeshFileName = "SM_Block_L_S_01",
            DisplayName = "L Small (3 cells)",
            Offsets = new[] { C(0,0), C(1,0), C(0,1) }
        },
        new ShapeEntry
        {
            ShapeId = "L_L", MeshFileName = "SM_Block_L_L_01",
            DisplayName = "L Large (4 cells)",
            Offsets = new[] { C(0,0), C(1,0), C(0,1), C(0,2) }
        },
        new ShapeEntry
        {
            ShapeId = "L_L_Mirror", MeshFileName = "SM_Block_L_L_Mirror_01",
            DisplayName = "L Large Mirror (4 cells)",
            Offsets = new[] { C(0,0), C(1,0), C(1,1), C(1,2) }
        },
        new ShapeEntry
        {
            ShapeId = "T_L", MeshFileName = "SM_Block_T_L_01",
            DisplayName = "T Large (4 cells)",
            Offsets = new[] { C(0,0), C(1,0), C(2,0), C(1,1) }
        },
        new ShapeEntry
        {
            ShapeId = "Z_L", MeshFileName = "SM_Block_Z_L_01",
            DisplayName = "Z Large (4 cells)",
            Offsets = new[] { C(0,0), C(1,0), C(1,1), C(2,1) }
        },
        new ShapeEntry
        {
            ShapeId = "Plus_L", MeshFileName = "SM_Block_Plus_L_01",
            DisplayName = "Plus Large (5 cells)",
            Offsets = new[] { C(1,0), C(0,1), C(1,1), C(2,1), C(1,2) }
        }
    };

    private static GrinderEntry[] GetGrinderEntries() => new[]
    {
        new GrinderEntry { Width = 1, MeshFileName = "SM_Grinder_1X_01", DisplayName = "Grinder 1X" },
        new GrinderEntry { Width = 2, MeshFileName = "SM_Grinder_2X_01", DisplayName = "Grinder 2X" },
        new GrinderEntry { Width = 3, MeshFileName = "SM_Grinder_3X_01", DisplayName = "Grinder 3X" }
    };

    // ---- helpers ----

    private static GridCoord C(int x, int y) => new GridCoord(x, y);

    private static T CreateOrLoad<T>(string path) where T : ScriptableObject
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null) return existing;

        var asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void SetArray<T>(ScriptableObject target, string fieldName, List<T> items) where T : Object
    {
        var so = new SerializedObject(target);
        var arr = so.FindProperty(fieldName);
        arr.arraySize = items.Count;
        for (int i = 0; i < items.Count; i++)
            arr.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        var parts = path.Replace("\\", "/").Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
