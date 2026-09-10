using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.DependencyInjection;

public class RequestExecutorBuilderExtensionsSchemaOptionsTests
{
    [Fact]
    public async Task ModifyOptions_ValidatePipelineOrder_False()
    {
        var interceptor = new OptionsInterceptor();

        await new ServiceCollection()
            .AddGraphQLServer()
            .AddType<Query>()
            .ModifyOptions(o => o.ValidatePipelineOrder = false)
            .TryAddTypeInterceptor(interceptor)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(interceptor.Options.ValidatePipelineOrder);
    }

    [Fact]
    public async Task ModifyOptions_EnableEmptySelectionSets_ExecutesEmptySelectionSets()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .ModifyOptions(o => o.EnableEmptySelectionSets = true)
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var rootResult = await executor.ExecuteAsync("{ }", TestContext.Current.CancellationToken);
        var objectResult = await executor.ExecuteAsync("{ hero { } }", TestContext.Current.CancellationToken);

        // assert
        rootResult.MatchInlineSnapshot(
            """
            {
              "data": {}
            }
            """);
        objectResult.MatchInlineSnapshot(
            """
            {
              "data": {
                "hero": {}
              }
            }
            """);
    }

    [Fact]
    public async Task ConfigureSchema_Should_ExecuteEmptySelectionSets_When_Enabled()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .ConfigureSchema(b => b.ModifyOptions(o => o.EnableEmptySelectionSets = true))
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync("{ }", TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {}
            }
            """);
    }

    [Fact]
    public async Task MakeExecutable_Should_ExecuteEmptySelectionSets_When_SchemaIsPrebuilt()
    {
        // arrange
        var schema =
            SchemaBuilder.New()
                .ModifyOptions(o => o.EnableEmptySelectionSets = true)
                .AddQueryType<Query>()
                .Create();
        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync("{ }", TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {}
            }
            """);
    }

    [Fact]
    public async Task ConfigureValidation_Should_RejectEmptySelectionSets_When_Disabled()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .ModifyOptions(o => o.EnableEmptySelectionSets = true)
                .ConfigureValidation((_, b) =>
                    b.ModifyOptions(o => o.EnableEmptySelectionSets = false))
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync("{ }", TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Operation `Unnamed` has an empty selection set. Root types without selections are disallowed.",
                  "locations": [
                    {
                      "line": 1,
                      "column": 1
                    }
                  ],
                  "extensions": {
                    "operation": "Unnamed",
                    "type": "Query",
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Field-Selections"
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_RejectEmptySelectionSets_When_Disabled()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync("{ }", TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Operation `Unnamed` has an empty selection set. Root types without selections are disallowed.",
                  "locations": [
                    {
                      "line": 1,
                      "column": 1
                    }
                  ],
                  "extensions": {
                    "operation": "Unnamed",
                    "type": "Query",
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Field-Selections"
                  }
                }
              ]
            }
            """);
    }

    private sealed class OptionsInterceptor : TypeInterceptor
    {
        public IReadOnlySchemaOptions Options { get; private set; } = null!;

        internal override void OnBeforeCreateSchemaInternal(
            IDescriptorContext context,
            ISchemaBuilder schemaBuilder)
        {
            Options = context.Options;
        }
    }

    public class Query
    {
        public string Abc() => "abc";

        public Hero Hero() => new();
    }

    public class Hero
    {
        public string Name => "Luke";
    }
}
