using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.ApolloFederation;

public class NestedRequiresOverlayTests
{
    [Fact]
    public async Task Invoke_Should_OverlayLeafOntoIntermediate_When_IntermediateIsNonNull()
    {
        // arrange
        var schema = await BuildSchemaAsync();
        var type = schema.Types.GetType<ObjectType>(nameof(NestedRequiresPost));
        var setter = GetExternalSetter(type);
        var entity = new NestedRequiresPost { Id = "p1", Author = new NestedAuthor { Id = "a1" } };
        var representation = new ObjectValueNode(
            new ObjectFieldNode("id", "p1"),
            new ObjectFieldNode(
                "author",
                new ObjectValueNode(new ObjectFieldNode("yearsOfExperience", 5))));

        // act
        setter.Invoke(schema, type, representation, entity);

        // assert
        Assert.Equal(5, entity.Author!.YearsOfExperience);
        Assert.True(entity.ByNovice);
    }

    [Fact]
    public async Task Invoke_Should_ReconstructIntermediate_When_IntermediateIsNull()
    {
        // arrange
        var schema = await BuildSchemaAsync();
        var type = schema.Types.GetType<ObjectType>(nameof(NestedRequiresPost));
        var setter = GetExternalSetter(type);
        var entity = new NestedRequiresPost { Id = "p1", Author = null };
        var representation = new ObjectValueNode(
            new ObjectFieldNode("id", "p1"),
            new ObjectFieldNode(
                "author",
                new ObjectValueNode(
                    new ObjectFieldNode("id", "a1"),
                    new ObjectFieldNode("yearsOfExperience", 5))));

        // act
        setter.Invoke(schema, type, representation, entity);

        // assert
        Assert.NotNull(entity.Author);
        Assert.Equal("a1", entity.Author!.Id);
        Assert.Equal(5, entity.Author.YearsOfExperience);
    }

    [Fact]
    public async Task Invoke_Should_PreserveResolverValue_When_RepresentationMissingNestedPath()
    {
        // arrange
        var schema = await BuildSchemaAsync();
        var type = schema.Types.GetType<ObjectType>(nameof(NestedRequiresPost));
        var setter = GetExternalSetter(type);
        var entity = new NestedRequiresPost
        {
            Id = "p1",
            Author = new NestedAuthor { Id = "a1", YearsOfExperience = 7 }
        };
        var representation = new ObjectValueNode(new ObjectFieldNode("id", "p1"));

        // act
        setter.Invoke(schema, type, representation, entity);

        // assert
        Assert.Equal(7, entity.Author!.YearsOfExperience);
    }

    [Fact]
    public async Task ExternalSetter_Should_NotBeGenerated_When_NestedLeafIsNotSettable()
    {
        // arrange
        var schema = await BuildSchemaAsync();
        var type = schema.Types.GetType<ObjectType>(nameof(ReadOnlyLeafPost));

        // act
        var hasSetter = type.Features.TryGet(out ExternalSetter? setter);

        // assert
        Assert.False(hasSetter);
        Assert.Null(setter);
    }

    private static async Task<Schema> BuildSchemaAsync()
        => await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

    private static ExternalSetter GetExternalSetter(ObjectType type)
    {
        Assert.True(type.Features.TryGet(out ExternalSetter? setter));
        return setter!;
    }

    public sealed class Query
    {
        public NestedRequiresPost NestedRequiresPost { get; set; } = null!;

        public ReadOnlyLeafPost ReadOnlyLeafPost { get; set; } = null!;
    }

    [ReferenceResolver(EntityResolver = nameof(GetAsync))]
    public sealed class NestedRequiresPost
    {
        [Key]
        public string Id { get; set; } = null!;

        public NestedAuthor? Author { get; set; }

        [Requires("author { yearsOfExperience }")]
        public bool ByNovice => Author?.YearsOfExperience is int years && years < 10;

        public static Task<NestedRequiresPost> GetAsync(string id)
            => Task.FromResult(new NestedRequiresPost { Id = id });
    }

    [ReferenceResolver(EntityResolver = nameof(GetAsync))]
    public sealed class NestedAuthor
    {
        [Key]
        public string Id { get; set; } = null!;

        [External]
        public int? YearsOfExperience { get; set; }

        public static Task<NestedAuthor> GetAsync(string id)
            => Task.FromResult(new NestedAuthor { Id = id });
    }

    [ReferenceResolver(EntityResolver = nameof(GetAsync))]
    public sealed class ReadOnlyLeafPost
    {
        [Key]
        public string Id { get; set; } = null!;

        public ReadOnlyLeafAuthor? Author { get; set; }

        [Requires("author { yearsOfExperience }")]
        public bool ByNovice => (Author?.YearsOfExperience ?? 0) < 10;

        public static Task<ReadOnlyLeafPost> GetAsync(string id)
            => Task.FromResult(new ReadOnlyLeafPost { Id = id });
    }

    [ReferenceResolver(EntityResolver = nameof(GetAsync))]
    public sealed class ReadOnlyLeafAuthor
    {
        [Key]
        public string Id { get; set; } = null!;

        [External]
        public int YearsOfExperience { get; }

        public static Task<ReadOnlyLeafAuthor> GetAsync(string id)
            => Task.FromResult(new ReadOnlyLeafAuthor { Id = id });
    }
}
