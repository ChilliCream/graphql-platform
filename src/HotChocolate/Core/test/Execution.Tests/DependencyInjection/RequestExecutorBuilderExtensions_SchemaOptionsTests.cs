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
        var rootResult = await executor.ExecuteAsync("{ }");
        var objectResult = await executor.ExecuteAsync("{ hero { } }");

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
