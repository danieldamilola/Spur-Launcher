using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Spur.Models;

namespace Spur.Services;

public sealed class AddOnStoreService
{
    // B4: HttpClient is injected rather than constructed here, preventing socket
    // exhaustion. The caller registers a named client via IHttpClientFactory or
    // passes a shared static instance. Do NOT instantiate HttpClient per service.
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly string _addOnsDir;
    private const string ManifestUrl = "https://raw.githubusercontent.com/danieldamilola/Spur-Extras-Manifest/main/manifest.json";

    public AddOnStoreService(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient;
        _logger     = logger;
        _addOnsDir  = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Spur", "AddOns");
        Directory.CreateDirectory(_addOnsDir);
    }

    public async Task<List<StoreManifestEntry>> GetManifestAsync(CancellationToken ct = default)
    {
        try
        {
            var json    = await _httpClient.GetStringAsync(ManifestUrl, ct);
            var entries = JsonSerializer.Deserialize<List<StoreManifestEntry>>(json);
            return entries ?? [];
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to fetch add-ons manifest.", ex);
            return [];
        }
    }

    public async Task<bool> InstallAddOnAsync(StoreManifestEntry entry, CancellationToken ct = default)
    {
        var zipPath   = Path.Combine(_addOnsDir, $"{entry.Id}.zip");
        var targetDir = Path.Combine(_addOnsDir, entry.Id);

        try
        {
            if (Directory.Exists(targetDir))
                Directory.Delete(targetDir, true);

            var zipBytes = await _httpClient.GetByteArrayAsync(entry.DownloadUrl, ct);
            await File.WriteAllBytesAsync(zipPath, zipBytes, ct);

            ZipFile.ExtractToDirectory(zipPath, targetDir, overwriteFiles: true);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to install add-on {entry.Id}.", ex);
            try { if (Directory.Exists(targetDir)) Directory.Delete(targetDir, true); } catch { }
            return false;
        }
        finally
        {
            try { if (File.Exists(zipPath)) File.Delete(zipPath); } catch { }
        }
    }

    public void UninstallAddOn(string addOnId)
    {
        try
        {
            var targetDir = Path.Combine(_addOnsDir, addOnId);
            if (Directory.Exists(targetDir))
                Directory.Delete(targetDir, true);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to uninstall add-on {addOnId}.", ex);
        }
    }

    /// <summary>
    /// Checks the manifest for a specific add-on and returns whether an update
    /// is available compared to the currently installed version.
    /// </summary>
    public async Task<(bool UpdateAvailable, string LatestVersion)> CheckForUpdateAsync(
        string addOnId, string currentVersion, CancellationToken ct = default)
    {
        try
        {
            var manifest = await GetManifestAsync(ct);
            var entry = manifest.Find(e => string.Equals(e.Id, addOnId, StringComparison.OrdinalIgnoreCase));

            if (entry == null)
                return (false, currentVersion);

            if (!Version.TryParse(NormalizeVersion(entry.Version), out var latest) ||
                !Version.TryParse(NormalizeVersion(currentVersion), out var current))
                return (false, entry.Version);

            return (latest > current, entry.Version);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to check for update for add-on {addOnId}.", ex);
            return (false, currentVersion);
        }
    }

    /// <summary>
    /// Ensures a version string has at least Major.Minor so <see cref="Version.TryParse"/> succeeds.
    /// </summary>
    private static string NormalizeVersion(string version)
    {
        var v = version.TrimStart('v', 'V');
        return v.Contains('.') ? v : v + ".0";
    }
}
