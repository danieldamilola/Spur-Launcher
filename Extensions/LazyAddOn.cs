using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Spur.Models;

namespace Spur.Extensions;

/// <summary>
/// Thin proxy that wraps a <see cref="Lazy{IAddOn}"/> so the real add-on
/// is only instantiated when a <em>behaviour</em> method (CanHandle, GetResults,
/// ExecuteAsync …) is first called.  Identity / metadata properties are stored
/// directly on the proxy so filtering, config load/save, and UI listing never
/// force creation.
/// </summary>
internal sealed class LazyAddOn : IAddOn
{
    private readonly Lazy<IAddOn> _inner;

    // ── Locally-stored identity (set at registration time) ──────────
    public string Id          { get; }
    public string Name        { get; }
    public string Description { get; }
    public string IconGlyph   { get; }
    public string? IconPath   { get; }
    public string Author      { get; }
    public string Version     { get; }
    public bool   IsBuiltIn   { get; }
    public bool   IsGlobal    { get; }

    // ── Mutable state kept on the proxy ─────────────────────────────
    public bool   IsEnabled { get; set; }
    public string Keyword   { get; set; }

    // Settings: stored on the proxy until the inner instance is created,
    // then forwarded.  Once the inner exists, reads come from the inner.
    private object? _pendingSettings;
    private bool    _settingsForwarded;

    public object? Settings
    {
        get => _inner.IsValueCreated ? _inner.Value.Settings : _pendingSettings;
        set
        {
            _pendingSettings = value;
            if (_inner.IsValueCreated)
            {
                _inner.Value.Settings = value;
                _settingsForwarded = true;
            }
        }
    }

    public LazyAddOn(
        string id, string name, string description,
        string iconGlyph, string? iconPath,
        string author, string version,
        bool isBuiltIn, bool isGlobal,
        bool isEnabled, string keyword,
        object? defaultSettings,
        Func<IAddOn> factory)
    {
        Id          = id;
        Name        = name;
        Description = description;
        IconGlyph   = iconGlyph;
        IconPath    = iconPath;
        Author      = author;
        Version     = version;
        IsBuiltIn   = isBuiltIn;
        IsGlobal    = isGlobal;
        IsEnabled   = isEnabled;
        Keyword     = keyword;
        _pendingSettings = defaultSettings;

        _inner = new Lazy<IAddOn>(() =>
        {
            var inst = factory();
            // Sync mutable state into the real instance
            inst.IsEnabled = IsEnabled;
            inst.Keyword   = Keyword;
            if (_pendingSettings is not null)
                inst.Settings = _pendingSettings;
            _settingsForwarded = true;
            return inst;
        });
    }

    /// <summary>Ensures the real instance is created and state is synced.</summary>
    private IAddOn Inner
    {
        get
        {
            var v = _inner.Value; // may trigger factory above
            // If settings were updated after initial creation, push them through
            if (!_settingsForwarded && _pendingSettings is not null)
            {
                v.Settings = _pendingSettings;
                _settingsForwarded = true;
            }
            return v;
        }
    }

    // ── Behaviour — delegates to inner (triggers instantiation) ─────
    public FrameworkElement? CreateSettingsView() => Inner.CreateSettingsView();
    public bool CanHandle(string query)           => Inner.CanHandle(query);
    public SearchResult BuildResult(string query) => Inner.BuildResult(query);
    public IEnumerable<SearchResult> GetResults(string subQuery) => Inner.GetResults(subQuery);
    public Task<AddOnResult> ExecuteAsync(string input, CancellationToken ct = default)
        => Inner.ExecuteAsync(input, ct);
}
