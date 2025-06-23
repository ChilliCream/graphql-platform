using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class IntegrationTests : FusionTestBase
{
    [Fact]
    public async Task Foo()
    {
        // arrange
        var schema = ComposeSchemaDocument(
            """
            schema @schemaName(value: "A") {
              query: Query
            }

            type Query {
              foo: String
            }

            directive @schemaName(value: String!) on SCHEMA
            """,
            """
            schema @schemaName(value: "B") {
              query: Query
            }

            type Query {
              bar: String
            }

            directive @schemaName(value: String!) on SCHEMA
            """);

        var services =
            new ServiceCollection()
                .AddHttpClient()
                .AddGraphQLGateway()
                .AddInMemoryConfiguration(schema)
                .Services
                .BuildServiceProvider();

        var executorProvider = services.GetRequiredService<IRequestExecutorProvider>();
        var executor = await executorProvider.GetExecutorAsync();
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ foo }")
                .Build());
    }
}
