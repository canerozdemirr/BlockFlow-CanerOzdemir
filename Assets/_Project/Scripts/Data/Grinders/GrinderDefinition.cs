using UnityEngine;

// Width is the maximum accepted block edge (2X grinder accepts edges of 1 or 2).
[CreateAssetMenu(menuName = "BlockFlow/Data/Grinder Definition", fileName = "GrinderDefinition_")]
public sealed class GrinderDefinition : ScriptableObject
{
    [SerializeField, Range(1, 3), Tooltip("Maximum block edge width this grinder accepts (1, 2, or 3 cells).")]
    private int width = 1;

    [SerializeField, Tooltip("Display name for editor UIs and debug overlays.")]
    private string displayName;

    [SerializeField, Tooltip("Mesh prefab instantiated as the visual root of the grinder view.")]
    private GameObject meshPrefab;

    public int Width => width;
    public string DisplayName => displayName;
    public GameObject MeshPrefab => meshPrefab;
}
