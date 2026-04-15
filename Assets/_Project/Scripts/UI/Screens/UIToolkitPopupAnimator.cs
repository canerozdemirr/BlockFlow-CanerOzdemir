using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

// Uses the "hidden" USS class for visibility toggling so elements remain
// queryable when not displayed.
public static class UIToolkitPopupAnimator
{
    private const string HiddenClass = "hidden";

    // Null means bootstrap never ran (pure-UI test scenes); fallback constants apply.
    public static PopupAnimationConfig Config { get; set; }

    public static void AnimateShow(VisualElement root, VisualElement overlay,
        VisualElement panel, float? durationOverride = null, Action onComplete = null)
    {
        if (root == null) { onComplete?.Invoke(); return; }
        root.RemoveFromClassList(HiddenClass);

        float duration = durationOverride ?? (Config != null ? Config.ShowDuration : 0.4f);
        float startScale = Config != null ? Config.StartScale : 0.4f;
        float dropOffset = Config != null ? Config.DropOffset : 60f;
        float posFactor = Config != null ? Config.PositionDurationFactor : 0.8f;
        float opacityFactor = Config != null ? Config.OpacityDurationFactor : 0.5f;

        if (overlay != null)
        {
            overlay.style.opacity = 0f;
            Tween.Custom(0f, 1f, duration, val => overlay.style.opacity = val,
                ease: Ease.OutQuad);
        }

        if (panel != null)
        {
            panel.transform.scale = new Vector3(startScale, startScale, 1f);
            panel.transform.position = new Vector3(0, dropOffset, 0);
            panel.style.opacity = 0f;

            Tween.Custom(startScale, 1f, duration, val =>
                panel.transform.scale = new Vector3(val, val, 1f),
                ease: Ease.OutBack);

            Tween.Custom(dropOffset, 0f, duration * posFactor, val =>
                panel.transform.position = new Vector3(0, val, 0),
                ease: Ease.OutCubic);

            Tween.Custom(0f, 1f, duration * opacityFactor, val =>
                panel.style.opacity = val,
                ease: Ease.OutQuad);
        }

        if (onComplete != null)
            Tween.Delay(duration, onComplete);
    }

    public static void AnimateHide(VisualElement root, VisualElement overlay,
        VisualElement panel, float? durationOverride = null, Action onComplete = null)
    {
        if (root == null) { onComplete?.Invoke(); return; }

        float duration = durationOverride ?? (Config != null ? Config.HideDuration : 0.25f);
        float hideScale = Config != null ? Config.HideScale : 0.85f;

        if (overlay != null)
        {
            Tween.Custom(1f, 0f, duration, val => overlay.style.opacity = val,
                ease: Ease.InQuad);
        }

        if (panel != null)
        {
            Tween.Custom(1f, hideScale, duration, val =>
                panel.transform.scale = new Vector3(val, val, 1f),
                ease: Ease.InBack);

            Tween.Custom(1f, 0f, duration, val =>
                panel.style.opacity = val,
                ease: Ease.InQuad);
        }

        Tween.Delay(duration, () =>
        {
            root.AddToClassList(HiddenClass);
            if (panel != null)
            {
                panel.transform.scale = Vector3.one;
                panel.transform.position = Vector3.zero;
                panel.style.opacity = 1f;
            }
            if (overlay != null)
                overlay.style.opacity = 1f;

            onComplete?.Invoke();
        });
    }

    public static void HideImmediate(VisualElement root)
    {
        if (root != null) root.AddToClassList(HiddenClass);
    }
}
