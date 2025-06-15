using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution;
using HotChocolate.Fusion.Logging;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public class FusionRequestExecutorManagerTests
{
    [Fact]
    public async Task CreateExecutor()
    {
        // arrange
        var schemaDocument =
            ComposeSchema(
                """
                schema @schemaName(value: "A") {
                    query: Query
                }

                type Query {
                    foo: String
                }

                directive @schemaName(value: String!) on SCHEMA
                """);

        var services =
            new ServiceCollection()
                .AddGraphQLGateway()
                .AddInMemoryConfiguration(schemaDocument)
                .UseDefaultPipeline()
                .Services
                .BuildServiceProvider();

        var executorProvider = services.GetRequiredService<IRequestExecutorProvider>();
        var executor = await executorProvider.GetExecutorAsync();

        // assert
        Assert.Equal(ISchemaDefinition.DefaultName, executor.Schema.Name);
    }

    protected static DocumentNode ComposeSchema(
        [StringSyntax("graphql")] params string[] schemas)
    {
        var compositionLog = new CompositionLog();
        var composer = new SchemaComposer(schemas, compositionLog);
        var result = composer.Compose();

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Errors[0].Message);
        }

        return result.Value.ToSyntaxNode();
    }
}
