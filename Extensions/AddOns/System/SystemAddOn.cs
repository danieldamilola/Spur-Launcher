using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Spur.Models;

namespace Spur.Extensions.AddOns.SystemOps;

public sealed class SystemAddOn : IAddOn
{
    public string Id => "system";
    public string Name => "System";
    public string Description => "Shutdown, restart, sleep, lock, sign out.";
    public string IconGlyph => "\ue7e8";
    public string? IconPath => "/Assets/Icons/system.png";
    public string Author => "Built-in";
    public string Version => "2.0.0";
    public bool IsBuiltIn => true;

    public bool IsEnabled { get; set; } = true;
    public string Keyword { get; set; } = "sys";
    public bool IsGlobal => false;

    public object? Settings { get; set; }
    public FrameworkElement? CreateSettingsView() => null;

    private static readonly Dictionary<string, (string display, string command, string args)> _commands = new()
    {
        ["shutdown"] = ("Shut Down",   "shutdown", "/s /t 0"),
        ["restart"]  = ("Restart",     "shutdown", "/r /t 0"),
        ["sleep"]    = ("Sleep",       "rundll32", "powrprof.dll,SetSuspendState 0,1,0"),
        ["lock"]     = ("Lock",        "rundll32", "user32.dll,LockWorkStation"),
        ["logout"]   = ("Sign Out",    "shutdown", "/l"),
    };

    public IEnumerable<SearchResult> GetResults(string subQuery)
    {
        var filter = subQuery.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(filter))
        {
            foreach (var (key, (display, _, _)) in _commands)
            {
                yield return new SearchResult
                {
                    Id         = $"system:{key}",
                    Type       = ResultType.Action,
                    Name       = display,
                    Subtitle   = $"system {key}",
                    IconGlyph  = IconGlyph,
                    IconPath   = IconPath,
                    ActionId   = Id,
                };
            }
            yield break;
        }

        foreach (var (key, (display, _, _)) in _commands)
        {
            if (key.StartsWith(filter, StringComparison.OrdinalIgnoreCase)
                || display.StartsWith(filter, StringComparison.OrdinalIgnoreCase))
            {
                yield return new SearchResult
                {
                    Id         = $"system:{key}",
                    Type       = ResultType.Action,
                    Name       = display,
                    Subtitle   = $"system {key}",
                    IconGlyph  = IconGlyph,
                    IconPath   = IconPath,
                    ActionId   = Id,
                };
            }
        }
    }

    public bool CanHandle(string query) => false;
    public SearchResult BuildResult(string query) => new();

    public Task<AddOnResult> ExecuteAsync(string input, CancellationToken ct = default)
    {
        var key = input.Trim().ToLowerInvariant();
        if (_commands.TryGetValue(key, out var cmd))
        {
            try
            {
                var psi = new ProcessStartInfo(cmd.command, cmd.args)
                {
                    UseShellExecute = true,
                    CreateNoWindow = true,
                };
                Process.Start(psi);
                
                return Task.FromResult(new AddOnResult
                {
                    Success = true,
                    Title = Name,
                    Detail = cmd.display,
                    PanelId = Id
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new AddOnResult
                {
                    Success = false,
                    Title = Name,
                    Detail = $"Error: {ex.Message}"
                });
            }
        }

        return Task.FromResult(new AddOnResult { Success = false });
    }
}
