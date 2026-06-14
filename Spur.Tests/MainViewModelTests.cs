using System;
using System.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Spur.Models;
using Spur.Services;
using Spur.ViewModels;
using Xunit;

namespace Spur.Tests;

public class MainViewModelTests
{
    private sealed class FakeApps : IAppDiscoveryService
    {
        public event Action<List<SearchResult>>? CatalogRefreshed;
        public Task<List<SearchResult>> DiscoverAsync(CancellationToken ct = default)
        {
            var list = new List<SearchResult>
            {
                new SearchResult { Id = "app:notepad", Type = ResultType.App, Name = "Notepad" },
                new SearchResult { Id = "app:vsc", Type = ResultType.App, Name = "Visual Studio Code" },
            };
            CatalogRefreshed?.Invoke(list);
            return Task.FromResult(list);
        }
        public void ClearCache() { }
    }

    private sealed class FakeFiles : IFileSearchService
    {
        public int MaxDepth { get; set; }
        public Task<List<SearchResult>> SearchAsync(string query, int maxReturn = 20, CancellationToken ct = default)
            => Task.FromResult(new List<SearchResult>());
        public Task<List<SearchResult>> BrowseRecentAsync(int maxReturn = 50)
            => Task.FromResult(new List<SearchResult>());
    }

    private sealed class FakeFreq : IFrequencyService
    {
        public int Get(string path) => 0;
        public void Increment(string path) { }
        public void ClearAll() { }
        public void Flush() { }
        public void Dispose() { }
    }

    private sealed class FakeConfigSvc : IConfigService
    {
        private readonly SpurConfig _cfg;
        public FakeConfigSvc(SpurConfig cfg) => _cfg = cfg;
        public SpurConfig Load() => _cfg;
        public void Save(SpurConfig config) { }
        public Task<SpurConfig> LoadAsync() => Task.FromResult(_cfg);
        public Task SaveAsync(SpurConfig config) { return Task.CompletedTask; }
    }

    private sealed class FakeClip : IClipboardService
    {
        public event Action? ClipboardChanged;
        public int MaxItems { get; set; }
        public IReadOnlyList<ClipboardEntry> GetHistory() => new List<ClipboardEntry>();
        public void Add(string text) { }
        public void AddImage(System.Windows.Media.Imaging.BitmapSource image) { }
        public void CopyToSystem(ClipboardEntry entry) { }
        public void CopyTextToSystem(string text) { }
        public string? ReadFromSystem() => null;
        public System.Windows.Media.Imaging.BitmapSource? ReadImageFromSystem() => null;
        public void Clear() { }
        public void KeepOnly(ISet<string> contentToKeep) { }
        public void RemoveById(Guid id) { }
    }

    private sealed class FakeNotify : INotificationService { public void Show(string t, string m) { } }
    private sealed class FakeAi : IAiService
    {
        public string[] SupportedProviders => new string[0];
        public Task StreamAsync(string provider, string model, string apiKey, string question, Action<string> onToken, CancellationToken ct = default) => Task.CompletedTask;
        public Task StreamAsync(string provider, string model, string apiKey, IEnumerable<(string Role, string Content)> messages, Action<string> onToken, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeTheme : IThemeManager
    {
        public string? LastApplied { get; private set; }
        public void Apply(string theme) => LastApplied = theme;
    }

    private sealed class FakeStartup : IStartupService
    {
        public bool Enabled { get; private set; }
        public void Enable() => Enabled = true;
        public void Disable() => Enabled = false;
        public bool IsEnabled() => Enabled;
        public bool Toggle() { Enabled = !Enabled; return Enabled; }
    }

    private sealed class FakeRegistry : ICommandRegistry
    {
        private readonly List<CommandPaletteEntry> _items = new();
        public IReadOnlyList<CommandPaletteEntry> All => _items;
        public void Register(CommandPaletteEntry entry) { _items.Add(entry); }
        public IEnumerable<CommandPaletteEntry> Search(string filter) => _items.Where(i => i.Label.Contains(filter ?? "", StringComparison.OrdinalIgnoreCase) || i.Description.Contains(filter ?? "", StringComparison.OrdinalIgnoreCase));
        public CommandPaletteEntry? Find(string id) => _items.FirstOrDefault(i => i.Id == id);
    }

    private class FakeSecureStorage : ISecureStorageService
    {
        public string Encrypt(string plain) => plain;
        public string Decrypt(string cipher) => cipher;
    }

    private static MainViewModel CreateViewModel(SpurConfig cfg)
    {
        var registry = new FakeRegistry();
        var commandPalette = new CommandPaletteViewModel(registry);
        var apps = new FakeApps();
        var files = new FakeFiles();
        var freq = new FakeFreq();
        var clip = new FakeClip();
        var addOns = new Spur.Extensions.AddOnRegistry(NullLogger.Instance);
        var searchEngine = new SearchEngineService(NullLogger.Instance, apps, files, clip, cfg, freq, addOns);

        return new MainViewModel(cfg, NullLogger.Instance,
            apps, files, freq, new FakeConfigSvc(cfg),
            clip, new FakeNotify(), new FakeAi(), new FakeTheme(), new FakeStartup(),
            registry, commandPalette, searchEngine, new FakeSecureStorage(), addOns, new Spur.Services.AddOnStoreService(new System.Net.Http.HttpClient(), NullLogger.Instance));
    }

    [Fact]
    public async Task QueryProducesAppResults()
    {
        var cfg = new SpurConfig { FuzzySearch = true };
        var apps = new FakeApps();
        var vm = CreateViewModel(cfg);

        // Set query and wait for debounce + async search.
        vm.Query = "note";
        await Task.Delay(300);

        Assert.True(vm.Results.Count > 0, "Expected some results after query");
        var found = vm.Results.OfType<SearchResult>().Any(r => r.Name.Contains("Notepad", StringComparison.OrdinalIgnoreCase));
        Assert.True(found, "Expected Notepad to appear in results");
    }

    [Fact]
    public async Task CommandsCategoryProducesActionRows()
    {
        var cfg = new SpurConfig
        {
            IndexSystemCommands = true,
        };

        var vm = CreateViewModel(cfg);

        vm.ActiveCategory = "actions";
        await Task.Delay(300);

        var actions = vm.Results.OfType<SearchResult>().Where(r => r.Type == ResultType.Action).ToList();
        Assert.NotEmpty(actions);
        Assert.Contains(actions, r => r.ActionId == "timer");
        Assert.Contains(actions, r => r.ActionId == "system");
    }

    [Fact]
    public async Task AiCategoryProducesAiPreviewRowEvenWhenDisabled()
    {
        var cfg = new SpurConfig();
        var vm = CreateViewModel(cfg);

        vm.ActiveCategory = "ai";
        await Task.Delay(300);

        var ai = vm.Results.OfType<SearchResult>().FirstOrDefault(r => r.ActionId == "ai");
        Assert.NotNull(ai);
        Assert.Equal("Ask AI", ai.Name);
    }
}
