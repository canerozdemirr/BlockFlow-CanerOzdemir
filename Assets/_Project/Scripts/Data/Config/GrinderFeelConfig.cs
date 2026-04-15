using UnityEngine;

[CreateAssetMenu(menuName = "BlockFlow/Config/Grinder Feel", fileName = "GrinderFeelConfig")]
public sealed class GrinderFeelConfig : ScriptableObject
{
    [SerializeField, Min(0f), Tooltip("Base slide duration for a 1-cell-thick block.")]
    private float consumeTweenDuration = 0.5f;

    [SerializeField, Min(0f), Tooltip("Extra seconds added per cell of block extent along the slide axis.")]
    private float durationPerCellExtent = 0.1f;

    [SerializeField, Min(0f), Tooltip("Extra cells of travel past the grinder edge so the block fully clears the clip plane.")]
    private float slideMargin = 1.5f;

    public float ConsumeTweenDuration => consumeTweenDuration;
    public float DurationPerCellExtent => durationPerCellExtent;
    public float SlideMargin => slideMargin;
}
