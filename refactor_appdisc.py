import re
import os

path = r'c:\dev\Arc\Services\AppDiscoveryService.cs'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

content = re.sub(r'private static (?!readonly)(\w+)(<[^>]+>)?(\??)\s+(\w+)\(', r'private \1\2\3 \4(', content)
content = re.sub(r'private static void (\w+)\(', r'private void \1(', content)
content = content.replace('public static ', 'public ')

content = content.replace(
    'public sealed class AppDiscoveryService\n{',
    'public interface IAppDiscoveryService\n{\n    Task<List<SearchResult>> DiscoverAsync(CancellationToken ct = default);\n    void ClearCache();\n}\n\npublic sealed class AppDiscoveryService : IAppDiscoveryService\n{\n    private readonly ILogger _logger;\n\n    public AppDiscoveryService(ILogger logger)\n    {\n        _logger = logger;\n    }'
)

content = re.sub(r'(?:System\.Diagnostics\.)?Debug\.WriteLine\(\$?"\[Arc\] ([^:]+): \{ex\.Message\}"\);', r'_logger.Warning("\1", ex);', content)

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)
