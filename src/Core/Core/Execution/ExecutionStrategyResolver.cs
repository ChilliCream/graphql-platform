using System;
using System.Collections.Generic;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Execution
{
    internal class ExecutionStrategyResolver
        : IExecutionStrategyResolver
    {
        private readonly Dictionary<OperationType, IExecutionStrategy> _strats;

        public ExecutionStrategyResolver(
            IRequestTimeoutOptionsAccessor options)
        {
            _strats = new Dictionary<OperationType, IExecutionStrategy>()
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
            if (_strats.TryGetValue(operationType,
                out IExecutionStrategy strategy))
            {
                return strategy;
            }

            throw new NotSupportedException(
                CoreResources.ExecutionStrategyResolver_NotSupported);
        }
    }
}
