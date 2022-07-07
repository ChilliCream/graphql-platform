using HotChocolate.Execution.Processing;
using Microsoft.Extensions.ObjectPool;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Execution;

internal static class OperationContextPoolExtensions
{
    public static OperationContextOwner GetOwner(this ObjectPool<OperationContext> pool)
        => new(pool.Get(), pool);
}
