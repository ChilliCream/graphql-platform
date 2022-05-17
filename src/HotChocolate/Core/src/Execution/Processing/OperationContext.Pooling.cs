using System;
using System.Collections.Concurrent;
using HotChocolate.Execution.Processing.Plan;
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
    private readonly ResultHelper _resultHelper;
    private readonly PooledPathFactory _pathFactory;
    private IRequestContext _requestContext = default!;
    private IPreparedOperation _operation = default!;
    private QueryPlan _queryPlan = default!;
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
        _resultHelper = new ResultHelper(resultPool);
        _pathFactory = new PooledPathFactory(indexerPathSegmentPool, namePathSegmentPool);
    }

    public bool IsInitialized => _isInitialized;

    public void Initialize(
        IRequestContext requestContext,
        IServiceProvider scopedServices,
        IBatchDispatcher batchDispatcher,
        IPreparedOperation operation,
        QueryPlan queryPlan,
        IVariableValueCollection variables,
        object? rootValue,
        Func<object?> resolveQueryRootValue)
    {
        _requestContext = requestContext;
        _operation = operation;
        _queryPlan = queryPlan;
        _variables = variables;
        _services = scopedServices;
        _rootValue = rootValue;
        _resolveQueryRootValue = resolveQueryRootValue;
        _isInitialized = true;

        _workScheduler.Initialize(batchDispatcher);
    }

    public void Clean()
    {
        if (_isInitialized)
        {
            if (!_cleanupActions.IsEmpty)
            {
                while (_cleanupActions.TryTake(out Action? clean))
                {
                    clean();
                }
            }

            _pathFactory.Clear();
            _workScheduler.Clear();
            _resultHelper.Clear();
            _requestContext = default!;
            _operation = default!;
            _queryPlan = default!;
            _variables = default!;
            _services = default!;
            _rootValue = null;
            _resolveQueryRootValue = default!;
            _isInitialized = false;
        }
    }

    private void AssertInitialized()
    {
        if (!_isInitialized)
        {
            throw Object_Not_Initialized();
        }
    }
}
