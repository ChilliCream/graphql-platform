using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate;

internal static class ObjectPoolProviderExtensions
{
    public static ObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>> CreateFieldMapPool(
        this ObjectPoolProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        return provider.Create(new FieldMapPooledObjectPolicy());
    }
}
