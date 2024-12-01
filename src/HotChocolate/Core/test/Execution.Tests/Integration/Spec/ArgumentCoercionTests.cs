using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Integration.Spec;

public class ArgumentCoercionTests
{
    [Fact]
    public async Task Pass_In_Null_To_NonNullArgument_With_DefaultValue()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ sayHello(name: null) }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Pass_In_Nothing_To_NonNullArgument_With_DefaultValue()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ sayHello }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Pass_In_Nothing_To_NonNullArgument_With_DefaultValue_By_Variable()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "query ($a: String!) { sayHello(name: $a) }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Pass_In_Null_To_NonNullArgument_With_DefaultValue_By_Variable()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .BuildRequestExecutorAsync();

        var variables = new Dictionary<string, object?> { { "a", null }, };

        // act
        var result = await executor.ExecuteAsync(
            "query ($a: String!) { sayHello(name: $a) }",
            variables);

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Pass_In_Sydney_To_NonNullArgument_With_DefaultValue_By_Variable()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .BuildRequestExecutorAsync();

        var variables = new Dictionary<string, object?> { { "a", "Sydney" }, };

        // act
        var result = await executor.ExecuteAsync(
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
