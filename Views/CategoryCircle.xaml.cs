using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Arc.Views;

public partial class CategoryCircle : UserControl
{
    public static readonly DependencyProperty IconDataProperty = DependencyProperty.Register(
        nameof(IconData), typeof(Geometry), typeof(CategoryCircle),
        new PropertyMetadata(null, OnIconDataChanged));

    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label), typeof(string), typeof(CategoryCircle),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
        nameof(IsActive), typeof(bool), typeof(CategoryCircle),
        new PropertyMetadata(false, OnIsActiveChanged));

    public Geometry? IconData
    {
        get => (Geometry?)GetValue(IconDataProperty);
        set => SetValue(IconDataProperty, value);
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
        Btn.Click += (_, _) =>
        {
            var args = new RoutedEventArgs(Button.ClickEvent, this);
            RaiseEvent(args);
        };
    }

    public event RoutedEventHandler? Click
    {
        add => AddHandler(Button.ClickEvent, value);
        remove => RemoveHandler(Button.ClickEvent, value);
    }

    private void OnMouseEnter(object sender, MouseEventArgs e)
    {
        if (IsActive) return;
        Root.Background = TryFindResource("HoverBg") as Brush ?? Root.Background;
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        ApplyVisualState();
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

        var delay = TimeSpan.FromMilliseconds(ArcMotion.AnchorStaggerMs * index);
        var duration = TimeSpan.FromMilliseconds(ArcMotion.AnchorRevealDurationMs);
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        BeginAnimation(OpacityProperty, null);
        BeginAnimation(OpacityProperty,
            new DoubleAnimation(0, 1, duration) { BeginTime = delay, EasingFunction = ease });

        CircleScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        CircleScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        var scale = new DoubleAnimation(0.92, 1, duration) { BeginTime = delay, EasingFunction = ease };
        CircleScale.BeginAnimation(ScaleTransform.ScaleXProperty, scale);
        CircleScale.BeginAnimation(ScaleTransform.ScaleYProperty, scale);
    }

    public void PlayHideAnimation(bool animate, int durationMs = ArcMotion.AnchorHideDurationMs)
    {
        if (!animate)
        {
            Opacity = 0;
            CircleScale.ScaleX = CircleScale.ScaleY = 0.92;
            IsHitTestVisible = false;
            return;
        }

        var duration = TimeSpan.FromMilliseconds(durationMs);
        var ease = new CubicEase { EasingMode = EasingMode.EaseIn };

        var fade = new DoubleAnimation(Opacity, 0, duration) { EasingFunction = ease };
        fade.Completed += (_, _) => IsHitTestVisible = false;
        BeginAnimation(OpacityProperty, fade);

        var scale = new DoubleAnimation(1, 0.92, duration) { EasingFunction = ease };
        CircleScale.BeginAnimation(ScaleTransform.ScaleXProperty, scale);
        CircleScale.BeginAnimation(ScaleTransform.ScaleYProperty, scale);
    }

    private static void OnIconDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CategoryCircle cc && !cc.IsActive && e.NewValue is Geometry g)
            cc.IconPath.Data = g;
    }

    private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CategoryCircle cc)
            cc.ApplyVisualState();
    }

    private void ApplyVisualState()
    {
        if (IsActive)
        {
            Root.Background = TryFindResource("SelectedBg") as Brush ?? Root.Background;
            Root.BorderBrush = TryFindResource("BorderBrush") as Brush;
            Root.BorderThickness = new Thickness(1);
            IconPath.Stroke = TryFindResource("TextPrimary") as Brush ?? IconPath.Stroke;
            IconPath.StrokeThickness = 1.5;
            return;
        }

        Root.BorderThickness = new Thickness(0);
        Root.Background = TryFindResource("Depth2") as Brush ?? Root.Background;
        if (IconData is not null)
            IconPath.Data = IconData;
        IconPath.Stroke = TryFindResource("TextSecondary") as Brush ?? IconPath.Stroke;
        IconPath.StrokeThickness = 1.5;
    }
}
