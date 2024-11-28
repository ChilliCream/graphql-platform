using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Descriptors.Definitions;

public class DefinitionBaseTests
{
    [Fact]
    public async Task MergeInto_MultipleTypeExtensionXmlDescriptions_UsesQueryTypeDescription()
    {
        // arrange & act
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d.Description("Query"))
            .AddTypeExtension(typeof(QueryExtWithDocs1))
            .AddTypeExtension(typeof(QueryExtWithDocs2))
            .BuildSchemaAsync();

        // assert
        schema.MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            "Query"
            type Query {
              foo1: Int!
              foo2: Int!
            }
            """);
    }
}
