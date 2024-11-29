using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Execution;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.ApolloFederation.Directives;

public class OverrideDirectiveTests
{
    [Fact]
    public async Task OverrideDirective_Annotation()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation(FederationVersion.Federation20)
            .AddQueryType()
            .AddType<Foo>()
            .BuildSchemaAsync();

        var entityType = schema.GetType<ObjectType>(FederationTypeNames.ServiceType_Name);
        var sdlResolver = entityType.Fields[WellKnownFieldNames.Sdl].Resolver!;

        // act
        var value = await sdlResolver(TestHelper.CreateResolverContext(schema));
        value!.MatchSnapshot();
    }

    [Fact]
    public async Task OverrideDirective_Progressive_Annotation()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation(FederationVersion.Federation27)
            .AddQueryType()
            .AddType<Foo>()
            .BuildSchemaAsync();

        var entityType = schema.GetType<ObjectType>(FederationTypeNames.ServiceType_Name);
        var sdlResolver = entityType.Fields[WellKnownFieldNames.Sdl].Resolver!;

        // act
        var value = await sdlResolver(TestHelper.CreateResolverContext(schema));
        value!.MatchSnapshot();
    }

    [Key("id")]
    public class Foo(string id)
    {
        [ID]
        public string Id { get; } = id;

        [Override("bar")]
        public string Name => "abc";

        [Override("bar", "percent(1)")]
        public string Description => "xyz";

        [ReferenceResolver]
        public static Foo? ResolveReference(string id)
        {
            return new Foo(id);
        }
    }
}
