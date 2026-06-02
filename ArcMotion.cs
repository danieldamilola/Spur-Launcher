using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Arc.Views;

namespace Arc;

/// <summary>Launcher motion primitives — timings from motion.md.</summary>
public static class ArcMotion
{
    public const int ShowDurationMs = 200;
    public const int HideDurationMs = 140;
    public const int AnchorRevealDurationMs = 150;
    public const int AnchorHideDurationMs = 100;
    public const int AnchorTypingHideDurationMs = 60;
    public const int AnchorStaggerMs = 30;
    public const int ColumnRevealDurationMs = 200;
    public const int DividerRevealDurationMs = 120;

    public static bool ShouldAnimate(bool userEnabled)
        => userEnabled && SystemParameters.ClientAreaAnimation;

    public static int ScaleMs(int baseMs, bool userEnabled)
    {
        if (!userEnabled) return 0;
        return SystemParameters.ClientAreaAnimation ? baseMs : Math.Max(baseMs / 2, 60);
    }

    public static void Show(Window window, ScaleTransform scale, bool animationEnabled)
    {
        if (!ShouldAnimate(animationEnabled))
        {
            window.Opacity = 1;
            scale.ScaleX = scale.ScaleY = 1;
            return;
        }

        var ms = ScaleMs(ShowDurationMs, animationEnabled);
        var ease = EaseOut();
        window.BeginAnimation(UIElement.OpacityProperty,
            new DoubleAnimation(0, 1, Ms(ms)) { EasingFunction = ease });

        var scaleAnim = new DoubleAnimation(0.97, 1, Ms(ms)) { EasingFunction = ease };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
    }

    public static void Hide(Window window, ScaleTransform scale, bool animationEnabled, Action onComplete)
    {
        if (!ShouldAnimate(animationEnabled))
        {
            window.Opacity = 0;
            scale.ScaleX = scale.ScaleY = 1;
            onComplete();
            return;
        }

        var ms = ScaleMs(HideDurationMs, animationEnabled);
        var ease = EaseIn();
        var fade = new DoubleAnimation(1, 0, Ms(ms)) { EasingFunction = ease };
        fade.Completed += (_, _) => onComplete();
        window.BeginAnimation(UIElement.OpacityProperty, fade);

        var scaleAnim = new DoubleAnimation(1, 0.98, Ms(ms)) { EasingFunction = ease };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
    }

    public static void RevealAnchors(
        ColumnDefinition column,
        GridLength fromWidth,
        double targetWidth,
        UIElement divider,
        UIElement buttonsHost,
        IReadOnlyList<CategoryCircle> anchors,
        bool animationEnabled)
    {
        if (!ShouldAnimate(animationEnabled))
        {
            SetAnchorsInstant(column, targetWidth, divider, buttonsHost, anchors, visible: true);
            return;
        }

        var colMs = ScaleMs(ColumnRevealDurationMs, animationEnabled);
        var divMs = ScaleMs(DividerRevealDurationMs, animationEnabled);
        var widthEase = EaseOut();
        column.BeginAnimation(ColumnDefinition.WidthProperty, null);
        column.BeginAnimation(ColumnDefinition.WidthProperty,
            new GridLengthAnimation(fromWidth, new GridLength(targetWidth), Ms(colMs))
            { EasingFunction = widthEase });

        divider.BeginAnimation(UIElement.OpacityProperty, null);
        divider.BeginAnimation(UIElement.OpacityProperty,
            new DoubleAnimation(0, 1, Ms(divMs)) { EasingFunction = widthEase });

        buttonsHost.BeginAnimation(UIElement.OpacityProperty, null);
        buttonsHost.Opacity = 1;

        for (var i = 0; i < anchors.Count; i++)
            anchors[i].PlayShowAnimation(i, animate: true);
    }

    public static void HideAnchors(
        ColumnDefinition column,
        GridLength fromWidth,
        UIElement divider,
        UIElement buttonsHost,
        IReadOnlyList<CategoryCircle> anchors,
        bool animationEnabled,
        bool fastForTyping = false)
    {
        if (!ShouldAnimate(animationEnabled))
        {
            SetAnchorsInstant(column, 0, divider, buttonsHost, anchors, visible: false);
            return;
        }

        var baseMs = fastForTyping ? AnchorTypingHideDurationMs : AnchorHideDurationMs;
        var durationMs = ScaleMs(baseMs, animationEnabled);
        var ease = EaseIn();

        column.BeginAnimation(ColumnDefinition.WidthProperty, null);
        column.BeginAnimation(ColumnDefinition.WidthProperty,
            new GridLengthAnimation(fromWidth, new GridLength(0), Ms(durationMs))
            { EasingFunction = ease });

        divider.BeginAnimation(UIElement.OpacityProperty, null);
        divider.BeginAnimation(UIElement.OpacityProperty,
            new DoubleAnimation(divider.Opacity, 0, Ms(durationMs)) { EasingFunction = ease });

        buttonsHost.BeginAnimation(UIElement.OpacityProperty, null);
        buttonsHost.BeginAnimation(UIElement.OpacityProperty,
            new DoubleAnimation(buttonsHost.Opacity, 0, Ms(durationMs)) { EasingFunction = ease });

        foreach (var anchor in anchors)
            anchor.PlayHideAnimation(animate: true, durationMs: durationMs);
    }

    private static void SetAnchorsInstant(
        ColumnDefinition column,
        double width,
        UIElement divider,
        UIElement buttonsHost,
        IReadOnlyList<CategoryCircle> anchors,
        bool visible)
    {
        column.Width = new GridLength(width, GridUnitType.Pixel);
        divider.Opacity = visible ? 1 : 0;
        buttonsHost.Opacity = visible ? 1 : 0;

        for (var i = 0; i < anchors.Count; i++)
        {
            if (visible)
                anchors[i].PlayShowAnimation(i, animate: false);
            else
                anchors[i].PlayHideAnimation(animate: false);
        }
    }

    public static void Crossfade(UIElement from, UIElement to, bool animationEnabled, int durationMs = 120)
    {
        if (!ShouldAnimate(animationEnabled))
        {
            from.Opacity = 0;
            to.Opacity = 1;
            return;
        }

        var ms = ScaleMs(durationMs, animationEnabled);
        var ease = EaseOut();
        from.BeginAnimation(UIElement.OpacityProperty,
            new DoubleAnimation(from.Opacity, 0, Ms(ms)) { EasingFunction = ease });
        to.BeginAnimation(UIElement.OpacityProperty,
            new DoubleAnimation(to.Opacity, 1, Ms(ms)) { EasingFunction = ease });
    }

    private static Duration Ms(int ms) => TimeSpan.FromMilliseconds(ms);

    private static CubicEase EaseOut() => new() { EasingMode = EasingMode.EaseOut };

    private static CubicEase EaseIn() => new() { EasingMode = EasingMode.EaseIn };
}

/// <summary>Animates <see cref="ColumnDefinition.Width"/> between grid length values.</summary>
internal sealed class GridLengthAnimation : AnimationTimeline
{
    public GridLength From { get; set; }
    public GridLength To { get; set; }

    public GridLengthAnimation() { }

    public GridLengthAnimation(GridLength from, GridLength to, Duration duration)
    {
        From = from;
        To = to;
        Duration = duration;
    }

    public IEasingFunction? EasingFunction { get; set; }

    public override Type TargetPropertyType => typeof(GridLength);

    protected override Freezable CreateInstanceCore() => new GridLengthAnimation();

    public override object GetCurrentValue(object defaultOriginValue, object originValue, AnimationClock clock)
    {
        if (clock.CurrentProgress is not double t) return To;
        if (EasingFunction is not null)
            t = EasingFunction.Ease(t);

        var from = From.Value;
        var to = To.Value;
        return new GridLength(from + (to - from) * t, GridUnitType.Pixel);
    }
}
