using Spur.Plugin;

namespace Spur.Core;

public class PluginPair
{
    public IPlugin Plugin { get; }
    public PluginMetadata Metadata { get; }

    public PluginPair(IPlugin plugin, PluginMetadata metadata)
    {
        Plugin = plugin;
        Metadata = metadata;
    }
}
