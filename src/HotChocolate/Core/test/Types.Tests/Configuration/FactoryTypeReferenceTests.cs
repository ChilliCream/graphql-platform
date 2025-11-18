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
            .BuildSchemaAsync();

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
