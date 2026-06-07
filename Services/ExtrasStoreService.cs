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

public sealed class ExtrasStoreService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly string _extrasDir;
    private const string ManifestUrl = "https://raw.githubusercontent.com/danieldamilola/Spur-Extras-Manifest/main/manifest.json";

    public ExtrasStoreService(ILogger logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _extrasDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Spur", "Extras");
        Directory.CreateDirectory(_extrasDir);
    }

    public async Task<List<StoreManifestEntry>> GetManifestAsync(CancellationToken ct = default)
    {
        try
        {
            var json = await _httpClient.GetStringAsync(ManifestUrl, ct);
            var entries = JsonSerializer.Deserialize<List<StoreManifestEntry>>(json);
            return entries ?? new List<StoreManifestEntry>();
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to fetch extras manifest.", ex);
            return new List<StoreManifestEntry>();
        }
    }

    public async Task<bool> InstallExtraAsync(StoreManifestEntry entry, CancellationToken ct = default)
    {
        try
        {
            var targetDir = Path.Combine(_extrasDir, entry.Id);
            if (Directory.Exists(targetDir))
            {
                Directory.Delete(targetDir, true);
            }

            var zipPath = Path.Combine(_extrasDir, $"{entry.Id}.zip");
            var zipBytes = await _httpClient.GetByteArrayAsync(entry.DownloadUrl, ct);
            await File.WriteAllBytesAsync(zipPath, zipBytes, ct);

            ZipFile.ExtractToDirectory(zipPath, targetDir, overwriteFiles: true);
            File.Delete(zipPath);

            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to install extra {entry.Id}.", ex);
            return false;
        }
    }

    public void UninstallExtra(string extraId)
    {
        try
        {
            var targetDir = Path.Combine(_extrasDir, extraId);
            if (Directory.Exists(targetDir))
            {
                Directory.Delete(targetDir, true);
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to uninstall extra {extraId}.", ex);
        }
    }
}
