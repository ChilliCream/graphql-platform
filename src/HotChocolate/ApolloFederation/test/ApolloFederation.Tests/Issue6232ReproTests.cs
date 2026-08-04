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
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

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

    [Fact]
    public async Task External_Object_And_Object_List_Fields_Are_Set_From_Representation()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        var type = schema.Types.GetType<ObjectType>(nameof(ExternalComplexFields));
        var representation = new ObjectValueNode(
            new ObjectFieldNode("id", "id_123"),
            new ObjectFieldNode(
                "category",
                new ObjectValueNode(
                    new ObjectFieldNode("averagePrice", 11))),
            new ObjectFieldNode(
                "comments",
                new ListValueNode(
                    new ObjectValueNode(new ObjectFieldNode("authorId", "author-1")),
                    new ObjectValueNode(new ObjectFieldNode("authorId", "author-2")))));

        // act
        var result = await ResolveRef(schema, type, representation);

        // assert
        var entity = Assert.IsType<ExternalComplexFields>(result);
        Assert.Equal("11:author-1/author-2", entity.Summary);
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
            externalSetter.Invoke(schema, type, representation, entity);
        }

        return entity;
    }

    public sealed class Query
    {
        public ExternalListFields ExternalRefResolver { get; set; } = null!;

        public ExternalComplexFields ExternalComplexRefResolver { get; set; } = null!;
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

    [ReferenceResolver(EntityResolver = nameof(GetAsync))]
    public sealed class ExternalComplexFields
    {
        [Key]
        public string Id { get; set; } = null!;

        [External]
        public Category? Category { get; private set; }

        [External]
        public IReadOnlyList<Comment>? Comments { get; private set; }

        [Requires("category { averagePrice } comments { authorId }")]
        public string? Summary
            => Category is null || Comments is null
                ? null
                : $"{Category.AveragePrice}:{string.Join('/', Comments.Select(t => t.AuthorId))}";

        public static Task<ExternalComplexFields> GetAsync(string id)
            => Task.FromResult(new ExternalComplexFields { Id = id });
    }

    public sealed class Category
    {
        public int AveragePrice { get; set; }
    }

    public sealed class Comment
    {
        public string? AuthorId { get; set; }
    }
}
