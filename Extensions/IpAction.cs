using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Volt.Extensions;

/// <summary>IP action. Triggered by typing exactly "ip".</summary>
public sealed class IpAction : IAction
{
    public string Id => "ip";

    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(5) };

    public bool CanHandle(string query) =>
        string.Equals(query.Trim(), "ip", StringComparison.OrdinalIgnoreCase);

    public SearchResult BuildResult(string query) => new()
    {
        Id       = "action:ip",
        Type     = ResultType.Action,
        Name     = "IP Address",
        Subtitle = "Local and public IP",
        LucideIcon = "wifi",
        ActionId = Id,
    };

    /// <summary>Returns the first active non-loopback IPv4 address.</summary>
    public static string? GetLocalIp()
    {
        try
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up)
                .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                .FirstOrDefault(a =>
                    a.Address.AddressFamily == AddressFamily.InterNetwork &&
                    !System.Net.IPAddress.IsLoopback(a.Address))
                ?.Address.ToString();
        }
        catch { return null; }
    }

    /// <summary>Fetches the public IP from ipify.org.</summary>
    public static async Task<string?> GetPublicIpAsync(CancellationToken ct = default)
    {
        try
        {
            var ip = await _http.GetStringAsync("https://api.ipify.org", ct);
            return ip.Trim();
        }
        catch { return null; }
    }
}
