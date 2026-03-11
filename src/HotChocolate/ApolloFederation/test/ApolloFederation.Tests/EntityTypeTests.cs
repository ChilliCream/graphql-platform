using HotChocolate.ApolloFederation.Types;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.ApolloFederation;

public class EntityTypeTests
{
    [Fact]
    public async Task TestEntityTypeCodeFirstNoEntities_ShouldOmitEntityField()
    {
        // arrange
        var schema = await new ServiceCollection()
                .AddGraphQL()
                .AddApolloFederation()
                .AddQueryType<Query<Address>>()
                .BuildSchemaAsync();

        // act/assert
        Assert.False(schema.Types.TryGetType<_EntityType>("_Entity", out _));
    }

    [Fact]
    public async Task TestEntityTypeCodeFirstClassKeyAttributeSingleKey()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query<Review>>()
            .BuildSchemaAsync();

        // act
        var entityType = schema.Types.GetType<_EntityType>("_Entity");

        // assert
        Assert.Collection(entityType.Types, t => Assert.Equal("Review", t.Name));
    }

    [Fact]
    public async Task TestEntityTypeCodeFirstClassKeyAttributeMultiKey()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query<UserWithClassAttribute>>()
            .BuildSchemaAsync();

        // act
        var entityType = schema.Types.GetType<_EntityType>("_Entity");

        // assert
        Assert.Collection(
            entityType.Types,
            t => Assert.Equal("UserWithClassAttribute", t.Name),
            t => Assert.Equal("Review", t.Name));
    }

    [Fact]
    public async Task TestEntityTypeCodeFirstPropertyKeyAttributes()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query<UserWithPropertyAttributes>>()
            .BuildSchemaAsync();

        // act
        var entityType = schema.Types.GetType<_EntityType>("_Entity");

        // assert
        Assert.Collection(
            entityType.Types,
            t => Assert.Equal("UserWithPropertyAttributes", t.Name));
    }

    [Fact]
    public async Task TestEntityTypeCodeFirstClassKeyAttributeNestedKey()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query<UserWithNestedKeyClassAttribute>>()
            .BuildSchemaAsync();

        // act
        var entityType = schema.Types.GetType<_EntityType>("_Entity");

        // assert
        Assert.Collection(entityType.Types,
            t => Assert.Equal("UserWithNestedKeyClassAttribute", t.Name));
    }

    public sealed class Query<T>
    {
        public T GetEntity(int id) => default!;
    }

    [Key("id idCode")]
    public sealed class UserWithClassAttribute
    {
        public int Id { get; set; }
        public string IdCode { get; set; } = null!;
        public Review[] Reviews { get; set; } = null!;
    }

    public sealed class UserWithPropertyAttributes
    {
        [Key]
        public int Id { get; set; }
        [Key]
        public string IdCode { get; set; } = null!;
    }

    [Key("id address { matchCode }")]
    public sealed class UserWithNestedKeyClassAttribute
    {
        public int Id { get; set; }
        public Address Address { get; set; } = null!;
    }

    public sealed class Address
    {
        public string MatchCode { get; set; } = null!;
    }

    [Key("id")]
    public sealed class Review
    {
        public int Id { get; set; }
    }
}
