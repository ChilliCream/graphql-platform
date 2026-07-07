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

public class ExternalAbstractFieldTests
{
    [Fact]
    public async Task ResolveReference_Should_ReconstructConcreteType_When_TypenameMatchesImplementor()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .AddType<IPet>()
            .AddType<Dog>()
            .AddType<Cat>()
            .AddType<Rock>()
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        var type = schema.Types.GetType<ObjectType>(nameof(PetOwner));
        var representation = new ObjectValueNode(
            new ObjectFieldNode("id", "o1"),
            new ObjectFieldNode(
                "pet",
                new ObjectValueNode(
                    new ObjectFieldNode("__typename", "Dog"),
                    new ObjectFieldNode("name", "Rex"),
                    new ObjectFieldNode("breed", "Lab"))));

        // act
        var result = await ResolveRef(schema, type, representation);

        // assert
        var owner = Assert.IsType<PetOwner>(result);
        var pet = Assert.IsType<Dog>(owner.Pet);
        Assert.Equal("Rex", pet.Name);
        Assert.Equal("Lab", pet.Breed);
    }

    [Fact]
    public async Task ResolveReference_Should_LeavePropertyNull_When_TypenameIsAbsent()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .AddType<IPet>()
            .AddType<Dog>()
            .AddType<Cat>()
            .AddType<Rock>()
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        var type = schema.Types.GetType<ObjectType>(nameof(PetOwner));
        var representation = new ObjectValueNode(
            new ObjectFieldNode("id", "o1"),
            new ObjectFieldNode(
                "pet",
                new ObjectValueNode(new ObjectFieldNode("name", "Rex"))));

        // act
        var result = await ResolveRef(schema, type, representation);

        // assert
        var owner = Assert.IsType<PetOwner>(result);
        Assert.Null(owner.Pet);
    }

    [Fact]
    public async Task ResolveReference_Should_LeavePropertyNull_When_TypenameIsNotAssignable()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .AddType<IPet>()
            .AddType<Dog>()
            .AddType<Cat>()
            .AddType<Rock>()
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        var type = schema.Types.GetType<ObjectType>(nameof(PetOwner));
        var representation = new ObjectValueNode(
            new ObjectFieldNode("id", "o1"),
            new ObjectFieldNode(
                "pet",
                new ObjectValueNode(
                    new ObjectFieldNode("__typename", "Rock"),
                    new ObjectFieldNode("weight", 5))));

        // act
        var result = await ResolveRef(schema, type, representation);

        // assert
        var owner = Assert.IsType<PetOwner>(result);
        Assert.Null(owner.Pet);
    }

    [Fact]
    public async Task ResolveReference_Should_LeavePropertyNull_When_TypenameIsUnknown()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .AddType<IPet>()
            .AddType<Dog>()
            .AddType<Cat>()
            .AddType<Rock>()
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        var type = schema.Types.GetType<ObjectType>(nameof(PetOwner));
        var representation = new ObjectValueNode(
            new ObjectFieldNode("id", "o1"),
            new ObjectFieldNode(
                "pet",
                new ObjectValueNode(
                    new ObjectFieldNode("__typename", "Unicorn"),
                    new ObjectFieldNode("name", "Sparkle"))));

        // act
        var result = await ResolveRef(schema, type, representation);

        // assert
        var owner = Assert.IsType<PetOwner>(result);
        Assert.Null(owner.Pet);
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
        public PetOwner PetOwnerRef { get; set; } = null!;
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

    public sealed class Cat : IPet
    {
        public string? Name { get; set; }

        public int Lives { get; set; }
    }

    public sealed class Rock
    {
        public int Weight { get; set; }
    }

    [ReferenceResolver(EntityResolver = nameof(GetAsync))]
    public sealed class PetOwner
    {
        [Key]
        public string Id { get; set; } = null!;

        [External]
        public IPet? Pet { get; private set; }

        [Requires("pet { name }")]
        public string? Description => Pet is null ? null : $"owns {Pet.Name}";

        public static Task<PetOwner> GetAsync(string id)
            => Task.FromResult(new PetOwner { Id = id });
    }
}
