using System;
using HotChocolate.Execution;
using HotChocolate.Execution.DependencyInjection;
using HotChocolate.Execution.Processing;
using HotChocolate.Execution.Processing.Tasks;
using HotChocolate.Utilities;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// The <see cref="OperationContextFactory"/> creates new instances of
/// <see cref="OperationContext"/>.
///
/// Operation context lifetime is managed by it's pool and the execution pipeline NOT by the DI.
///
/// The <see cref="OperationContextFactory"/> MUST be a singleton.
/// </summary>
internal sealed class OperationContextFactory : IFactory<OperationContext>
{
    private readonly IFactory<ResolverTask> _resolverTaskFactory;
    private readonly PooledPathFactory _pathFactory;
    private readonly ResultBuilder _resultBuilder;
    private readonly ITypeConverter _typeConverter;

    public OperationContextFactory(
        IFactory<ResolverTask> resolverTaskFactory,
        PooledPathFactory pathFactory,
        ResultBuilder resultBuilder,
        ITypeConverter typeConverter)
    {
        _resolverTaskFactory = resolverTaskFactory ??
            throw new ArgumentNullException(nameof(resolverTaskFactory));
        _pathFactory = pathFactory ??
            throw new ArgumentNullException(nameof(pathFactory));
        _resultBuilder = resultBuilder ??
            throw new ArgumentNullException(nameof(resultBuilder));
        _typeConverter = typeConverter ??
            throw new ArgumentNullException(nameof(typeConverter));
    }

    public OperationContext Create()
        => new OperationContext(
            _resolverTaskFactory,
            _pathFactory,
            _resultBuilder,
            _typeConverter);
}
