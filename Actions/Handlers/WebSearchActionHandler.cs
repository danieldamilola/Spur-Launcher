using Spur.Models;
using System.Diagnostics;

namespace Spur.Actions.Handlers;

public sealed class WebSearchActionHandler : IActionHandler
{
    public event Action? HideRequested;

    public bool CanHandle(SearchResult result) =>
        result.ActionId == "web";

    public Task<ActionPanelState> ExecuteAsync(
        SearchResult result,
        string actionInput,
        CancellationToken ct = default)
    {
        var query = NormalizeWebQuery(actionInput);
        var url = $"https://www.google.com/search?q={Uri.EscapeDataString(query)}";
        LaunchUrl(url);
        HideRequested?.Invoke();
        return Task.FromResult(ActionPanelState.Hidden());
    }

    private static string NormalizeWebQuery(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        return input.Trim();
    }

    private static void LaunchUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // Intentional: Process.Start may fail for invalid or unsupported URLs
        }
    }
}
