using Spur.Extensions;
using Spur.Services;
using Spur.Models;

namespace Spur.ViewModels;

/// <summary>
/// Represents a single countdown timer instance with its own
/// label, duration, remaining time, and DispatcherTimer.
/// Implements INotifyPropertyChanged so the UI can bind to individual timer properties.
/// </summary>
public sealed class TimerInstance : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private string _label = string.Empty;
    private TimeSpan _remaining;
    private TimeSpan _totalDuration;
    private bool _isRunning;
    private bool _isPaused;
    private double _progress = 100;
    private string _displayTime = "00:00";
    private DispatcherTimer? _tick;

    /// <summary>User-visible label, e.g. "Timer 1".</summary>
    public string Label
    {
        get => _label;
        set { if (_label != value) { _label = value; OnPropertyChanged(nameof(Label)); } }
    }

    /// <summary>Time remaining on this timer.</summary>
    public TimeSpan Remaining
    {
        get => _remaining;
        set { if (_remaining != value) { _remaining = value; OnPropertyChanged(nameof(Remaining)); } }
    }

    /// <summary>The original duration this timer was set to.</summary>
    public TimeSpan TotalDuration
    {
        get => _totalDuration;
        set { if (_totalDuration != value) { _totalDuration = value; OnPropertyChanged(nameof(TotalDuration)); } }
    }

    /// <summary>Whether this timer is actively counting down.</summary>
    public bool IsRunning
    {
        get => _isRunning;
        set { if (_isRunning != value) { _isRunning = value; OnPropertyChanged(nameof(IsRunning)); } }
    }

    /// <summary>Whether this timer is paused (still active but not ticking).</summary>
    public bool IsPaused
    {
        get => _isPaused;
        set { if (_isPaused != value) { _isPaused = value; OnPropertyChanged(nameof(IsPaused)); } }
    }

    /// <summary>Progress percentage (100 = full, 0 = done).</summary>
    public double Progress
    {
        get => _progress;
        set { if (Math.Abs(_progress - value) > 0.001) { _progress = value; OnPropertyChanged(nameof(Progress)); } }
    }

    /// <summary>Formatted display string, e.g. "04:30" or "01:15:00".</summary>
    public string DisplayTime
    {
        get => _displayTime;
        set { if (_displayTime != value) { _displayTime = value; OnPropertyChanged(nameof(DisplayTime)); } }
    }

    /// <summary>Fired when this timer reaches zero. Carries the instance so the parent VM can remove it.</summary>
    public event Action<TimerInstance>? Completed;

    /// <summary>Start counting down.</summary>
    public void Start()
    {
        if (IsRunning && !IsPaused) return;

        IsRunning = true;
        IsPaused = false;
        _tick = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _tick.Tick += OnTick;
        _tick.Start();
    }

    /// <summary>Pause the countdown without removing the timer.</summary>
    public void Pause()
    {
        if (!IsRunning || IsPaused) return;
        _tick?.Stop();
        IsPaused = true;
    }

    /// <summary>Resume a paused timer.</summary>
    public void Resume()
    {
        if (!IsRunning || !IsPaused) return;
        IsPaused = false;
        _tick?.Start();
    }

    /// <summary>Stop and clean up the DispatcherTimer.</summary>
    public void StopTick()
    {
        _tick?.Stop();
        _tick = null;
        IsRunning = false;
        IsPaused = false;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        Remaining -= TimeSpan.FromMilliseconds(100);
        if (Remaining <= TimeSpan.Zero)
        {
            Remaining = TimeSpan.Zero;
            StopTick();
            UpdateDisplay();
            Progress = 0;
            Completed?.Invoke(this);
            return;
        }
        UpdateDisplay();
        Progress = TotalDuration > TimeSpan.Zero
            ? Remaining.TotalMilliseconds / TotalDuration.TotalMilliseconds * 100.0
            : 0;
    }

    /// <summary>Recalculate the formatted display time.</summary>
    public void UpdateDisplay()
    {
        DisplayTime = Remaining.TotalHours >= 1
            ? Remaining.ToString(@"hh\:mm\:ss")
            : Remaining.ToString(@"mm\:ss");
    }

    private void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

/// <summary>
/// ViewModel for the countdown timer feature. Supports up to 3
/// concurrent labeled timers. Each timer is a <see cref="TimerInstance"/>.
/// </summary>
public sealed partial class TimerViewModel : ObservableObject
{
    private const int MaxTimers = 3;

    private readonly INotificationService _notification;
    private int _timerCounter;

    // Staged values from the latest preview (before Start is pressed)
    private TimeSpan _stagedTotal;
    private TimeSpan _stagedRemaining;

    /// <summary>All currently active timers.</summary>
    public ObservableCollection<TimerInstance> ActiveTimers { get; } = new();

    // ── Backward-compatible properties for existing UI bindings ──
    [ObservableProperty] private string _timerDisplay = "00:00";
    [ObservableProperty] private string _timerStatus = "Choose a duration";
    [ObservableProperty] private double _timerProgress = 100;
    [ObservableProperty] private bool   _timerRunning = false;

    /// <summary>True when at least one timer is active.</summary>
    public bool HasActiveTimer => ActiveTimers.Count > 0;

    public TimerViewModel(INotificationService notification)
    {
        _notification = notification;
        StartCommand  = new RelayCommand(Start);
        CancelCommand = new RelayCommand(Cancel);

        ActiveTimers.CollectionChanged += (_, _) =>
            OnPropertyChanged(nameof(HasActiveTimer));
    }

    public IRelayCommand StartCommand  { get; }
    public IRelayCommand CancelCommand { get; }

    public bool StartTimerPreview(string query)
    {
        if (!Spur.Extensions.AddOns.Timer.TimerAddOn.TryParseDuration(query, out var durationSec))
        {
            _stagedTotal = TimeSpan.Zero;
            _stagedRemaining = TimeSpan.Zero;
            TimerDisplay = "00:00";
            TimerProgress = 100;
            TimerStatus = "Use a duration like 5m, 30s, or 1h";
            TimerRunning = false;
            return false;
        }
        var duration = TimeSpan.FromSeconds(durationSec);
        _stagedTotal     = duration;
        _stagedRemaining = duration;
        TimerProgress = 100;
        UpdateLegacyDisplay(duration);
        TimerStatus = "Ready to start";
        TimerRunning = false;
        return true;
    }

    private void Start()
    {
        if (_stagedTotal == TimeSpan.Zero)
        {
            TimerStatus = "Choose a duration first";
            return;
        }

        if (ActiveTimers.Count >= MaxTimers)
        {
            TimerStatus = $"Maximum of {MaxTimers} concurrent timers reached";
            return;
        }

        _timerCounter++;
        var instance = new TimerInstance
        {
            Label         = $"Timer {_timerCounter}",
            TotalDuration = _stagedTotal,
            Remaining     = _stagedTotal,
        };
        instance.UpdateDisplay();
        instance.Completed += OnTimerCompleted;

        ActiveTimers.Add(instance);
        instance.Start();

        // Update legacy properties to reflect the newest timer
        TimerRunning = true;
        TimerStatus = "Counting down";
        BindLegacyToInstance(instance);
    }

    /// <summary>
    /// Start a timer directly with a specific duration and label (used by action handler).
    /// Returns the created instance, or null if the max has been reached.
    /// </summary>
    public TimerInstance? StartTimer(TimeSpan duration, string? label = null)
    {
        if (ActiveTimers.Count >= MaxTimers)
        {
            TimerStatus = $"Maximum of {MaxTimers} concurrent timers reached";
            return null;
        }

        _timerCounter++;
        var instance = new TimerInstance
        {
            Label         = label ?? $"Timer {_timerCounter}",
            TotalDuration = duration,
            Remaining     = duration,
        };
        instance.UpdateDisplay();
        instance.Completed += OnTimerCompleted;

        ActiveTimers.Add(instance);
        instance.Start();

        TimerRunning = true;
        TimerStatus = "Counting down";
        BindLegacyToInstance(instance);
        return instance;
    }

    /// <summary>Cancel a specific timer instance.</summary>
    public void Cancel(TimerInstance instance)
    {
        instance.StopTick();
        instance.Completed -= OnTimerCompleted;
        instance.PropertyChanged -= OnBoundInstancePropertyChanged;
        ActiveTimers.Remove(instance);
        SyncLegacyAfterRemoval();
    }

    /// <summary>Pause a specific timer instance.</summary>
    public void Pause(TimerInstance instance)
    {
        instance.Pause();
    }

    /// <summary>Resume a specific timer instance.</summary>
    public void Resume(TimerInstance instance)
    {
        instance.Resume();
    }

    /// <summary>Cancel the most recent timer (legacy button binding).</summary>
    private void Cancel()
    {
        if (ActiveTimers.Count > 0)
        {
            var last = ActiveTimers[^1];
            Cancel(last);
        }
        else
        {
            TimerStatus = "No active timer";
        }
    }

    private void OnTimerCompleted(TimerInstance instance)
    {
        _notification.Show("Spur Timer", $"{instance.Label} has finished!");
        instance.Completed -= OnTimerCompleted;
        instance.PropertyChanged -= OnBoundInstancePropertyChanged;
        ActiveTimers.Remove(instance);
        SyncLegacyAfterRemoval();
    }

    /// <summary>Stop all timers (called on app shutdown / panel close).</summary>
    public void Stop()
    {
        foreach (var t in ActiveTimers.ToList())
        {
            t.StopTick();
            t.Completed -= OnTimerCompleted;
            t.PropertyChanged -= OnBoundInstancePropertyChanged;
        }
        ActiveTimers.Clear();
        TimerRunning = false;
    }

    // ── Legacy helpers ──────────────────────────────────────────

    /// <summary>
    /// Wire the legacy display properties to mirror a specific instance.
    /// Uses a PropertyChanged subscription so progress/display stay in sync.
    /// </summary>
    private void BindLegacyToInstance(TimerInstance instance)
    {
        // Detach from any previously-bound instance to prevent leak
        foreach (var t in ActiveTimers)
            t.PropertyChanged -= OnBoundInstancePropertyChanged;

        // Directly show current values
        TimerDisplay  = instance.DisplayTime;
        TimerProgress = instance.Progress;

        instance.PropertyChanged += OnBoundInstancePropertyChanged;
    }

    private void OnBoundInstancePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not TimerInstance inst) return;
        // Only mirror the most recently added timer
        if (ActiveTimers.Count == 0 || ActiveTimers[^1] != inst) return;

        switch (e.PropertyName)
        {
            case nameof(TimerInstance.DisplayTime):
                TimerDisplay = inst.DisplayTime;
                break;
            case nameof(TimerInstance.Progress):
                TimerProgress = inst.Progress;
                break;
        }
    }

    private void SyncLegacyAfterRemoval()
    {
        if (ActiveTimers.Count > 0)
        {
            var latest = ActiveTimers[^1];
            TimerDisplay  = latest.DisplayTime;
            TimerProgress = latest.Progress;
            TimerRunning  = true;
            TimerStatus   = latest.IsPaused ? "Paused" : "Counting down";
        }
        else
        {
            TimerRunning  = false;
            TimerDisplay  = "00:00";
            TimerProgress = 100;
            TimerStatus   = "Finished";
        }
    }

    private void UpdateLegacyDisplay(TimeSpan remaining)
    {
        TimerDisplay = remaining.TotalHours >= 1
            ? remaining.ToString(@"hh\:mm\:ss")
            : remaining.ToString(@"mm\:ss");
    }
}
