using System.Diagnostics.CodeAnalysis;
using ModelContextProtocol.Protocol;

namespace HotChocolate.ModelContextProtocol.Caching;

public interface IListToolsCache
{
    void Set(ListToolsResult result);

    bool TryGetValue([NotNullWhen(true)] out ListToolsResult? result);
}
