using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Spur.Models;
using Spur.Services;

namespace Spur.Views;

public partial class OnboardingWindow : Window
{
    private int _slide;
    private readonly StackPanel[] _slides;
    private readonly Ellipse[] _dots;
    private readonly SpurConfig _config;
    private readonly IConfigService _configService;
    private bool _recordingShortcut;

    public event Action? OnCompleted;

    public OnboardingWindow(SpurConfig config, IConfigService configService)
    {
        _config = config;
        _configService = configService;

        InitializeComponent();
        _slides = [Slide1, Slide2, Slide3, Slide4];
        _dots   = [Dot1, Dot2, Dot3, Dot4];

        KeyCaptureText.Text = _config.Shortcut.Replace("+", " + ");
        UpdateThemeCards();
    }

    // ── Navigation ─────────────────────────────────────────────

    private void OnSkipClick(object sender, RoutedEventArgs e) => Finish();

    private void OnNextClick(object sender, RoutedEventArgs e)
    {
        if (_slide >= _slides.Length - 1)
        {
            Finish();
            return;
        }

        if (_slide == 2)
        {
            var themeName = _config.Theme switch
            {
                "light" => "Light",
                "system" => "System",
                _ => "Dark"
            };
            ReadySummary.Text =
                $"Shortcut: {_config.Shortcut.Replace("+", " + ")}\n" +
                $"Theme: {themeName}\n\n" +
                "You can change both anytime in Settings.";
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
        _configService.Save(_config);
        OnCompleted?.Invoke();
        Close();
    }

    private void OnDrag(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    // ── Hotkey capture (Slide 2) ───────────────────────────────

    private void OnKeyCaptureClick(object sender, MouseButtonEventArgs e) => StartRecording();
    private void OnKeyCaptureGotFocus(object sender, RoutedEventArgs e) => StartRecording();

    private void StartRecording()
    {
        if (_recordingShortcut) return;
        _recordingShortcut = true;
        KeyCaptureHint.Text = "Press any key combination…";
        KeyCaptureBorder.BorderBrush = TryFindResource("Accent") as Brush ?? Brushes.DodgerBlue;
        Keyboard.Focus(KeyCaptureBorder);
    }

    private void OnKeyCaptureDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (key is Key.LeftCtrl or Key.RightCtrl
                   or Key.LeftAlt or Key.RightAlt
                   or Key.LeftShift or Key.RightShift
                   or Key.LWin or Key.RWin)
            return;

        var mods = Keyboard.Modifiers;
        var parts = new System.Collections.Generic.List<string>();
        if ((mods & ModifierKeys.Control)  != 0) parts.Add("Ctrl");
        if ((mods & ModifierKeys.Alt)      != 0) parts.Add("Alt");
        if ((mods & ModifierKeys.Shift)    != 0) parts.Add("Shift");
        if ((mods & ModifierKeys.Windows)  != 0) parts.Add("Win");
        parts.Add(key.ToString());

        var shortcut = string.Join("+", parts);
        _config.Shortcut = shortcut;
        KeyCaptureText.Text = shortcut.Replace("+", " + ");

        StopRecording();
    }

    private void OnKeyCaptureLostFocus(object sender, RoutedEventArgs e) => StopRecording();

    private void StopRecording()
    {
        if (!_recordingShortcut) return;
        _recordingShortcut = false;
        KeyCaptureHint.Text = "Click to change";
        KeyCaptureBorder.BorderBrush = TryFindResource("BorderStrong") as Brush ?? Brushes.Gray;
    }

    // ── Theme selection (Slide 3) ──────────────────────────────

    private void OnThemeDarkClick(object sender, MouseButtonEventArgs e)  => SelectTheme("dark");
    private void OnThemeLightClick(object sender, MouseButtonEventArgs e)  => SelectTheme("light");
    private void OnThemeSystemClick(object sender, MouseButtonEventArgs e) => SelectTheme("system");

    private void SelectTheme(string theme)
    {
        _config.Theme = theme;
        UpdateThemeCards();
    }

    private void UpdateThemeCards()
    {
        var accent = TryFindResource("Accent") as Brush ?? Brushes.DodgerBlue;

        ThemeDarkCard.BorderBrush   = _config.Theme == "dark"   ? accent : Brushes.Transparent;
        ThemeLightCard.BorderBrush  = _config.Theme == "light"  ? accent : Brushes.Transparent;
        ThemeSystemCard.BorderBrush = _config.Theme == "system" ? accent : Brushes.Transparent;
    }
}

