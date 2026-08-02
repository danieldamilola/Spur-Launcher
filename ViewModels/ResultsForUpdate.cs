using Spur.Models;

namespace Spur.ViewModels;

public readonly record struct ResultsForUpdate(
    List<SearchResult> Results,
    string PluginId);
