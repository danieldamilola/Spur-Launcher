using Spur.Extensions;
using Spur.Services;
using Spur.Models;

namespace Spur.ViewModels;

public sealed partial class TimerViewModel : ObservableObject
{
    private readonly INotificationService _notification;

    [ObservableProperty] private string _timerDisplay = "00:00";
    [ObservableProperty] private double _timerProgress = 100;
    [ObservableProperty] private bool   _timerRunning = false;

    private TimeSpan _timerRemaining;
    private TimeSpan _timerTotal;
    private DispatcherTimer? _timerTick;

    public TimerViewModel(INotificationService notification)
    {
        _notification = notification;
        StartCommand  = new RelayCommand(Start);
        CancelCommand = new RelayCommand(Cancel);
    }

    public IRelayCommand StartCommand  { get; }
    public IRelayCommand CancelCommand { get; }

    public void StartTimerPreview(string query)
    {
        if (!TimerAction.TryParse(query, out var duration)) return;
        _timerTotal     = duration;
        _timerRemaining = duration;
        UpdateTimerDisplay();
        TimerRunning = false;
    }

    private void Start()
    {
        if (TimerRunning || _timerTotal == TimeSpan.Zero) return;
        _timerRemaining = _timerTotal;
        TimerRunning    = true;

        _timerTick = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _timerTick.Tick += OnTimerTick;
        _timerTick.Start();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        _timerRemaining -= TimeSpan.FromMilliseconds(100);
        if (_timerRemaining <= TimeSpan.Zero)
        {
            _timerRemaining = TimeSpan.Zero;
            _timerTick?.Stop();
            TimerRunning = false;
            _notification.Show("Spur Timer", "Your timer has finished!");
        }
        UpdateTimerDisplay();
    }

    private void UpdateTimerDisplay()
    {
        TimerDisplay = _timerRemaining.TotalHours >= 1
            ? _timerRemaining.ToString(@"hh\:mm\:ss")
            : _timerRemaining.ToString(@"mm\:ss");

        TimerProgress = _timerTotal > TimeSpan.Zero
            ? _timerRemaining.TotalMilliseconds / _timerTotal.TotalMilliseconds * 100.0
            : 0;
    }

    private void Cancel()
    {
        _timerTick?.Stop();
        _timerTick  = null;
        TimerRunning = false;
    }

    public void Stop()
    {
        _timerTick?.Stop();
        _timerTick  = null;
        TimerRunning = false;
    }
}
