namespace Spur.Services;

/// <summary>
/// Default implementation of ICommandRegistry.
/// Thread-safe, maintains insertion order + alphabetical sort for display.
/// </summary>
public sealed class CommandRegistry : ICommandRegistry
{
    private readonly List<CommandPaletteEntry> _entries = [];
    private readonly Lock _lock = new();

    public IReadOnlyList<CommandPaletteEntry> All
    {
        get
        {
            lock (_lock) return [.. _entries];
        }
    }

    public void Register(CommandPaletteEntry entry)
    {
        lock (_lock)
        {
            if (_entries.Any(e => e.Id == entry.Id))
                return;
            _entries.Add(entry);
            _entries.Sort((a, b) => string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase));
        }
    }

    public IEnumerable<CommandPaletteEntry> Search(string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return All;

        lock (_lock)
        {
            return _entries
                .Where(e =>
                    e.Label.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    e.Description.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    e.Id.Contains(filter, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    public CommandPaletteEntry? Find(string id)
    {
        lock (_lock)
        {
            return _entries.FirstOrDefault(e => e.Id == id);
        }
    }
}
