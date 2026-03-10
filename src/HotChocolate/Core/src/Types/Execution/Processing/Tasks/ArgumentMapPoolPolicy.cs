using Microsoft.Extensions.ObjectPool;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution.Processing.Tasks;

internal sealed class ArgumentMapPoolPolicy : PooledObjectPolicy<Dictionary<string, ArgumentValue>>
{
    public override Dictionary<string, ArgumentValue> Create()
        => new(StringComparer.Ordinal);

    public override bool Return(Dictionary<string, ArgumentValue> obj)
    {
        obj.Clear();
        return true;
    }
}
