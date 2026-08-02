using System.Data.OleDb;

namespace Spur.Services.FileSearch;

internal sealed class WindowsIndexProvider
{
    private const string ConnectionString = "Provider=Search.CollatorDSO;Extended Properties='Application=Windows';";

    private static bool? _available;

    public static bool IsAvailable
    {
        get
        {
            if (_available.HasValue) return _available.Value;
            try
            {
                using var conn = new OleDbConnection(ConnectionString);
                conn.Open();
                conn.Close();
                _available = true;
            }
            catch
            {
                _available = false;
            }
            return _available.Value;
        }
    }

    public static async Task<List<(string Path, bool IsFolder)>> SearchAsync(string query, int maxCount, CancellationToken ct)
    {
        if (!IsAvailable || string.IsNullOrWhiteSpace(query))
            return [];

        var results = new List<(string, bool)>();

        try
        {
            var searchTerms = query.Replace("'", "''");
            var sql = $@"
                SELECT TOP {maxCount}
                    System.ItemUrl,
                    System.FileExtension
                FROM SystemIndex
                WHERE System.ItemName LIKE '%{searchTerms}%'
                    AND System.ItemUrl IS NOT NULL
                    AND System.FileExtension IS NOT NULL
                ORDER BY System.DateModified DESC";

            await using var conn = new OleDbConnection(ConnectionString);
            await conn.OpenAsync(ct);
            if (ct.IsCancellationRequested) return [];

            await using var cmd = new OleDbCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                if (reader.GetValue(0) is not string rawUrl || rawUrl.Length == 0) continue;
                var ext = reader.GetValue(1) as string ?? string.Empty;

                var path = rawUrl.StartsWith("file://", StringComparison.OrdinalIgnoreCase)
                    ? new Uri(rawUrl).LocalPath
                    : rawUrl;

                results.Add((path, ext.Equals("Directory", StringComparison.OrdinalIgnoreCase)));
            }
        }
        catch (OleDbException) { /* Search index unavailable */ }
        catch (InvalidOperationException) { /* Query error */ }

        return results;
    }
}
