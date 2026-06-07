using Spur.Extensions;
using Spur.Services;
using Spur.Models;

namespace Spur.ViewModels;

public sealed partial class TimerViewModel : ObservableObject
{
    private readonly INotificationService _notification;

    [ObservableProperty] private string _timerDisplay = "00:00";
    [ObservableProperty] private string _timerStatus = "Choose a duration";
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

    public bool StartTimerPreview(string query)
    {
        if (!Spur.Extensions.Extras.Timer.TimerExtra.TryParseDuration(query, out var durationSec))
        {
            _timerTotal = TimeSpan.Zero;
            _timerRemaining = TimeSpan.Zero;
            TimerDisplay = "00:00";
            TimerProgress = 0;
            TimerStatus = "Use a duration like 5m, 30s, or 1h";
            TimerRunning = false;
            return false;
        }
        var duration = TimeSpan.FromSeconds(durationSec);
        _timerTotal     = duration;
        _timerRemaining = duration;
        UpdateTimerDisplay();
        TimerStatus = "Ready to start";
        TimerRunning = false;
        return true;
    }

    private void Start()
    {
        if (TimerRunning || _timerTotal == TimeSpan.Zero) return;
        _timerRemaining = _timerTotal;
        TimerRunning    = true;
        TimerStatus = "Counting down";

        _timerTick = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _timerTick.Tick += OnTimerTick;
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
            TimerStatus = "Finished";
            _notification.Show("Spur Timer", "Your timer has finished!");
        }
        UpdateTimerDisplay();

        TimerProgress = _timerTotal > TimeSpan.Zero
            ? _timerRemaining.TotalMilliseconds / _timerTotal.TotalMilliseconds * 100.0
            : 0;
    }

    private void UpdateTimerDisplay()
    {
        TimerDisplay = _timerRemaining.TotalHours >= 1
            ? _timerRemaining.ToString(@"hh\:mm\:ss")
            : _timerRemaining.ToString(@"mm\:ss");
    }

    private void Cancel()
    {
        _timerTick?.Stop();
        _timerTick  = null;
        TimerRunning = false;
        TimerStatus = "Canceled";
    }

    public void Stop()
    {
        _timerTick?.Stop();
        _timerTick  = null;
        TimerRunning = false;
    }
}
