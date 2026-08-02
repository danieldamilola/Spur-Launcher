namespace Spur.Plugin;

public interface IPlugin
{
    string Id { get; }
    string Name { get; }
    string Description { get; }
    string IconGlyph { get; }
    string ActionKeyword { get; }

    Task InitAsync(PluginInitContext context, CancellationToken ct = default);
    Task<List<PluginResult>> QueryAsync(Query query, CancellationToken ct);
}
