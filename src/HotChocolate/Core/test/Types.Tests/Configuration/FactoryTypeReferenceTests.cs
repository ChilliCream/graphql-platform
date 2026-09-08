using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Configuration;

public class FactoryTypeReferenceTests
{
    [Fact]
    public async Task FactoryTypeReference_Is_Handled()
    {
        var schema = await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType<QueryType>()
            // No @listSize on this schema, so pin the assumed list size ahead of
            // cost enforcement going live (R-DEFAULT-LIST-SIZE).
            .ModifyCostOptions(o => o.DefaultListSize = 1)
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        schema.MatchSnapshot();
    }

    public class QueryType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("Query");
            descriptor
                .Field("hello")
                .Resolve(new object[] { "World" })
                .ExtendWith(static extension =>
                {
                    extension.Configuration.Type =
                        TypeReference.Create(
                            extension.Context.TypeInspector.GetTypeRef(typeof(string), TypeContext.Output),
                            Utf8GraphQLParser.Syntax.ParseTypeReference("[String!]!"));
                });
        }
    }
}
