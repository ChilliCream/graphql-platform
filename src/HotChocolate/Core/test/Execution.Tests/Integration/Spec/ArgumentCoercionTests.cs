using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

#nullable enable

namespace HotChocolate.Execution.Integration.Spec
{
    public class ArgumentCoercionTests
    {
        [Fact]
        public async Task Pass_In_Null_To_NonNullArgument_With_DefaultValue()
        {
            // arrange
            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<Query>()
                    .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ sayHello(name: null) }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Pass_In_Nothing_To_NonNullArgument_With_DefaultValue()
        {
            // arrange
            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<Query>()
                    .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ sayHello }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Pass_In_Nothing_To_NonNullArgument_With_DefaultValue_By_Variable()
        {
            // arrange
            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<Query>()
                    .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "query ($a: String!) { sayHello(name: $a) }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Pass_In_Null_To_NonNullArgument_With_DefaultValue_By_Variable()
        {
            // arrange
            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<Query>()
                    .BuildRequestExecutorAsync();

            var variables = new Dictionary<string, object?> { { "a", null } };

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "query ($a: String!) { sayHello(name: $a) }",
                variables);

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Pass_In_Sydney_To_NonNullArgument_With_DefaultValue_By_Variable()
        {
            // arrange
            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<Query>()
                    .BuildRequestExecutorAsync();

            var variables = new Dictionary<string, object?> { { "a", "Sydney" } };

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "query ($a: String!) { sayHello(name: $a) }",
                variables);

            // assert
            result.ToJson().MatchSnapshot();
        }

        public class Query
        {
            public string SayHello(string name = "Michael") => $"Hello {name}.";
        }
    }
}
