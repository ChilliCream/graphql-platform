using System.Threading.Tasks;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.ApolloFederation.Properties.FederationResources;

namespace HotChocolate.ApolloFederation;

public class EntityTypeTests
{
    [Fact]
    public async Task TestEntityTypeCodeFirstNoEntities_ShouldThrow()
    {
        async Task CreateSchema()
        {
            // arrange
            await new ServiceCollection()
                .AddGraphQL()
                .AddApolloFederation()
                .AddQueryType<Query<Address>>()
                .BuildSchemaAsync();
        }

        var exception = await Assert.ThrowsAsync<SchemaException>(CreateSchema);
        Assert.Contains(ThrowHelper_EntityType_NoEntities, exception.Message);
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
        var entityType = schema.GetType<_EntityType>("_Entity");

        // assert
        Assert.Collection(entityType.Types.Values, t => Assert.Equal("Review", t.Name));
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
        var entityType = schema.GetType<_EntityType>("_Entity");

        // assert
        Assert.Collection(
            entityType.Types.Values,
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
        var entityType = schema.GetType<_EntityType>("_Entity");

        // assert
        Assert.Collection(
            entityType.Types.Values,
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
        var entityType = schema.GetType<_EntityType>("_Entity");

        // assert
        Assert.Collection(entityType.Types.Values,
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
        public string IdCode { get; set; } = default!;
        public Review[] Reviews { get; set; } = default!;
    }

    public sealed class UserWithPropertyAttributes
    {
        [Key]
        public int Id { get; set; }
        [Key]
        public string IdCode { get; set; } = default!;
    }

    [Key("id address { matchCode }")]
    public sealed class UserWithNestedKeyClassAttribute
    {
        public int Id { get; set; }
        public Address Address { get; set; } = default!;
    }

    public sealed class Address
    {
        public string MatchCode { get; set; } = default!;
    }

    [Key("id")]
    public sealed class Review
    {
        public int Id { get; set; }
    }
}
