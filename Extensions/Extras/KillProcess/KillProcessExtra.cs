using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Spur.Models;

namespace Spur.Extensions.Extras.KillProcess;

public sealed class KillProcessExtra : IExtra
{
    public string Id => "kill";
    public string Name => "Kill";
    public string Description => "Find and terminate running processes by name.";
    public string IconGlyph => "\ue711";
    public string Author => "Built-in";
    public string Version => "2.0.0";
    public bool IsBuiltIn => true;

    public bool IsEnabled { get; set; } = true;
    public string Keyword { get; set; } = "kill";
    public bool IsGlobal => false;

    public object? Settings { get; set; }
    public FrameworkElement? CreateSettingsView()
    {
        return new KillProcessSettingsView { DataContext = Settings };
    }

    private static readonly Regex _trigger = new(
        @"^kill\s+(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public IEnumerable<SearchResult> GetResults(string subQuery)
    {
        var name = subQuery.Trim();
        var settings = (Settings as KillProcessSettings) ?? new KillProcessSettings();

        if (string.IsNullOrWhiteSpace(name))
        {
            var procs = Process.GetProcesses()
                .OrderByDescending(p => settings.PrioritizeVisibleWindows && HasVisibleWindow(p))
                .ThenByDescending(p =>
                {
                    try { return p.WorkingSet64; }
                    catch { return 0; }
                })
                .Take(20);

            foreach (var p in procs)
            {
                string? pname;
                try { pname = p.ProcessName; } catch { continue; }
                var mb = 0L;
                try { mb = p.WorkingSet64 / 1024 / 1024; } catch { }
                var title = GetWindowTitle(p);
                string? iconPath = null;
                try { iconPath = p.MainModule?.FileName; } catch { }
                yield return new SearchResult
                {
                    Id         = $"kill:{pname.ToLowerInvariant()}",
                    Type       = ResultType.Action,
                    Name       = settings.ShowWindowTitles && !string.IsNullOrWhiteSpace(title) ? $"{pname} - {title}" : pname,
                    Subtitle   = $"{mb} MB  ·  PID {p.Id}",
                    IconGlyph  = string.IsNullOrEmpty(iconPath) ? IconGlyph : null,
                    IconPath   = iconPath,
                    ActionId   = Id,
                };
            }
            yield break;
        }

        var matches = Process.GetProcesses()
            .Where(p =>
            {
                try { return p.ProcessName.StartsWith(name, StringComparison.OrdinalIgnoreCase); }
                catch { return false; }
            })
            .OrderByDescending(p => settings.PrioritizeVisibleWindows && HasVisibleWindow(p))
            .ToList();

        if (matches.Count == 0)
        {
            yield return new SearchResult
            {
                Id         = $"action:kill:{name.ToLowerInvariant()}",
                Type       = ResultType.Action,
                Name       = $"Kill \"{name}\" · not found",
                Subtitle   = "No matching process running",
                IconGlyph  = IconGlyph,
                ActionId   = Id,
            };
            yield break;
        }

        foreach (var p in matches.Take(10))
        {
            var mb = 0L;
            try { mb = p.WorkingSet64 / 1024 / 1024; } catch { }
            var title = GetWindowTitle(p);
            var label = settings.ShowWindowTitles && !string.IsNullOrWhiteSpace(title)
                ? $"{p.ProcessName} - {title}"
                : p.ProcessName;
                
            string? iconPath = null;
            try { iconPath = p.MainModule?.FileName; } catch { }
                
            yield return new SearchResult
            {
                Id         = $"kill:{p.ProcessName.ToLowerInvariant()}:{p.Id}",
                Type       = ResultType.Action,
                Name       = $"Kill {label}",
                Subtitle   = $"{mb} MB  ·  PID {p.Id}  ·  ↵ to kill",
                IconGlyph  = string.IsNullOrEmpty(iconPath) ? IconGlyph : null,
                IconPath   = iconPath,
                ActionId   = Id,
            };
        }
    }

    private static bool HasVisibleWindow(Process process)
    {
        try { return process.MainWindowHandle != IntPtr.Zero; }
        catch { return false; }
    }

    private static string GetWindowTitle(Process process)
    {
        try { return process.MainWindowTitle; }
        catch { return string.Empty; }
    }

    public bool CanHandle(string query) => false;

    public SearchResult BuildResult(string query) => new();

    private static string? ExtractName(string query)
    {
        var trimmed = query.Trim();
        var m = _trigger.Match(trimmed);
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    public Task<ExtraResult> ExecuteAsync(string input, CancellationToken ct = default)
    {
        var name = ExtractName(input) ?? input.Trim();
        if (string.IsNullOrWhiteSpace(name)) 
            return Task.FromResult(new ExtraResult { Success = false });

        var procs = Process.GetProcessesByName(name);
        var killed = 0;
        foreach (var p in procs)
        {
            try
            {
                p.Kill();
                p.Dispose();
                killed++;
            }
            catch { }
        }

        return Task.FromResult(new ExtraResult
        {
            Success = killed > 0,
            Title = Name,
            Detail = input,
            PanelId = Id,
            SubText = $"Killed {killed} process(es)"
        });
    }
}
