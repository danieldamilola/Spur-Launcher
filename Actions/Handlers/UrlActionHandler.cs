using Spur.Models;
using System.Diagnostics;

namespace Spur.Actions.Handlers;

public sealed class UrlActionHandler : IActionHandler
{
    public event Action? HideRequested;

    public bool CanHandle(SearchResult result) =>
        result.ActionId == "url";

    public Task<ActionPanelState> ExecuteAsync(
        SearchResult result,
        string actionInput,
        CancellationToken ct = default)
    {
        var url = NormalizeUrl(actionInput.Trim());
        LaunchUrl(url);
        HideRequested?.Invoke();
        return Task.FromResult(ActionPanelState.Hidden());
    }

    private static string NormalizeUrl(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var trimmed = input.Trim();
        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return trimmed;

        return "https://" + trimmed;
    }

    private static void LaunchUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // Fail silently
        }
    }
}
