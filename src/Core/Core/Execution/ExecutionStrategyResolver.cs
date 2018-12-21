using System;
using System.Collections.Generic;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Execution
{
    internal class ExecutionStrategyResolver
        : IExecutionStrategyResolver
    {
        private readonly Dictionary<OperationType, IExecutionStrategy> _strategies;

        public ExecutionStrategyResolver(
            IRequestTimeoutOptionsAccessor options)
        {
            _strategies = new Dictionary<OperationType, IExecutionStrategy>()
            {
                {
                    OperationType.Query,
                    new QueryExecutionStrategy()
                },
                {
                    OperationType.Mutation,
                    new MutationExecutionStrategy()
                },
                {
                    OperationType.Subscription,
                    new SubscriptionExecutionStrategy(options)
                }
            };
        }

        public IExecutionStrategy Resolve(OperationType operationType)
        {
            if (_strategies.TryGetValue(operationType,
                out IExecutionStrategy strategy))
            {
                return strategy;
            }

            throw new NotSupportedException("Operation not supported!");
        }
    }
}
