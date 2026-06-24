using System.ComponentModel;

namespace Spur.Extensions.AddOns.Timer;

public sealed class TimerSettings : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private string _defaultPresets = "1m,3m,5m,10m,15m,30m";
    public string DefaultPresets
    {
        get => _defaultPresets;
        set
        {
            if (_defaultPresets == value) return;
            _defaultPresets = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DefaultPresets)));
        }
    }
}
