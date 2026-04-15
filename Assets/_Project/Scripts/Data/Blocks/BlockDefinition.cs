using UnityEngine;

[CreateAssetMenu(menuName = "BlockFlow/Data/Block Definition", fileName = "BlockDefinition_")]
public sealed class BlockDefinition : ScriptableObject
{
    [SerializeField, Tooltip("Stable id referenced by level JSON (e.g. \"L_L\", \"Plus_L\").")]
    private string shapeId;

    [SerializeField, Tooltip("Display name for editor UIs and debug overlays.")]
    private string displayName;

    [SerializeField, Tooltip("Cells occupied by this shape, relative to the block's origin cell.")]
    private GridCoord[] cellOffsets;

    [SerializeField, Tooltip("Mesh prefab instantiated as the visual root of the block view.")]
    private GameObject meshPrefab;

    public string ShapeId => shapeId;
    public string DisplayName => displayName;
    public GridCoord[] CellOffsets => cellOffsets;
    public GameObject MeshPrefab => meshPrefab;
    public int CellCount => cellOffsets == null ? 0 : cellOffsets.Length;

    public GridSize GetBounds()
    {
        if (cellOffsets == null || cellOffsets.Length == 0)
            return new GridSize(0, 0);

        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;
        for (int i = 0; i < cellOffsets.Length; i++)
        {
            var o = cellOffsets[i];
            if (o.x < minX) minX = o.x;
            if (o.y < minY) minY = o.y;
            if (o.x > maxX) maxX = o.x;
            if (o.y > maxY) maxY = o.y;
        }
        return new GridSize(maxX - minX + 1, maxY - minY + 1);
    }
}
