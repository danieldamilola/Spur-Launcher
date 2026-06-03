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
        public Task<List<SearchResult>> DiscoverAsync(CancellationToken ct = default)
        {
            var list = new List<SearchResult>
            {
                new SearchResult { Id = "app:notepad", Type = ResultType.App, Name = "Notepad" },
                new SearchResult { Id = "app:vsc", Type = ResultType.App, Name = "Visual Studio Code" },
            };
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
        public int MaxItems { get; set; }
        public IReadOnlyList<ClipboardEntry> GetHistory() => new List<ClipboardEntry>();
        public void Add(string text) { }
        public void AddImage(System.Windows.Media.Imaging.BitmapSource image) { }
        public void CopyToSystem(string text) { }
        public string? ReadFromSystem() => null;
        public System.Windows.Media.Imaging.BitmapSource? ReadImageFromSystem() => null;
        public void Clear() { }
        public void KeepOnly(ISet<string> contentToKeep) { }
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
    }

    private sealed class FakeRegistry : ICommandRegistry
    {
        private readonly List<CommandPaletteEntry> _items = new();
        public IReadOnlyList<CommandPaletteEntry> All => _items;
        public void Register(CommandPaletteEntry entry) { _items.Add(entry); }
        public IEnumerable<CommandPaletteEntry> Search(string filter) => _items.Where(i => i.Label.Contains(filter ?? "", StringComparison.OrdinalIgnoreCase) || i.Description.Contains(filter ?? "", StringComparison.OrdinalIgnoreCase));
        public CommandPaletteEntry? Find(string id) => _items.FirstOrDefault(i => i.Id == id);
    }

    [Fact]
    public async Task QueryProducesAppResults()
    {
        var cfg = new SpurConfig { FuzzySearch = true };
        var registry = new FakeRegistry();
        var commandPalette = new CommandPaletteViewModel(registry);
        var vm = new MainViewModel(cfg, NullLogger.Instance,
            new FakeApps(), new FakeFiles(), new FakeFreq(), new FakeConfigSvc(cfg),
            new FakeClip(), new FakeNotify(), new FakeAi(), new FakeTheme(), new FakeStartup(),
            registry, commandPalette);

        // Set query and wait for debounce + async search
        vm.Query = "note";
        await Task.Delay(300);

        Assert.True(vm.Results.Count > 0, "Expected some results after query");
        var found = vm.Results.OfType<SearchResult>().Any(r => r.Name.Contains("Notepad", StringComparison.OrdinalIgnoreCase));
        Assert.True(found, "Expected Notepad to appear in results");
    }
}



