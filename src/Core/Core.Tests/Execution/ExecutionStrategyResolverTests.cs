using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Execution
{
    public class ExecutionStrategyResolverTests
    {
        [Fact]
        public void Default_Query_Strategy_Is_QueryExecutionStrategy()
        {
            // arrange
            var options = new QueryExecutionOptions();
            var strategyResolver = new ExecutionStrategyResolver(options, options);

            // act
            IExecutionStrategy strategy = strategyResolver.Resolve(OperationType.Query);

            // assert
            Assert.IsType<QueryExecutionStrategy>(strategy);
        }

        [Fact]
        public void Default_Mutation_Strategy_Is_MutationStrategy()
        {
            // arrange
            var options = new QueryExecutionOptions();
            var strategyResolver = new ExecutionStrategyResolver(options, options);

            // act
            IExecutionStrategy strategy = strategyResolver.Resolve(OperationType.Mutation);

            // assert
            Assert.IsType<MutationExecutionStrategy>(strategy);
        }

        [Fact]
        public void Default_Subscription_Strategy_Is_SubscriptionStrategy()
        {
            // arrange
            var options = new QueryExecutionOptions();
            var strategyResolver = new ExecutionStrategyResolver(options, options);

            // act
            IExecutionStrategy strategy = strategyResolver.Resolve(OperationType.Subscription);

            // assert
            Assert.IsType<SubscriptionExecutionStrategy>(strategy);
        }

        [Fact]
        public void Serial_Query_Strategy_Is_QueryExecutionStrategy()
        {
            // arrange
            var options = new QueryExecutionOptions { ForceSerialExecution = true };
            var strategyResolver = new ExecutionStrategyResolver(options, options);

            // act
            IExecutionStrategy strategy = strategyResolver.Resolve(OperationType.Query);

            // assert
            Assert.IsType<SerialExecutionStrategy>(strategy);
        }

        [Fact]
        public void Serial_Mutation_Strategy_Is_MutationStrategy()
        {
            // arrange
            var options = new QueryExecutionOptions { ForceSerialExecution = true };
            var strategyResolver = new ExecutionStrategyResolver(options, options);

            // act
            IExecutionStrategy strategy = strategyResolver.Resolve(OperationType.Mutation);

            // assert
            Assert.IsType<SerialExecutionStrategy>(strategy);
        }

        [Fact]
        public void Serial_Subscription_Strategy_Is_SubscriptionStrategy()
        {
            // arrange
            var options = new QueryExecutionOptions { ForceSerialExecution = true };
            var strategyResolver = new ExecutionStrategyResolver(options, options);

            // act
            IExecutionStrategy strategy = strategyResolver.Resolve(OperationType.Subscription);

            // assert
            Assert.IsType<SubscriptionExecutionStrategy>(strategy);
        }
    }
}
