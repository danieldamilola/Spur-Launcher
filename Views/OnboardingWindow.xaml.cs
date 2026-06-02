using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Arc.Views;

public partial class OnboardingWindow : Window
{
    private int _slide;
    private readonly StackPanel[] _slides;
    private readonly Ellipse[] _dots;

    public event Action? OnCompleted;

    public OnboardingWindow()
    {
        InitializeComponent();
        _slides = [Slide1, Slide2, Slide3];
        _dots = [Dot1, Dot2, Dot3];
    }

    private void OnSkipClick(object sender, RoutedEventArgs e) => Finish();

    private void OnNextClick(object sender, RoutedEventArgs e)
    {
        if (_slide >= _slides.Length - 1)
        {
            Finish();
            return;
        }

        var current = _slides[_slide];
        _slide++;

        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(120))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        fadeOut.Completed += (_, _) => current.Visibility = Visibility.Collapsed;
        current.BeginAnimation(OpacityProperty, fadeOut);

        var next = _slides[_slide];
        next.Visibility = Visibility.Visible;
        next.Opacity = 0;
        next.BeginAnimation(OpacityProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });

        UpdateDots();
        NextButton.Content = _slide == _slides.Length - 1 ? "Finish" : "Continue";
    }

    private void UpdateDots()
    {
        var active = TryFindResource("TextPrimary") as Brush ?? Brushes.White;
        var idle = TryFindResource("BorderStrong") as Brush ?? Brushes.Gray;
        for (var i = 0; i < _dots.Length; i++)
            _dots[i].Fill = i == _slide ? active : idle;
    }

    private void Finish()
    {
        OnCompleted?.Invoke();
        Close();
    }

    private void OnDrag(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }
}
