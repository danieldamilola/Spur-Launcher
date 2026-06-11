using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Spur.Views;

public partial class CategoryCircle : UserControl
{
    public static readonly DependencyProperty IconGlyphProperty = DependencyProperty.Register(
        nameof(IconGlyph), typeof(string), typeof(CategoryCircle),
        new PropertyMetadata("\ue946", OnIconGlyphChanged));

    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label), typeof(string), typeof(CategoryCircle),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
        nameof(IsActive), typeof(bool), typeof(CategoryCircle),
        new PropertyMetadata(false, OnIsActiveChanged));

    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public CategoryCircle()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplyVisualState();
    }

    public event RoutedEventHandler? Click
    {
        add => AddHandler(Button.ClickEvent, value);
        remove => RemoveHandler(Button.ClickEvent, value);
    }

    public void PlayShowAnimation(int index, bool animate)
    {
        IsHitTestVisible = true;
        if (!animate)
        {
            Opacity = 1;
            CircleScale.ScaleX = CircleScale.ScaleY = 1;
            return;
        }

        var delay = TimeSpan.FromMilliseconds(SpurMotion.AnchorStaggerMs * index);
        var duration = TimeSpan.FromMilliseconds(SpurMotion.AnchorRevealDurationMs);
        var ease = SpurMotion.EaseOut();

        BeginAnimation(OpacityProperty, null);
        BeginAnimation(OpacityProperty,
            new DoubleAnimation(0, 1, duration) { BeginTime = delay, EasingFunction = ease });

        CircleScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        CircleScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        var scale = new DoubleAnimation(0.92, 1, duration) { BeginTime = delay, EasingFunction = ease };
        CircleScale.BeginAnimation(ScaleTransform.ScaleXProperty, scale);
        CircleScale.BeginAnimation(ScaleTransform.ScaleYProperty, scale);
    }

    public void PlayHideAnimation(bool animate, int durationMs = SpurMotion.AnchorHideDurationMs)
    {
        if (!animate)
        {
            Opacity = 0;
            CircleScale.ScaleX = CircleScale.ScaleY = 0.92;
            IsHitTestVisible = false;
            return;
        }

        // Cancel any in-progress opacity animation so the old Completed
        // handler doesn't fire after we've already started showing again.
        BeginAnimation(OpacityProperty, null);

        var duration = TimeSpan.FromMilliseconds(durationMs);
        var ease = SpurMotion.EaseIn();

        var fade = new DoubleAnimation(Opacity, 0, duration) { EasingFunction = ease };
        fade.Completed += (_, _) => IsHitTestVisible = false;
        BeginAnimation(OpacityProperty, fade);

        CircleScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        CircleScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        var scale = new DoubleAnimation(1, 0.92, duration) { EasingFunction = ease };
        CircleScale.BeginAnimation(ScaleTransform.ScaleXProperty, scale);
        CircleScale.BeginAnimation(ScaleTransform.ScaleYProperty, scale);
    }

    private static void OnIconGlyphChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CategoryCircle cc && !cc.IsActive && e.NewValue is string sym)
            cc.IconPresenter.Glyph = sym;
    }

    private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CategoryCircle cc)
            cc.ApplyVisualState();
    }

    private void ApplyVisualState()
    {
        IconPresenter.Glyph = IconGlyph;
    }
}
