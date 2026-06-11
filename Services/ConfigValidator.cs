using System;
using Spur.Models;

namespace Spur.Services;

public static class ConfigValidator
{
    /// <summary>Clamps out-of-range values to valid ranges.</summary>
    public static void Validate(SpurConfig config)
    {
        config.WindowOpacity = Math.Clamp(config.WindowOpacity, 0.5, 1.0);
        config.ResultsCount = Math.Clamp(config.ResultsCount, 3, 20);
        config.MaxFileDepth = Math.Clamp(config.MaxFileDepth, 1, 5);
        config.ClipboardHistorySize = Math.Clamp(config.ClipboardHistorySize, 5, 200);
        config.PinnedClipboard ??= [];
    }
}
