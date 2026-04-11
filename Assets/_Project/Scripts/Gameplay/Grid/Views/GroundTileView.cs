using UnityEngine;

/// <summary>
/// Marker component sitting on ground tile prefabs. No logic yet; the type
/// exists so the <see cref="GridBuilder"/> can locate tiles in hierarchy
/// queries and so future systems (baked decals, ripple VFX) have a stable
/// handle to attach to.
/// </summary>
public sealed class GroundTileView : MonoBehaviour
{
}
