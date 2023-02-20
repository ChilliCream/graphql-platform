using HotChocolate.Execution.DependencyInjection;
using HotChocolate.Execution.Processing;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// The <see cref="OperationContextOwnerFactory"/> creates new instances of
/// <see cref="OperationContextOwner"/>. Each create will create a new instance that MUST NOT
/// be tracked by the DI.
///
/// The <see cref="OperationContextOwnerFactory"/> MUST be a singleton.
/// </summary>
internal sealed class OperationContextOwnerFactory : IFactory<OperationContextOwner>
{
    private readonly ObjectPool<OperationContext> _pool;

    public OperationContextOwnerFactory(ObjectPool<OperationContext> pool)
    {
        _pool = pool;
    }

    public OperationContextOwner Create() => new(_pool);
}
