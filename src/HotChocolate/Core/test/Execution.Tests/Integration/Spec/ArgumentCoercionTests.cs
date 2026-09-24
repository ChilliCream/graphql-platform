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
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request =
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    {
                      sayHello(name: null)
                    }
                    """)
                .Build();

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

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
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request =
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    {
                      sayHello
                    }
                    """)
                .Build();

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

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
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request =
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    query ($a: String!) {
                      sayHello(name: $a)
                    }
                    """)
                .Build();

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

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
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request =
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    query ($a: String!) {
                      sayHello(name: $a)
                    }
                    """)
                .SetVariableValues(
                    """
                    {
                      "a": null
                    }
                    """)
                .Build();

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

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
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request =
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    query ($a: String!) {
                      sayHello(name: $a)
                    }
                    """)
                .SetVariableValues(
                    """
                    {
                      "a": "Sydney"
                    }
                    """)
                .Build();

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_Pass_Null_When_Omitted_Variable_Has_Null_Default()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .BuildRequestExecutorAsync(
                    cancellationToken: TestContext.Current.CancellationToken);

        var request =
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    query ($value: Int = null) {
                      echo(value: $value)
                    }
                    """)
                .Build();

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "echo": null
              }
            }
            """);
    }

    public class Query
    {
        public int? Echo(int? value = 5)
            => value;

        public string SayHello(string name = "Michael")
            => $"Hello {name}.";
    }
}
