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
        private readonly Dictionary<OperationType, IExecutionStrategy> _strategies;

        public ExecutionStrategyResolver(
            IRequestTimeoutOptionsAccessor requestTimeoutOptions,
            IExecutionStrategyOptionsAccessor strategyOptions)
        {
            bool serialExecution = strategyOptions.ForceSerialExecution ?? false;

            _strategies = new Dictionary<OperationType, IExecutionStrategy>()
            {
                {
                    OperationType.Query,
                    serialExecution
                        ? (IExecutionStrategy)new SerialExecutionStrategy()
                        : new QueryExecutionStrategy()
                },
                {
                    OperationType.Mutation,
                    serialExecution
                        ? (IExecutionStrategy)new SerialExecutionStrategy()
                        : new MutationExecutionStrategy()
                },
                {
                    OperationType.Subscription,
                    new SubscriptionExecutionStrategy(requestTimeoutOptions)
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

            throw new NotSupportedException(
                CoreResources.ExecutionStrategyResolver_NotSupported);
        }
    }
}
