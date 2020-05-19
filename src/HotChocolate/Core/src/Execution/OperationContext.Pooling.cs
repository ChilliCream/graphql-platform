using HotChocolate.Execution.Utilities;
using HotChocolate.Fetching;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution
{
    internal sealed partial class OperationContext : IOperationContext
    {
        private readonly ExecutionContext _executionContext;
        private readonly ResultHelper _resultHelper;
        private IRequestContext _requestContext = default!;

        public OperationContext(
            ObjectPool<ResolverTask> resolverTaskPool,
            ResultPool resultPool)
        {
            _executionContext = new ExecutionContext(resolverTaskPool);
            _resultHelper = new ResultHelper(resultPool);
        }

        public void Initialize(
            IRequestContext requestContext,
            IBatchDispatcher batchDispatcher,
            IPreparedOperation operation,
            object? rootValue,
            IVariableValueCollection variables)
        {
            _requestContext = requestContext;
            _executionContext.Initialize(batchDispatcher);
            Operation = operation;
            RootValue = rootValue;
            Variables = variables;
        }

        public void Reset()
        {
            _executionContext.Reset();
            _resultHelper.Reset();
            _requestContext = default!;
            Operation = default!;
            RootValue = null;
            Variables = default!;
        }
    }
}