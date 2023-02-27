using System;
using Microsoft.Extensions.ObjectPool;
using HotChocolate;
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
/// Operation context lifetime is managed by the OperationContext pool and
/// the execution pipeline.
///
/// The lifetime MUST NOT be managed or tracked by the DI container.
///
/// The <see cref="OperationContextFactory"/> MUST be a singleton.
/// </summary>
internal sealed class OperationContextFactory : IFactory<OperationContext>
{
    private readonly IFactory<ResolverTask> _resolverTaskFactory;
    private readonly ObjectPool<PathSegmentBuffer<IndexerPathSegment>> _indexerPathPool;
    private readonly ObjectPool<PathSegmentBuffer<NamePathSegment>> _namePathPool;
    private readonly ResultPool _resultPool;
    private readonly ITypeConverter _typeConverter;

    public OperationContextFactory(
        IFactory<ResolverTask> resolverTaskFactory,
        ObjectPool<PathSegmentBuffer<IndexerPathSegment>> indexerPathPool,
        ObjectPool<PathSegmentBuffer<NamePathSegment>> namePathPool,
        ResultPool resultPool,
        ITypeConverter typeConverter)
    {
        _resolverTaskFactory = resolverTaskFactory ??
            throw new ArgumentNullException(nameof(resolverTaskFactory));
        _indexerPathPool = indexerPathPool ??
            throw new ArgumentNullException(nameof(indexerPathPool));
        _namePathPool = namePathPool ??
            throw new ArgumentNullException(nameof(namePathPool));
        _resultPool = resultPool ??
            throw new ArgumentNullException(nameof(resultPool));
        _typeConverter = typeConverter ??
            throw new ArgumentNullException(nameof(typeConverter));
    }

    public OperationContext Create()
        => new OperationContext(
            _resolverTaskFactory,
            new PooledPathFactory(_indexerPathPool, _namePathPool),
            new ResultBuilder(_resultPool),
            _typeConverter);
}
