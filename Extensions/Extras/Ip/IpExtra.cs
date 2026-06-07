using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Spur.Models;

namespace Spur.Extensions.Extras.Ip;

public sealed class IpExtra : IExtra
{
    public string Id => "ip";
    public string Name => "IP";
    public string Description => "Show local and public IP addresses.";
    public string IconGlyph => "\ue717";
    public string Author => "Built-in";
    public string Version => "2.0.0";
    public bool IsBuiltIn => true;

    public bool IsEnabled { get; set; } = true;
    public string Keyword { get; set; } = "ip";
    public bool IsGlobal => false;

    public object? Settings { get; set; }
    public FrameworkElement? CreateSettingsView() => null;

    public IEnumerable<SearchResult> GetResults(string subQuery)
    {
        yield return new SearchResult
        {
            Id         = "action:ip",
            Type       = ResultType.Action,
            Name       = "Get IP Addresses",
            Subtitle   = "Press ↵ to fetch local and public IPs",
            IconGlyph  = IconGlyph,
            ActionId   = Id,
        };
    }

    public bool CanHandle(string query) => false;
    public SearchResult BuildResult(string query) => new();

    public async Task<ExtraResult> ExecuteAsync(string input, CancellationToken ct = default)
    {
        var local = GetLocalIp();
        var pub = await GetPublicIpAsync(ct);
        
        var combined = $"{local} · {pub}";

        return new ExtraResult
        {
            Success = true,
            Title = Name,
            Detail = combined,
            CopyText = combined,
            PanelId = Id,
            SubText = "IP copied"
        };
    }

    private static string GetLocalIp()
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);
            return (socket.LocalEndPoint as IPEndPoint)?.Address.ToString() ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private static async Task<string> GetPublicIpAsync(CancellationToken ct)
    {
        try
        {
            using var http = new HttpClient();
            var response = await http.GetStringAsync("https://api.ipify.org", ct);
            return response.Trim();
        }
        catch
        {
            return "Unknown";
        }
    }
}
