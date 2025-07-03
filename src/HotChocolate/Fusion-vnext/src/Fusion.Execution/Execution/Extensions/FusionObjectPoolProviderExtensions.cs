using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;

namespace Microsoft.Extensions.ObjectPool;

internal static class FusionObjectPoolProviderExtensions
{
    public static ObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>> CreateFieldMapPool(
        this ObjectPoolProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        return provider.Create(new FieldMapPooledObjectPolicy());
    }
}
