using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Spur.Extensions.Extras.Ai;
using Spur.Extensions.Extras.Calculator;
using Spur.Extensions.Extras.Color;
using Spur.Extensions.Extras.Currency;
using Spur.Extensions.Extras.Ip;
using Spur.Extensions.Extras.KillProcess;
using Spur.Extensions.Extras.PasswordGen;
using Spur.Extensions.Extras.QuickNote;
using Spur.Extensions.Extras.Screenshot;
using Spur.Extensions.Extras.SettingsNav;
using Spur.Extensions.Extras.Shell;
using Spur.Extensions.Extras.SystemOps;
using Spur.Extensions.Extras.Timer;
using Spur.Models;
using Spur.Services;

namespace Spur.Extensions;

public sealed class ExtrasRegistry
{
    private readonly List<IExtra> _builtIn = new();
    private readonly List<IExtra> _community = new();
    private readonly ILogger _logger;
    private IReadOnlyList<IExtra>? _allCache;

    public IReadOnlyList<IExtra> All
    {
        get
        {
            if (_allCache is null)
                _allCache = _builtIn.Concat(_community).ToList();
            return _allCache;
        }
    }
    public IReadOnlyList<IExtra> BuiltIn => _builtIn;
    public IReadOnlyList<IExtra> Community => _community;

    public ExtrasRegistry(ILogger logger)
    {
        _logger = logger;
        RegisterBuiltIn();
    }

    private void RegisterBuiltIn()
    {
        _builtIn.Add(new CalculatorExtra { Settings = new CalculatorSettings() });
        _builtIn.Add(new TimerExtra { Settings = new TimerSettings() });
        _builtIn.Add(new KillProcessExtra { Settings = new KillProcessSettings() });
        _builtIn.Add(new PasswordGenExtra { Settings = new PasswordGenSettings() });
        _builtIn.Add(new ScreenshotExtra { Settings = new ScreenshotSettings() });
        _builtIn.Add(new QuickNoteExtra { Settings = new QuickNoteSettings() });
        _builtIn.Add(new CurrencyExtra { Settings = new CurrencySettings() });
        _builtIn.Add(new ColorExtra { Settings = new ColorSettings() });
        _builtIn.Add(new IpExtra { Settings = new IpSettings() });
        _builtIn.Add(new AiExtra { Settings = new AiSettings() });
        _builtIn.Add(new ShellExtra { Settings = new ShellSettings() });
        _builtIn.Add(new SystemExtra { Settings = new SystemSettings() });
        _builtIn.Add(new SettingsNavExtra { Settings = new SettingsNavSettings() });
        InvalidateCache();
    }

    public IExtra? FindById(string id)
        => All.FirstOrDefault(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public IExtra? FindByKeyword(string keyword)
        => All.FirstOrDefault(x => x.IsEnabled && !x.IsGlobal && string.Equals(x.Keyword, keyword, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<IExtra> GetEnabled()
        => All.Where(x => x.IsEnabled);

    public IEnumerable<IExtra> GetGlobalEnabled()
        => All.Where(x => x.IsEnabled && x.IsGlobal);

    public void LoadSettings(SpurConfig config)
    {
        foreach (var extra in All)
        {
            if (config.Extras.TryGetValue(extra.Id, out var entry))
            {
                extra.IsEnabled = entry.Enabled;
                if (!string.IsNullOrWhiteSpace(entry.Keyword))
                    extra.Keyword = entry.Keyword;

                if (extra.Settings != null)
                {
                    try
                    {
                        var json = JsonSerializer.Serialize(entry.Options);
                        var loaded = JsonSerializer.Deserialize(json, extra.Settings.GetType());
                        if (loaded != null) extra.Settings = loaded;
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning($"Failed to load settings for extra {extra.Id}", ex);
                    }
                }
            }
        }
    }

    public void SaveSettings(SpurConfig config)
    {
        foreach (var extra in All)
        {
            var options = new Dictionary<string, JsonElement>();
            if (extra.Settings != null)
            {
                var json = JsonSerializer.Serialize(extra.Settings);
                options = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? new();
            }

            config.Extras[extra.Id] = new ExtrasEntryConfig
            {
                Enabled = extra.IsEnabled,
                Keyword = extra.Keyword,
                Options = options
            };
        }
    }

    private void InvalidateCache() => _allCache = null;

    public void LoadCommunityExtras(string extrasFolder)
    {
        // To be implemented as part of Store logic
    }
}
