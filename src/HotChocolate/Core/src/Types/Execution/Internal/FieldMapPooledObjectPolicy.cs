using HotChocolate.Execution.Processing;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution;

internal sealed class FieldMapPooledObjectPolicy
    : DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>
{
    public override OrderedDictionary<string, List<FieldSelectionNode>> Create() => new(StringComparer.Ordinal);

    public override bool Return(OrderedDictionary<string, List<FieldSelectionNode>> obj)
    {
        if (obj.Count > 256)
        {
            return false;
        }

        obj.Clear();
        return base.Return(obj);
    }
}
