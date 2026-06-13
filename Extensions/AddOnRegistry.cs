using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Spur.Extensions.AddOns.Ai;
using Spur.Extensions.AddOns.Calculator;
using Spur.Extensions.AddOns.Color;
using Spur.Extensions.AddOns.Currency;
using Spur.Extensions.AddOns.Ip;
using Spur.Extensions.AddOns.KillProcess;
using Spur.Extensions.AddOns.PasswordGen;
using Spur.Extensions.AddOns.QuickNote;
using Spur.Extensions.AddOns.Screenshot;
using Spur.Extensions.AddOns.SettingsNav;
using Spur.Extensions.AddOns.Shell;
using Spur.Extensions.AddOns.SystemOps;
using Spur.Extensions.AddOns.Timer;
using Spur.Models;
using Spur.Services;

namespace Spur.Extensions;

public sealed class AddOnRegistry
{
    private readonly List<IAddOn> _builtIn = new();
    private readonly List<IAddOn> _community = new();
    private readonly ILogger _logger;
    private IReadOnlyList<IAddOn>? _allCache;

    public IReadOnlyList<IAddOn> All
    {
        get
        {
            if (_allCache is null)
                _allCache = _builtIn.Concat(_community).ToList();
            return _allCache;
        }
    }
    public IReadOnlyList<IAddOn> BuiltIn => _builtIn;
    public IReadOnlyList<IAddOn> Community => _community;

    public AddOnRegistry(ILogger logger)
    {
        _logger = logger;
        RegisterBuiltIn();
    }

    private void RegisterBuiltIn()
    {
        _builtIn.Add(new CalculatorAddOn { Settings = new CalculatorSettings() });
        _builtIn.Add(new TimerAddOn { Settings = new TimerSettings() });
        _builtIn.Add(new KillProcessAddOn { Settings = new KillProcessSettings() });
        _builtIn.Add(new PasswordGenAddOn { Settings = new PasswordGenSettings() });
        _builtIn.Add(new ScreenshotAddOn { Settings = new ScreenshotSettings() });
        _builtIn.Add(new QuickNoteAddOn { Settings = new QuickNoteSettings() });
        _builtIn.Add(new CurrencyAddOn { Settings = new CurrencySettings() });
        _builtIn.Add(new ColorAddOn { Settings = new ColorSettings() });
        _builtIn.Add(new IpAddOn { Settings = new IpSettings() });
        _builtIn.Add(new AiAddOn { Settings = new AiSettings() });
        _builtIn.Add(new ShellAddOn { Settings = new ShellSettings() });
        _builtIn.Add(new SystemAddOn { Settings = new SystemSettings() });
        _builtIn.Add(new SettingsNavAddOn { Settings = new SettingsNavSettings() });
        InvalidateCache();
    }

    public IAddOn? FindById(string id)
        => All.FirstOrDefault(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public IAddOn? FindByKeyword(string keyword)
        => All.FirstOrDefault(x => x.IsEnabled && !x.IsGlobal && string.Equals(x.Keyword, keyword, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<IAddOn> GetEnabled()
        => All.Where(x => x.IsEnabled);

    public IEnumerable<IAddOn> GetGlobalEnabled()
        => All.Where(x => x.IsEnabled && x.IsGlobal);

    public void LoadSettings(SpurConfig config)
    {
        foreach (var addOn in All)
        {
            if (config.AddOns.TryGetValue(addOn.Id, out var entry))
            {
                addOn.IsEnabled = entry.Enabled;
                if (!string.IsNullOrWhiteSpace(entry.Keyword))
                    addOn.Keyword = entry.Keyword;

                if (addOn.Settings != null)
                {
                    try
                    {
                        // A12: entry.Options is already a Dictionary<string,JsonElement>;
                        // re-serialising it to a string just to deserialise again wastes CPU.
                        // Deserialise directly from the JsonElement dictionary instead.
                        var json = JsonSerializer.SerializeToUtf8Bytes(entry.Options);
                        var loaded = JsonSerializer.Deserialize(json, addOn.Settings.GetType());
                        if (loaded != null) addOn.Settings = loaded;
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning($"Failed to load settings for add-on {addOn.Id}", ex);
                    }
                }
            }
        }
    }

    public void SaveSettings(SpurConfig config)
    {
        foreach (var addOn in All)
        {
            var options = new Dictionary<string, JsonElement>();
            if (addOn.Settings != null)
            {
                var json = JsonSerializer.Serialize(addOn.Settings);
                options = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? new();
            }

            config.AddOns[addOn.Id] = new AddOnEntryConfig
            {
                Enabled = addOn.IsEnabled,
                Keyword = addOn.Keyword,
                Options = options
            };
        }
    }

    private void InvalidateCache() => _allCache = null;

    public void LoadCommunityAddOns(string addOnsFolder)
    {
        // C2: Invalidate the cached All list whenever community add-ons are added
        // so the next access to All reflects the newly loaded add-ons.
        // (Implementation of actual dynamic loading is pending store logic.)
        InvalidateCache();
    }
}
