using System;
using Spur.Models;

namespace Spur.Services;

/// <summary>
/// Clamps out-of-range config values to valid ranges and null-initialises
/// all collection properties so callers never need to null-check them.
/// </summary>
public static class ConfigValidator
{
    /// <summary>
    /// Clamps out-of-range values to valid ranges and null-initialises all
    /// collection properties so callers never need to null-check them.
    /// </summary>
    public static void Validate(SpurConfig config)
    {
        // Numeric ranges
        config.WindowOpacity        = Math.Clamp(config.WindowOpacity,        0.5, 1.0);
        config.ResultsCount         = Math.Clamp(config.ResultsCount,         3,   20);
        config.MaxFileDepth         = Math.Clamp(config.MaxFileDepth,         1,   5);
        config.ClipboardHistorySize = Math.Clamp(config.ClipboardHistorySize, 5,   200);
        config.SearchDelay          = Math.Clamp(config.SearchDelay,          0,   500);


        config.FontScale            = Math.Clamp(config.FontScale,           0.8, 1.4);

        // Null-init all collection / dictionary properties so callers never need
        // to guard against null (e.g. after deserialising an older config file
        // that predates a newly added property).
        config.PinnedClipboard   ??= [];
        config.PinnedCategories  ??= ["files", "ai", "clipboard", ""];
        config.AddOns            ??= [];
        config.IndexedFolders    ??= [];
        config.ExclusionPatterns ??= ["node_modules", ".git", "__pycache__", "bin", "obj"];
        config.ExcludedFolders   ??= [];
        config.FileExtensions    ??= [".pdf", ".docx", ".doc", ".txt", ".xlsx"];
    }
}
