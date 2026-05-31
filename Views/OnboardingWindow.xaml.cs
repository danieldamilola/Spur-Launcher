using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Arc.Views;

public partial class OnboardingWindow : Window
{
    private int _slide = 0;
    private readonly StackPanel[] _slides;
    private readonly Ellipse[] _dots;

    public event Action? OnCompleted;

    public OnboardingWindow()
    {
        InitializeComponent();

        _slides = new[] { Slide1, Slide2, Slide3 };
        _dots   = new[] { Dot1, Dot2, Dot3 };

        _slides[0].Opacity = 1;
    }

    private void OnNextClick(object sender, RoutedEventArgs e)
    {
        var current = _slides[_slide];

        _slide++;

        if (_slide >= _slides.Length)
        {
            OnCompleted?.Invoke();
            Close();
            return;
        }

        // Animate current slide out
        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        fadeOut.Completed += (_, _) => current.Visibility = Visibility.Collapsed;
        current.BeginAnimation(OpacityProperty, fadeOut);

        // Show and animate next slide in
        var next = _slides[_slide];
        next.Visibility = Visibility.Visible;
        next.Opacity = 0;

        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            BeginTime = TimeSpan.FromMilliseconds(100)
        };
        next.BeginAnimation(OpacityProperty, fadeIn);

        var slideUp = new DoubleAnimation(12, 0, TimeSpan.FromMilliseconds(300))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            BeginTime = TimeSpan.FromMilliseconds(100)
        };
        var transform = (TranslateTransform)next.RenderTransform;
        transform.BeginAnimation(TranslateTransform.YProperty, slideUp);

        // Update dots
        for (int i = 0; i < _dots.Length; i++)
        {
            _dots[i].Fill = i == _slide
                ? (Brush)TryFindResource("Accent") ?? Brushes.Gray
                : (Brush)TryFindResource("BorderStrong") ?? Brushes.DimGray;
        }

        // Update button text for last slide
        NextButton.Content = _slide == _slides.Length - 1 ? "Launch Arc" : "Continue";
    }

    private void OnDrag(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }
}
