namespace Arc.Models;

public sealed class ScopeFilterItem
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public int Count { get; init; }
}
