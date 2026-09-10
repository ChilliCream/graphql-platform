using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Introspection;

public sealed class ObjectDeprecationIntrospectionTests : FusionTestBase
{
    [Fact]
    public async Task Introspect_Should_ReportTheDeprecation_When_ObjectIsDeprecated()
    {
        // arrange
        var executor = await CreateExecutorAsync(enableObjectDeprecation: true);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              dragon: __type(name: "Dragon") { name isDeprecated deprecationReason }
              dog: __type(name: "Dog") { name isDeprecated deprecationReason }
              string: __type(name: "String") { name isDeprecated deprecationReason }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "dragon": {
                  "name": "Dragon",
                  "isDeprecated": true,
                  "deprecationReason": "No longer known to exist."
                },
                "dog": {
                  "name": "Dog",
                  "isDeprecated": false,
                  "deprecationReason": null
                },
                "string": {
                  "name": "String",
                  "isDeprecated": null,
                  "deprecationReason": null
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Introspect_Should_FilterSchemaTypes_When_IncludeDeprecatedIsFalse()
    {
        // arrange
        var executor = await CreateExecutorAsync(enableObjectDeprecation: true);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              included: __schema { types(includeDeprecated: true) { name } }
              excluded: __schema { types(includeDeprecated: false) { name } }
              omitted: __schema { types { name } }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Introspect_Should_FilterPossibleTypes_When_IncludeDeprecatedIsFalse()
    {
        // arrange
        var executor = await CreateExecutorAsync(enableObjectDeprecation: true);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              animal: __type(name: "Animal") {
                included: possibleTypes(includeDeprecated: true) { name }
                excluded: possibleTypes(includeDeprecated: false) { name }
                omitted: possibleTypes { name }
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Introspect_Should_NotExposeTheFields_When_OptionIsOff()
    {
        // arrange
        var executor = await CreateExecutorAsync(enableObjectDeprecation: false);

        // act
        var result = await executor.ExecuteAsync(
            """{ __type(name: "__Type") { fields { name } } }""",
            TestContext.Current.CancellationToken);

        // assert
        result.MatchSnapshot();
    }

    private static async Task<IRequestExecutor> CreateExecutorAsync(bool enableObjectDeprecation)
    {
        const string sdl =
            """
            type Query { animals: [Animal] }

            interface Animal { name: String }

            type Dog implements Animal { name: String }

            type Dragon implements Animal @deprecated(reason: "No longer known to exist.") {
              name: String
            }
            """;

        var services = new ServiceCollection();
        services.AddHttpClient();
        services
            .AddGraphQLGateway()
            .ModifyOptions(o => o.EnableObjectDeprecation = enableObjectDeprecation)
            .AddInMemoryConfiguration(ComposeSchemaDocument(sdl))
            .UseDefaultPipeline();

        return await services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
    }
}
