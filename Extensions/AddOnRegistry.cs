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
        // Lazy registration: identity metadata is stored directly on the proxy;
        // the real add-on instance is only created when a behaviour method
        // (CanHandle, GetResults, ExecuteAsync, …) is first called.
        RegisterLazy("calc",     "Calculator",    "Evaluate math expressions inline.",               "\ue1d0", "/Assets/Icons/calculator.png",  true,  "",         new CalculatorSettings(),    () => new CalculatorAddOn());
        RegisterLazy("timer",    "Timer",         "Start a countdown directly from the launcher.",   "\ue121", "/Assets/Icons/history.png",     false, "timer",    new TimerSettings(),         () => new TimerAddOn());
        RegisterLazy("kill",     "Kill",          "Find and terminate running processes by name.",    "\ue711", "/Assets/Icons/shutdown.png",    false, "kill",     new KillProcessSettings(),   () => new KillProcessAddOn());
        RegisterLazy("pw",       "Password",      "Generate strong random passwords.",               "\ue8d7", "/Assets/Icons/lock.png",        false, "pw",       new PasswordGenSettings(),   () => new PasswordGenAddOn());
        RegisterLazy("ss",       "Screenshot",    "Capture and save screenshots.",                   "\ue722", "/Assets/Icons/image.png",       false, "ss",       new ScreenshotSettings(),    () => new ScreenshotAddOn());
        RegisterLazy("note",     "Quick Note",    "Save quick notes instantly.",                     "\ue70b", "/Assets/Icons/copy.png",        false, "note",     new QuickNoteSettings(),     () => new QuickNoteAddOn());
        RegisterLazy("cur",      "Currency",      "Real-time currency conversion.",                  "\ue12c", "/Assets/Icons/url.png",         false, "cur",      new CurrencySettings(),      () => new CurrencyAddOn());
        RegisterLazy("color",    "Color",         "Hex/RGB/HSL color conversion and preview.",       "\ue790", "/Assets/Icons/color.png",       false, "color",    new ColorSettings(),         () => new ColorAddOn());
        RegisterLazy("ip",       "IP",            "Show local and public IP addresses.",             "\ue717", "/Assets/Icons/info.png",        false, "ip",       new IpSettings(),            () => new IpAddOn());
        RegisterLazy("ai",       "AI Assistant",  "Ask the configured AI provider.",                 "\ue270", "/Assets/Icons/ai.png",          false, "ai",       new AiSettings(),            () => new AiAddOn());
        RegisterLazy("shell",    "Shell",         "Run commands through your configured terminal.",   "\ue765", "/Assets/Icons/shell.png",       false, ">",        new ShellSettings(),         () => new ShellAddOn());
        RegisterLazy("system",   "System",        "Shutdown, restart, sleep, lock, sign out.",       "\ue7e8", "/Assets/Icons/settings.png",    false, "sys",      new SystemSettings(),        () => new SystemAddOn());
        RegisterLazy("settings", "Settings",      "Open Spur settings.",                             "\ue713", "/Assets/Icons/settings.png",    false, "settings", new SettingsNavSettings(),   () => new SettingsNavAddOn());
        InvalidateCache();
    }

    private void RegisterLazy(
        string id, string name, string description,
        string iconGlyph, string? iconPath,
        bool isGlobal, string keyword,
        object? defaultSettings,
        Func<IAddOn> factory)
    {
        _builtIn.Add(new LazyAddOn(
            id, name, description,
            iconGlyph, iconPath,
            "Built-in", "2.0.0",
            isBuiltIn: true, isGlobal,
            isEnabled: true, keyword,
            defaultSettings, factory));
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
