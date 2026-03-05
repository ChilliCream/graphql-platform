using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.ApolloFederation.FederationContextData;
using static HotChocolate.ApolloFederation.TestHelper;

namespace HotChocolate.ApolloFederation;

public class Issue6232ReproTests
{
    [Fact]
    public async Task External_List_Field_Is_Set_From_Representation()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .BuildSchemaAsync();

        var type = schema.Types.GetType<ObjectType>(nameof(ExternalListFields));
        var representation = new ObjectValueNode(
            new ObjectFieldNode("id", "id_123"),
            new ObjectFieldNode(
                "nickNames",
                new ListValueNode(
                    new StringValueNode("Jane"),
                    new StringValueNode("Jay"))));

        // act
        var result = await ResolveRef(schema, type, representation);

        // assert
        var entity = Assert.IsType<ExternalListFields>(result);
        Assert.Equal(new[] { "Jane", "Jay" }, entity.NickNames);
        Assert.Equal("Jane/Jay", entity.DisplayName);
    }

    private async ValueTask<object?> ResolveRef(
        Schema schema,
        ObjectType type,
        ObjectValueNode representation)
    {
        var resolverContextObject = type.Features.Get<ReferenceResolver>()?.Resolver;
        Assert.NotNull(resolverContextObject);

        var resolver = Assert.IsType<FieldResolverDelegate>(resolverContextObject);
        var context = CreateResolverContext(schema, type);

        context.SetLocalState(DataField, representation);
        context.SetLocalState(TypeField, type);

        var entity = await resolver.Invoke(context);

        if (entity is not null
            && type.Features.TryGet(out ExternalSetter? externalSetter))
        {
            externalSetter.Invoke(type, representation, entity);
        }

        return entity;
    }

    public sealed class Query
    {
        public ExternalListFields ExternalRefResolver { get; set; } = null!;
    }

    [ReferenceResolver(EntityResolver = nameof(GetAsync))]
    public sealed class ExternalListFields
    {
        [Key]
        public string Id { get; set; } = null!;

        [External]
        public IReadOnlyList<string>? NickNames { get; private set; }

        [Requires("nickNames")]
        public string? DisplayName => NickNames is null ? null : string.Join('/', NickNames);

        public static Task<ExternalListFields> GetAsync(string id)
            => Task.FromResult(new ExternalListFields { Id = id });
    }
}
