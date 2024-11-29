using HotChocolate.ApolloFederation.Types;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;

namespace HotChocolate.ApolloFederation;

public class ComposeDirectiveTests
{
    [Fact]
    public async Task TestServiceTypeEmptyQueryTypePureCodeFirst()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType()
            .AddType<Address>()
            .ExportDirective<Custom>()
            .BuildSchemaAsync();

        var entityType = schema.GetType<ObjectType>(FederationTypeNames.ServiceType_Name);
        var sdlResolver = entityType.Fields[WellKnownFieldNames.Sdl].Resolver!;

        // act
        var value = await sdlResolver(TestHelper.CreateResolverContext(schema));

        Utf8GraphQLParser
            .Parse((string)value!)
            .MatchSnapshot();
    }

    [Key("field")]
    public class Address
    {
        [CustomDirective]
        public string Field => "abc";
    }

    [Package("https://specs.custom.dev/custom/v1.0")]
    [DirectiveType(DirectiveLocation.FieldDefinition)]
    public sealed class Custom;

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public sealed class CustomDirectiveAttribute()
        : DirectiveAttribute<Custom>(new Custom());
}
