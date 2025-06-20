using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution.Nodes;

internal sealed class FieldMapPooledObjectPolicy : DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>
{
    public override OrderedDictionary<string, List<FieldSelectionNode>> Create()
        => new OrderedDictionary<string, List<FieldSelectionNode>>(StringComparer.Ordinal);

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
