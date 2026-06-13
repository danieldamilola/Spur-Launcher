using System.Collections.Generic;
using System.Text.Json;

namespace Spur.Models;

public sealed class AddOnEntryConfig
{
    public bool Enabled { get; set; } = true;
    public string Keyword { get; set; } = "";
    public Dictionary<string, JsonElement> Options { get; set; } = new();
}
