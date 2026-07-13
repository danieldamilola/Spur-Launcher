using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Spur.Behaviors;

public static class SmoothScrollBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(SmoothScrollBehavior),
            new FrameworkPropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    private static readonly Dictionary<ScrollViewer, ScrollState> _states = new();
    private static bool _subscribed;
    private static bool _anyActive;

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue)
        {
            if (d is ScrollViewer sv)
                Attach(sv);
            else if (d is FrameworkElement fe)
            {
                fe.Loaded += OnLoaded;
                fe.Unloaded += OnUnloaded;
            }
        }
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        var fe = (FrameworkElement)sender;
        var sv = FindScrollViewer(fe);
        if (sv != null) Attach(sv);
    }

    private static void OnUnloaded(object sender, RoutedEventArgs e)
    {
        var fe = (FrameworkElement)sender;
        var sv = FindScrollViewer(fe);
        if (sv != null) Detach(sv);
    }

    private static void Attach(ScrollViewer sv)
    {
        if (!_states.TryAdd(sv, new ScrollState())) return;
        sv.PreviewMouseWheel += OnMouseWheel;
        sv.Unloaded += OnScrollViewerUnloaded;
    }

    private static void Detach(ScrollViewer sv)
    {
        sv.PreviewMouseWheel -= OnMouseWheel;
        sv.Unloaded -= OnScrollViewerUnloaded;
        _states.Remove(sv);
        UnsubscribeIfEmpty();
    }

    private static void OnScrollViewerUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is ScrollViewer sv) Detach(sv);
    }

    private static void EnsureRenderingActive()
    {
        if (_subscribed) return;
        CompositionTarget.Rendering += OnRendering;
        _subscribed = true;
    }

    private static void UnsubscribeIfEmpty()
    {
        if (!_subscribed) return;
        if (_states.Count > 0 && _anyActive) return;
        if (_states.Count > 0) return; // still have states, but all idle — keep subscribed for fast re-trigger
        CompositionTarget.Rendering -= OnRendering;
        _subscribed = false;
    }

    private static void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!_states.TryGetValue((ScrollViewer)sender, out var state)) return;
        state.Velocity -= e.Delta * 0.5;
        if (!_subscribed)
            EnsureRenderingActive();
        e.Handled = true;
    }

    private static void OnRendering(object? sender, EventArgs e)
    {
        _anyActive = false;
        foreach (var (sv, state) in _states)
        {
            if (Math.Abs(state.Velocity) < 0.5 || !sv.IsLoaded || !sv.IsVisible)
            {
                state.Velocity = 0;
                continue;
            }

            _anyActive = true;
            state.Velocity *= 0.92;
            var offset = Math.Clamp(sv.VerticalOffset + state.Velocity, 0, sv.ScrollableHeight);

            if (sv.ScrollableHeight > 0 && (offset <= 0 || offset >= sv.ScrollableHeight))
                state.Velocity = 0;

            sv.ScrollToVerticalOffset(offset);
        }

        if (!_anyActive && _subscribed)
        {
            CompositionTarget.Rendering -= OnRendering;
            _subscribed = false;
        }
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject root)
    {
        var stack = new Stack<DependencyObject>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current is ScrollViewer sv) return sv;
            var count = VisualTreeHelper.GetChildrenCount(current);
            for (var i = 0; i < count; i++)
                stack.Push(VisualTreeHelper.GetChild(current, i));
        }
        return null;
    }

    private sealed class ScrollState
    {
        public double Velocity;
    }
}
