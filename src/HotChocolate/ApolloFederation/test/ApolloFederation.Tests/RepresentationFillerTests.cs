using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.ApolloFederation;

public class RepresentationFillerTests
{
    [Fact]
    public async Task Invoke_Should_ConstructNestedObject_When_InstanceIsNull()
    {
        // arrange
        var schema = await BuildSchemaAsync();
        var type = schema.Types.GetType<ObjectType>(nameof(Post));
        var filler = GetFiller(type);
        var entity = new Post { Id = "p1", Author = null };
        var representation = new ObjectValueNode(
            new ObjectFieldNode("id", "p1"),
            new ObjectFieldNode(
                "author",
                new ObjectValueNode(
                    new ObjectFieldNode("id", "a1"),
                    new ObjectFieldNode("yearsOfExperience", 5),
                    new ObjectFieldNode(
                        "address",
                        new ObjectValueNode(new ObjectFieldNode("city", "NYC"))))));

        // act
        filler.Invoke(schema, type, representation, entity);

        // assert
        Assert.NotNull(entity.Author);
        Assert.Equal("a1", entity.Author!.Id);
        Assert.Equal(5, entity.Author.YearsOfExperience);
        Assert.Equal("NYC", entity.Author.Address!.City);
    }

    [Fact]
    public async Task Invoke_Should_FillNestedObjectPerProperty_When_InstanceIsNonNull()
    {
        // arrange
        var schema = await BuildSchemaAsync();
        var type = schema.Types.GetType<ObjectType>(nameof(Post));
        var filler = GetFiller(type);
        var entity = new Post
        {
            Id = "p1",
            Author = new Author
            {
                Id = "a1",
                YearsOfExperience = 7,
                Address = new Address { City = "OldCity", Street = "Main St" }
            }
        };
        var representation = new ObjectValueNode(
            new ObjectFieldNode("id", "p1"),
            new ObjectFieldNode(
                "author",
                new ObjectValueNode(
                    new ObjectFieldNode("yearsOfExperience", 9),
                    new ObjectFieldNode(
                        "address",
                        new ObjectValueNode(new ObjectFieldNode("city", "NewCity"))))));

        // act
        filler.Invoke(schema, type, representation, entity);

        // assert
        Assert.Equal(9, entity.Author!.YearsOfExperience);
        Assert.Equal("a1", entity.Author.Id);
        Assert.Equal("NewCity", entity.Author.Address!.City);
        Assert.Equal("Main St", entity.Author.Address.Street);
    }

    [Fact]
    public async Task Invoke_Should_ReconstructList_When_RepresentationCarriesList()
    {
        // arrange
        var schema = await BuildSchemaAsync();
        var type = schema.Types.GetType<ObjectType>(nameof(Post));
        var filler = GetFiller(type);
        var entity = new Post { Id = "p1", Tags = new[] { "old1", "old2", "old3" } };
        var representation = new ObjectValueNode(
            new ObjectFieldNode("id", "p1"),
            new ObjectFieldNode(
                "tags",
                new ListValueNode(new StringValueNode("a"), new StringValueNode("b"))));

        // act
        filler.Invoke(schema, type, representation, entity);

        // assert
        Assert.Equal(new[] { "a", "b" }, entity.Tags);
    }

    [Fact]
    public async Task Invoke_Should_OverwriteScalar_When_RepresentationCarriesValue()
    {
        // arrange
        var schema = await BuildSchemaAsync();
        var type = schema.Types.GetType<ObjectType>(nameof(Post));
        var filler = GetFiller(type);
        var entity = new Post { Id = "p1", Rating = 3 };
        var representation = new ObjectValueNode(
            new ObjectFieldNode("id", "p1"),
            new ObjectFieldNode("rating", 10));

        // act
        filler.Invoke(schema, type, representation, entity);

        // assert
        Assert.Equal(10, entity.Rating);
    }

    [Fact]
    public async Task Invoke_Should_PreserveResolverValues_When_RepresentationSilent()
    {
        // arrange
        var schema = await BuildSchemaAsync();
        var type = schema.Types.GetType<ObjectType>(nameof(Post));
        var filler = GetFiller(type);
        var entity = new Post
        {
            Id = "p1",
            Rating = 3,
            Author = new Author { Id = "a1", YearsOfExperience = 7 }
        };
        var representation = new ObjectValueNode(new ObjectFieldNode("id", "p1"));

        // act
        filler.Invoke(schema, type, representation, entity);

        // assert
        Assert.Equal(3, entity.Rating);
        Assert.Equal(7, entity.Author!.YearsOfExperience);
        Assert.Equal("a1", entity.Author.Id);
    }

    [Fact]
    public async Task Invoke_Should_ReconstructAbstractObject_When_TypenameMatches()
    {
        // arrange
        var schema = await BuildSchemaAsync();
        var type = schema.Types.GetType<ObjectType>(nameof(PetHost));
        var filler = GetFiller(type);
        var entity = new PetHost { Id = "o1", Pet = null };
        var representation = new ObjectValueNode(
            new ObjectFieldNode("id", "o1"),
            new ObjectFieldNode(
                "pet",
                new ObjectValueNode(
                    new ObjectFieldNode("__typename", "Dog"),
                    new ObjectFieldNode("name", "Rex"),
                    new ObjectFieldNode("breed", "Lab"))));

        // act
        filler.Invoke(schema, type, representation, entity);

        // assert
        var dog = Assert.IsType<Dog>(entity.Pet);
        Assert.Equal("Rex", dog.Name);
        Assert.Equal("Lab", dog.Breed);
    }

    [Fact]
    public async Task Invoke_Should_TerminateAndFill_When_EntityGraphIsCyclic()
    {
        // arrange
        var schema = await BuildSchemaAsync();
        var type = schema.Types.GetType<ObjectType>(nameof(CyclicNode));
        var filler = GetFiller(type);
        var entity = new CyclicNode { Id = "n1", Next = null };
        var representation = new ObjectValueNode(
            new ObjectFieldNode("id", "n1"),
            new ObjectFieldNode(
                "next",
                new ObjectValueNode(
                    new ObjectFieldNode("id", "n2"),
                    new ObjectFieldNode("label", "second"),
                    new ObjectFieldNode(
                        "next",
                        new ObjectValueNode(
                            new ObjectFieldNode("id", "n3"),
                            new ObjectFieldNode("label", "third"))))));

        // act
        filler.Invoke(schema, type, representation, entity);

        // assert
        Assert.Equal("n2", entity.Next!.Id);
        Assert.Equal("second", entity.Next.Label);
        Assert.Equal("third", entity.Next.Next!.Label);
    }

    [Fact]
    public async Task Invoke_Should_SkipNonSettableMember_And_FillSiblings()
    {
        // arrange
        var schema = await BuildSchemaAsync();
        var type = schema.Types.GetType<ObjectType>(nameof(GadgetHost));
        var entity = new GadgetHost { Id = "h1", Gadget = new Gadget { Name = "old" } };
        var representation = new ObjectValueNode(
            new ObjectFieldNode("id", "h1"),
            new ObjectFieldNode(
                "gadget",
                new ObjectValueNode(
                    new ObjectFieldNode("name", "new"),
                    new ObjectFieldNode("serial", 99))));
        var hasFiller = type.Features.TryGet(out ExternalSetter? filler);

        // act
        filler?.Invoke(schema, type, representation, entity);

        // assert
        Assert.True(hasFiller);
        Assert.Equal("new", entity.Gadget!.Name);
        Assert.Equal(0, entity.Gadget.Serial);
    }

    private static async Task<Schema> BuildSchemaAsync()
        => await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .AddType<IPet>()
            .AddType<Dog>()
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

    private static ExternalSetter GetFiller(ObjectType type)
    {
        Assert.True(type.Features.TryGet(out ExternalSetter? setter));
        return setter!;
    }

    public sealed class Query
    {
        public Post Post { get; set; } = null!;

        public PetHost PetHost { get; set; } = null!;

        public CyclicNode CyclicNode { get; set; } = null!;

        public GadgetHost GadgetHost { get; set; } = null!;
    }

    [ReferenceResolver(EntityResolver = nameof(GetAsync))]
    public sealed class Post
    {
        [Key]
        public string Id { get; set; } = null!;

        public Author? Author { get; set; }

        public IReadOnlyList<string>? Tags { get; set; }

        public int Rating { get; set; }

        public static Task<Post> GetAsync(string id)
            => Task.FromResult(new Post { Id = id });
    }

    public sealed class Author
    {
        public string? Id { get; set; }

        public int YearsOfExperience { get; set; }

        public Address? Address { get; set; }
    }

    public sealed class Address
    {
        public string? City { get; set; }

        public string? Street { get; set; }
    }

    [ReferenceResolver(EntityResolver = nameof(GetAsync))]
    public sealed class PetHost
    {
        [Key]
        public string Id { get; set; } = null!;

        public IPet? Pet { get; set; }

        public static Task<PetHost> GetAsync(string id)
            => Task.FromResult(new PetHost { Id = id });
    }

    [InterfaceType("Pet")]
    public interface IPet
    {
        string? Name { get; }
    }

    public sealed class Dog : IPet
    {
        public string? Name { get; set; }

        public string? Breed { get; set; }
    }

    [ReferenceResolver(EntityResolver = nameof(GetAsync))]
    public sealed class CyclicNode
    {
        [Key]
        public string Id { get; set; } = null!;

        public string? Label { get; set; }

        public CyclicNode? Next { get; set; }

        public static Task<CyclicNode> GetAsync(string id)
            => Task.FromResult(new CyclicNode { Id = id });
    }

    [ReferenceResolver(EntityResolver = nameof(GetAsync))]
    public sealed class GadgetHost
    {
        [Key]
        public string Id { get; set; } = null!;

        public Gadget? Gadget { get; set; }

        public static Task<GadgetHost> GetAsync(string id)
            => Task.FromResult(new GadgetHost { Id = id });
    }

    public sealed class Gadget
    {
        public string? Name { get; set; }

        public int Serial { get; }
    }
}
