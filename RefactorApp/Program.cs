using System.IO;
using System.Text.RegularExpressions;

var path = @"c:\dev\Arc\Services\AppDiscoveryService.cs";
var content = File.ReadAllText(path);

content = content.Replace("public sealed class AppDiscoveryService\r\n{", 
    "public interface IAppDiscoveryService\r\n{\r\n    Task<List<SearchResult>> DiscoverAsync(CancellationToken ct = default);\r\n    void ClearCache();\r\n}\r\n\r\npublic sealed class AppDiscoveryService : IAppDiscoveryService\r\n{\r\n    private readonly ILogger _logger;\r\n\r\n    public AppDiscoveryService(ILogger logger)\r\n    {\r\n        _logger = logger;\r\n    }");
content = content.Replace("public sealed class AppDiscoveryService\n{", 
    "public interface IAppDiscoveryService\n{\n    Task<List<SearchResult>> DiscoverAsync(CancellationToken ct = default);\n    void ClearCache();\n}\n\npublic sealed class AppDiscoveryService : IAppDiscoveryService\n{\n    private readonly ILogger _logger;\n\n    public AppDiscoveryService(ILogger logger)\n    {\n        _logger = logger;\n    }");

content = content.Replace("public static Task<List<SearchResult>> DiscoverAsync", "public Task<List<SearchResult>> DiscoverAsync");
content = content.Replace("public static void ClearCache", "public void ClearCache");

content = Regex.Replace(content, @"private static (?!readonly\b)([a-zA-Z0-9_]+(?:<[^>]+>)?\??)\s+([a-zA-Z0-9_]+)\s*\(", "private $1 $2(");

content = Regex.Replace(content, @"(?:System\.Diagnostics\.)?Debug\.WriteLine\(\$?""\[Arc\] ([^:]+): \{ex\.Message\}""\);", "_logger.Warning(\"$1\", ex);");

File.WriteAllText(path, content);
