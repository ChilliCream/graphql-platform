using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution;

/// <summary>
/// Represents a pooled object policy for <see cref="PooledRequestContext"/>.
/// </summary>
public sealed class RequestContextPooledObjectPolicy : PooledObjectPolicy<PooledRequestContext>
{
    /// <inheritdoc />
    public override PooledRequestContext Create()
        => new();

    /// <inheritdoc />
    public override bool Return(PooledRequestContext obj)
    {
        obj.Reset();
        return true;
    }
}
