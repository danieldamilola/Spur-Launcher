namespace Spur.ViewModels;

/// <summary>
/// Groups all transient state for the action-preview panel
/// (calculator result, AI streaming text, colour swatch, etc.).
/// Previously scattered as individual backing fields on <see cref="MainViewModel"/>.
/// Implement <see cref="CommunityToolkit.Mvvm.ComponentModel.ObservableObject"/> so
/// individual properties can fire change notifications when bound directly in XAML.
/// </summary>
public sealed class ActionPreviewState : ObservableObject
{
    private string _title = string.Empty;
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    private string _subtitle = string.Empty;
    public string Subtitle
    {
        get => _subtitle;
        set => SetProperty(ref _subtitle, value);
    }

    private string _state = string.Empty;
    /// <summary>Arbitrary state string passed to the action panel (e.g. hex colour value).</summary>
    public string State
    {
        get => _state;
        set => SetProperty(ref _state, value);
    }

    private string _resultText = string.Empty;
    public string ResultText
    {
        get => _resultText;
        set => SetProperty(ref _resultText, value);
    }

    private string _resultSubText = string.Empty;
    public string ResultSubText
    {
        get => _resultSubText;
        set => SetProperty(ref _resultSubText, value);
    }

    private string _query = string.Empty;
    /// <summary>The original query that triggered the action (shown in calc/system previews).</summary>
    public string Query
    {
        get => _query;
        set => SetProperty(ref _query, value);
    }

    /// <summary>Reset all fields to their defaults (called when a panel is dismissed).</summary>
    public void Clear()
    {
        Title      = string.Empty;
        Subtitle   = string.Empty;
        State      = string.Empty;
        ResultText = string.Empty;
        ResultSubText = string.Empty;
        Query      = string.Empty;
    }
}
