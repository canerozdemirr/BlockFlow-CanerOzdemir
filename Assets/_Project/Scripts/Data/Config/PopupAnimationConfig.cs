using UnityEngine;

/// <summary>
/// Tuning for UI Toolkit popup show/hide tweens in <see cref="UIToolkitPopupAnimator"/>.
/// Assigned at gameplay bootstrap via the static <c>Config</c> hook.
/// </summary>
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
}
