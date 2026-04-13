using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Animates UI Toolkit popup show/hide using PrimeTween.Custom.
/// Uses the "hidden" USS class for visibility toggling so elements
/// remain queryable even when not displayed.
/// </summary>
public static class UIToolkitPopupAnimator
{
    private const string HiddenClass = "hidden";

    public static void AnimateShow(VisualElement root, VisualElement overlay,
        VisualElement panel, float duration = 0.4f, Action onComplete = null)
    {
        if (root == null) { onComplete?.Invoke(); return; }
        root.RemoveFromClassList(HiddenClass);

        if (overlay != null)
        {
            overlay.style.opacity = 0f;
            Tween.Custom(0f, 1f, duration, val => overlay.style.opacity = val,
                ease: Ease.OutQuad);
        }

        if (panel != null)
        {
            panel.transform.scale = new Vector3(0.4f, 0.4f, 1f);
            panel.transform.position = new Vector3(0, 60f, 0);
            panel.style.opacity = 0f;

            Tween.Custom(0.4f, 1f, duration, val =>
                panel.transform.scale = new Vector3(val, val, 1f),
                ease: Ease.OutBack);

            Tween.Custom(60f, 0f, duration * 0.8f, val =>
                panel.transform.position = new Vector3(0, val, 0),
                ease: Ease.OutCubic);

            Tween.Custom(0f, 1f, duration * 0.5f, val =>
                panel.style.opacity = val,
                ease: Ease.OutQuad);
        }

        if (onComplete != null)
            Tween.Delay(duration, onComplete);
    }

    public static void AnimateHide(VisualElement root, VisualElement overlay,
        VisualElement panel, float duration = 0.25f, Action onComplete = null)
    {
        if (root == null) { onComplete?.Invoke(); return; }

        if (overlay != null)
        {
            Tween.Custom(1f, 0f, duration, val => overlay.style.opacity = val,
                ease: Ease.InQuad);
        }

        if (panel != null)
        {
            Tween.Custom(1f, 0.85f, duration, val =>
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

    /// <summary>Hides immediately without animation.</summary>
    public static void HideImmediate(VisualElement root)
    {
        if (root != null) root.AddToClassList(HiddenClass);
    }
}
