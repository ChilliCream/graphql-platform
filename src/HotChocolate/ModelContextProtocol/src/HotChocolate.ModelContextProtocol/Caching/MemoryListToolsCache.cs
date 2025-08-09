using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Caching.Memory;
using ModelContextProtocol.Protocol;

namespace HotChocolate.ModelContextProtocol.Caching;

public sealed class MemoryListToolsCache(IMemoryCache memoryCache) : IListToolsCache
{
    public void Set(ListToolsResult result)
    {
        memoryCache.Set(nameof(MemoryListToolsCache), result);
    }

    public bool TryGetValue([NotNullWhen(true)] out ListToolsResult? result)
    {
        return memoryCache.TryGetValue(nameof(MemoryListToolsCache), out result);
    }
}
