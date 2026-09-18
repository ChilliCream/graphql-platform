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
    public async Task ExecuteAsync_Should_Report_Path_Without_Locations_When_Inline_Input_Object_Literal_Has_Invalid_Leaf()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .AddMutationType<Mutation>()
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request =
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    mutation {
                      updateUser(input: { avatar: "" })
                    }
                    """)
                .Build();

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The value is not a valid image data URL.",
                  "path": [
                    "updateUser"
                  ],
                  "extensions": {
                    "inputPath": [
                      "input",
                      "avatar"
                    ],
                    "coordinate": "UpdateUserInput.avatar",
                    "fieldType": "ImageDataUrl"
                  }
                }
              ],
              "data": null
            }
            """);
    }

    public class Query
    {
        public string SayHello(string name = "Michael")
            => $"Hello {name}.";
    }

    public class Mutation
    {
        public string UpdateUser(UpdateUserInput input)
            => $"{input.Avatar} was updated!";
    }
}
