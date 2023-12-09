using static HotChocolate.ApolloFederation.Properties.FederationResources;

namespace HotChocolate.ApolloFederation;

public class EntityTypeTests
{
    [Fact]
    public void TestEntityTypeSchemaFirstSingleKey()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddApolloFederation()
            .AddDocumentFromString(
                """
                type Query {
                    user(a: Int!): User
                }

                type Review @key(fields: "id") {
                    id: Int!
                    author: User
                }

                type User @key(fields: "id") {
                    id: Int!
                    idCode: String!
                    reviews: [Review!]!
                }
                """)
            .Use(_ => _ => default)
            .Create();

        // act
        var entityType = schema.GetType<EntityType>("_Entity");

        // assert
        Assert.Collection(entityType.Types.Values,
            t => Assert.Equal("Review", t.Name),
            t => Assert.Equal("User", t.Name));
    }

    [Fact]
    public void TestEntityTypeSchemaFirstMultiKey()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddApolloFederation()
            .AddDocumentFromString(
                """
                type Query {
                    user(a: Int!): User
                }

                type User @key(fields: "id idCode") {
                    id: Int!
                    idCode: String!
                }
                """)
            .Use(_ => _ => default)
            .Create();

        // act
        var entityType = schema.GetType<EntityType>("_Entity");

        // assert
        Assert.Collection(entityType.Types.Values, t => Assert.Equal("User", t.Name));
    }

    [Fact]
    public void TestEntityTypeSchemaFirstNestedKey()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddApolloFederation()
            .AddDocumentFromString(
                """
                    type Query {
                        user(a: Int!): User
                    }

                    type User @key(fields: ""id address { matchCode }"") {
                        id: Int!
                        address: Address
                    }

                    type Address {
                        matchCode: String!
                    }
                """)
            .Use(_ => _ => default)
            .Create();

        // act
        var entityType = schema.GetType<EntityType>("_Entity");

        // assert
        Assert.Collection(entityType.Types.Values, t => Assert.Equal("User", t.Name));
    }

    [Fact]
    public void TestEntityTypeCodeFirstNoEntities_ShouldThrow()
    {
        void CreateSchema()
        {
            // arrange
            SchemaBuilder.New()
                .AddApolloFederation()
                .AddQueryType<Query<Address>>()
                .Create();
        }

        var exception = Assert.Throws<SchemaException>(CreateSchema);
        Assert.Contains(ThrowHelper_EntityType_NoEntities, exception.Message);
    }

    [Fact]
    public void TestEntityTypeCodeFirstClassKeyAttributeSingleKey()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddApolloFederation()
            .AddQueryType<Query<Review>>()
            .Create();

        // act
        var entityType = schema.GetType<EntityType>("_Entity");

        // assert
        Assert.Collection(entityType.Types.Values, t => Assert.Equal("Review", t.Name));
    }

    [Fact]
    public void TestEntityTypeCodeFirstClassKeyAttributeMultiKey()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddApolloFederation()
            .AddQueryType<Query<UserWithClassAttribute>>()
            .Create();

        // act
        var entityType = schema.GetType<EntityType>("_Entity");

        // assert
        Assert.Collection(
            entityType.Types.Values,
            t => Assert.Equal("UserWithClassAttribute", t.Name),
            t => Assert.Equal("Review", t.Name));
    }

    [Fact]
    public void TestEntityTypeCodeFirstPropertyKeyAttributes()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddApolloFederation()
            .AddQueryType<Query<UserWithPropertyAttributes>>()
            .Create();

        // act
        var entityType = schema.GetType<EntityType>("_Entity");

        // assert
        Assert.Collection(
            entityType.Types.Values,
            t => Assert.Equal("UserWithPropertyAttributes", t.Name));
    }

    [Fact]
    public void TestEntityTypeCodeFirstClassKeyAttributeNestedKey()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddApolloFederation()
            .AddQueryType<Query<UserWithNestedKeyClassAttribute>>()
            .Create();

        // act
        var entityType = schema.GetType<EntityType>("_Entity");

        // assert
        Assert.Collection(entityType.Types.Values,
            t => Assert.Equal("UserWithNestedKeyClassAttribute", t.Name));
    }
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
