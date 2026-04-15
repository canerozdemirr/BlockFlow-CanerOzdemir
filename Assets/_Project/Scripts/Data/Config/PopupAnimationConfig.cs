using UnityEngine;

[CreateAssetMenu(menuName = "BlockFlow/Config/Popup Animation", fileName = "PopupAnimationConfig")]
public sealed class PopupAnimationConfig : ScriptableObject
{
    [SerializeField, Min(0f)] private float showDuration = 0.4f;
    [SerializeField, Min(0f)] private float hideDuration = 0.25f;

    [SerializeField, Tooltip("Panel starts at this uniform scale and springs to 1.")]
    private float startScale = 0.4f;
    [SerializeField, Tooltip("Panel shrinks to this scale on hide.")]
    private float hideScale = 0.85f;

    [SerializeField, Tooltip("Panel Y offset it drops from on show.")]
    private float dropOffset = 60f;

    [SerializeField, Range(0f, 1f), Tooltip("Fraction of show duration used for the Y position ease.")]
    private float positionDurationFactor = 0.8f;
    [SerializeField, Range(0f, 1f), Tooltip("Fraction of show duration used for the panel opacity ease.")]
    private float opacityDurationFactor = 0.5f;

    public float ShowDuration => showDuration;
    public float HideDuration => hideDuration;
    public float StartScale => startScale;
    public float HideScale => hideScale;
    public float DropOffset => dropOffset;
    public float PositionDurationFactor => positionDurationFactor;
    public float OpacityDurationFactor => opacityDurationFactor;

    [Header("Win stars")]
    [SerializeField, Min(0f), Tooltip("Delay before the first star animation starts.")]
    private float starInitialDelay = 0.2f;
    [SerializeField, Min(0f), Tooltip("Stagger between each star's animation.")]
    private float starStagger = 0.25f;
    [SerializeField, Min(0f), Tooltip("Scale-up duration before the settle.")]
    private float starScaleUpDuration = 0.3f;
    [SerializeField, Min(0f), Tooltip("Settle duration back to scale 1.")]
    private float starSettleDuration = 0.2f;
    [SerializeField, Tooltip("Peak scale for filled stars before settling to 1.")]
    private float starBounceScale = 1.35f;
    [SerializeField, Min(0f), Tooltip("Rotation wobble duration for filled stars.")]
    private float starRotationDuration = 0.4f;
    [SerializeField, Tooltip("Rotation wobble angle applied to side stars (idx 0 = -value, idx 2 = +value).")]
    private float starRotationWobble = 12f;
    [SerializeField, Min(0f), Tooltip("Scale duration for empty stars.")]
    private float emptyStarScaleDuration = 0.25f;
    [SerializeField, Range(0f, 1f), Tooltip("Opacity of empty stars.")]
    private float emptyStarOpacity = 0.25f;

    public float StarInitialDelay => starInitialDelay;
    public float StarStagger => starStagger;
    public float StarScaleUpDuration => starScaleUpDuration;
    public float StarSettleDuration => starSettleDuration;
    public float StarBounceScale => starBounceScale;
    public float StarRotationDuration => starRotationDuration;
    public float StarRotationWobble => starRotationWobble;
    public float EmptyStarScaleDuration => emptyStarScaleDuration;
    public float EmptyStarOpacity => emptyStarOpacity;
}
