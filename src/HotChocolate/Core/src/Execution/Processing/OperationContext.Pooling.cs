using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using HotChocolate.Execution.Processing.Tasks;
using HotChocolate.Fetching;
using Microsoft.Extensions.ObjectPool;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution.Processing;

internal sealed partial class OperationContext
{
    private readonly ConcurrentBag<Action> _cleanupActions = new();
    private readonly ObjectPool<ResolverTask> _resolverTaskPool;
    private readonly WorkScheduler _workScheduler;
    private readonly ResultBuilder _resultHelper;
    private readonly PooledPathFactory _pathFactory;
    private IRequestContext _requestContext = default!;
    private IOperation _operation = default!;
    private IVariableValueCollection _variables = default!;
    private IServiceProvider _services = default!;
    private Func<object?> _resolveQueryRootValue = default!;
    private object? _rootValue;
    private bool _isInitialized;

    public OperationContext(
        ObjectPool<ResolverTask> resolverTaskPool,
        ResultPool resultPool,
        ObjectPool<PathSegmentBuffer<IndexerPathSegment>> indexerPathSegmentPool,
        ObjectPool<PathSegmentBuffer<NamePathSegment>> namePathSegmentPool)
    {
        _resolverTaskPool = resolverTaskPool;
        _workScheduler = new WorkScheduler(this);
        _resultHelper = new ResultBuilder(resultPool);
        _pathFactory = new PooledPathFactory(indexerPathSegmentPool, namePathSegmentPool);
    }

    public bool IsInitialized => _isInitialized;

    public void Initialize(
        IRequestContext requestContext,
        IServiceProvider scopedServices,
        IBatchDispatcher batchDispatcher,
        IOperation operation,
        IVariableValueCollection variables,
        object? rootValue,
        Func<object?> resolveQueryRootValue)
    {
        _requestContext = requestContext;
        _operation = operation;
        _variables = variables;
        _services = scopedServices;
        _rootValue = rootValue;
        _resolveQueryRootValue = resolveQueryRootValue;
        _isInitialized = true;

        IncludeFlags = _operation.CreateIncludeFlags(variables);
        _workScheduler.Initialize(batchDispatcher);
    }

    public void Clean()
    {
        if (_isInitialized)
        {
            if (!_cleanupActions.IsEmpty)
            {
                while (_cleanupActions.TryTake(out var clean))
                {
                    clean();
                }
            }

            _pathFactory.Clear();
            _workScheduler.Clear();
            _resultHelper.Clear();
            _requestContext = default!;
            _operation = default!;
            _variables = default!;
            _services = default!;
            _rootValue = null;
            _resolveQueryRootValue = default!;
            _isInitialized = false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AssertInitialized()
    {
        if (!_isInitialized)
        {
            throw Object_Not_Initialized();
        }
    }
}
