using System;
using System.Collections.Concurrent;
using HotChocolate.Fetching;
using Microsoft.Extensions.ObjectPool;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution.Processing
{
    internal sealed partial class OperationContext
    {
        private readonly ConcurrentBag<Action> _cleanupActions = new();
        private readonly ExecutionContext _executionContext;
        private readonly ResultHelper _resultHelper;
        private IRequestContext _requestContext = default!;
        private IPreparedOperation _operation = default!;
        private IVariableValueCollection _variables = default!;
        private IServiceProvider _services = default!;
        private Func<object?> _resolveQueryRootValue = default!;
        private object? _rootValue;
        private bool _isPooled = true;

        public OperationContext(
            ObjectPool<ResolverTask> resolverTaskPool,
            ResultPool resultPool)
        {
            _executionContext = new ExecutionContext(this, resolverTaskPool);
            _resultHelper = new ResultHelper(resultPool);
        }

        public bool IsPooled => _isPooled;

        public void Initialize(
            IRequestContext requestContext,
            IServiceProvider scopedServices,
            IContextBatchDispatcher batchDispatcher,
            IPreparedOperation operation,
            IVariableValueCollection variables,
            object? rootValue,
            Func<object?> resolveQueryRootValue)
        {
            _requestContext = requestContext;
            _executionContext.Initialize(
                batchDispatcher,
                requestContext.RequestAborted);
            _operation = operation;
            _variables = variables;
            _services = scopedServices;
            _rootValue = rootValue;
            _resolveQueryRootValue = resolveQueryRootValue;
            _isPooled = false;
        }

        public void Clean()
        {
            while (_cleanupActions.TryTake(out var clean))
            {
                clean();
            }

            _executionContext.Clean();
            _resultHelper.Clear();
            _requestContext = default!;
            _operation = default!;
            _variables = default!;
            _services = default!;
            _rootValue = null;
            _resolveQueryRootValue = default!;
            _isPooled = true;
        }

        private void AssertNotPooled()
        {
            if (_isPooled)
            {
                throw Object_Returned_To_Pool();
            }
        }
    }
}
