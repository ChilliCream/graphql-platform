using HotChocolate.Execution;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Configuration;

public class SourceGeneratorFactoryTypeReferenceTests
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
                        new FactoryTypeReference(
                            extension.Context.TypeInspector.GetTypeRef(typeof(string), TypeContext.Output),
                            static (_, typeDef) => new NonNullType(new ListType(new NonNullType(typeDef))),
                            "[String!]!",
                            TypeContext.Output);
                });
        }
    }
}
