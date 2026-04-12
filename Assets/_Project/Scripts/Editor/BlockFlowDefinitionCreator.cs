using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-click asset creator that generates every <see cref="BlockDefinition"/>,
/// <see cref="GrinderDefinition"/>, <see cref="BlockDefinitionCatalog"/> and
/// <see cref="GrinderDefinitionCatalog"/> from the meshes found in
/// <c>Assets/Art/Meshes/</c>. Run via <c>BlockFlow → Create All Definitions</c>.
///
/// Idempotent: re-running overwrites existing assets at the same paths so
/// the GUIDs stay stable across re-creations.
/// </summary>
public static class BlockFlowDefinitionCreator
{
    private const string MeshRoot = "Assets/Art/Meshes/";
    private const string DefRoot  = "Assets/_Project/ScriptableObjects/";

    private struct ShapeEntry
    {
        public string ShapeId;
        public string MeshFileName;   // e.g. "SM_Block_Cube_S_01"
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

        var shapes = GetShapeEntries();
        var blockDefs = new List<BlockDefinition>();

        foreach (var entry in shapes)
        {
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

            // Wire mesh prefab — look for the FBX at the expected path.
            var meshPath = MeshRoot + entry.MeshFileName + ".fbx";
            var meshAsset = AssetDatabase.LoadAssetAtPath<GameObject>(meshPath);
            so.FindProperty("meshPrefab").objectReferenceValue = meshAsset;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
            blockDefs.Add(def);
        }

        // Block catalog
        var blockCatalog = CreateOrLoad<BlockDefinitionCatalog>(DefRoot + "BlockDefinitionCatalog.asset");
        {
            var so = new SerializedObject(blockCatalog);
            var arr = so.FindProperty("definitions");
            arr.arraySize = blockDefs.Count;
            for (int i = 0; i < blockDefs.Count; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = blockDefs[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(blockCatalog);
        }

        // Grinders
        var grinders = GetGrinderEntries();
        var grinderDefs = new List<GrinderDefinition>();

        foreach (var entry in grinders)
        {
            var def = CreateOrLoad<GrinderDefinition>(DefRoot + "Grinders/GrinderDef_" + entry.Width + "X.asset");
            var so = new SerializedObject(def);
            so.FindProperty("width").intValue = entry.Width;
            so.FindProperty("displayName").stringValue = entry.DisplayName;

            var meshPath = MeshRoot + entry.MeshFileName + ".fbx";
            var meshAsset = AssetDatabase.LoadAssetAtPath<GameObject>(meshPath);
            so.FindProperty("meshPrefab").objectReferenceValue = meshAsset;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
            grinderDefs.Add(def);
        }

        // Grinder catalog
        var grinderCatalog = CreateOrLoad<GrinderDefinitionCatalog>(DefRoot + "GrinderDefinitionCatalog.asset");
        {
            var so = new SerializedObject(grinderCatalog);
            var arr = so.FindProperty("definitions");
            arr.arraySize = grinderDefs.Count;
            for (int i = 0; i < grinderDefs.Count; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = grinderDefs[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(grinderCatalog);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[BlockFlowDefinitionCreator] Created {blockDefs.Count} block definitions + catalog, {grinderDefs.Count} grinder definitions + catalog under {DefRoot}.");
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

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        // Walk path segments and create each missing folder.
        var parts = path.Replace("\\", "/").Split('/');
        string current = parts[0]; // "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
