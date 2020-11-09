using System;
using HotChocolate.Fetching;
using Microsoft.Extensions.ObjectPool;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution.Processing
{
    internal sealed partial class OperationContext
    {
        private readonly ExecutionContext _executionContext;
        private readonly ResultHelper _resultHelper;
        private IRequestContext _requestContext = default!;
        private IPreparedOperation _operation = default!;
        private IVariableValueCollection _variables = default!;
        private IServiceProvider _services = default!;
        private object? _rootValue;
        private bool _isPooled = true;

        public OperationContext(
            ObjectPool<ResolverTask> resolverTaskPool,
            ResultPool resultPool)
        {
            _executionContext = new ExecutionContext(this, resolverTaskPool);
            _resultHelper = new ResultHelper(resultPool);
        }

        public void Initialize(
            IRequestContext requestContext,
            IServiceProvider scopedServices,
            IBatchDispatcher batchDispatcher,
            IPreparedOperation operation,
            object? rootValue,
            IVariableValueCollection variables)
        {
            _requestContext = requestContext;
            _executionContext.Initialize(
                batchDispatcher,
                requestContext.RequestAborted);
            _operation = operation;
            _variables = variables;
            _services = scopedServices;
            _rootValue = rootValue;
            _isPooled = false;
        }

        public void Clean()
        {
            _executionContext.Clean();
            _resultHelper.Clear();
            _requestContext = default!;
            _operation = default!;
            _variables = default!;
            _services = default!;
            _rootValue = null;
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
