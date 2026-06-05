using System.Net;
using System.Net.Sockets;
using Spur.Models;

namespace Spur.Extensions;

public sealed class IpAction : IAction
{
    public string Id => "ip";
    public string Name => "IP Address";
    public string IconGlyph => "\ue701";
    public bool IsGlobal => false;

    public bool CanHandle(string query) =>
        !string.IsNullOrWhiteSpace(query) && query.Trim().Equals("ip", StringComparison.OrdinalIgnoreCase);

    public SearchResult BuildResult(string query)
    {
        var local = GetLocalIp();
        return new SearchResult
        {
            Id = "action:ip",
            Type = ResultType.Action,
            Name = "IP Address",
            Subtitle = $"Local: {local ?? "Not connected"}  ·  Public: fetching…",
            IconGlyph = "\ue701",
            ActionId = Id,
            Score = 700,
        };
    }

    public IEnumerable<SearchResult> GetResults(string subQuery)
    {
        var local = GetLocalIp();
        yield return new SearchResult
        {
            Id = "action:ip",
            Type = ResultType.Action,
            Name = "IP Address",
            Subtitle = $"Local: {local ?? "Not connected"}  ·  Public: fetching…",
            IconGlyph = "\ue701",
            ActionId = Id,
        };
    }

    public static string? GetLocalIp()
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);
            return (socket.LocalEndPoint as IPEndPoint)?.Address.ToString();
        }
        catch { return null; }
    }

    public static async Task<string?> GetPublicIpAsync()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            return (await client.GetStringAsync("https://api.ipify.org")).Trim();
        }
        catch { return null; }
    }
}
