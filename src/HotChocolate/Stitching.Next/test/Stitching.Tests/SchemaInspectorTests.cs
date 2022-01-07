
using System.Linq;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Stitching.SchemaBuilding;

public class SchemaInspectorTests
{
    [Fact]
    public void Single_Fetchers_Are_Correctly_Discoverd()
    {
        // arrange
        var schema =
            @"schema @schema(name: ""Persons"") { query: Query }

            type Query {
                personById(id: ID! @is(a: ""Person.id"")) : Person
            }
            
            type Person {
                id: ID!
                name: String!
            }";

        // act
        var inspector = new SchemaInspector();
        var schemaInfo = inspector.Inspect(Utf8GraphQLParser.Parse(schema));

        // assert
        Assert.Equal("Persons", schemaInfo.Name);
        Assert.NotNull(schemaInfo.Query);
        Assert.Null(schemaInfo.Mutation);
        Assert.Null(schemaInfo.Subscription);

        Assert.Collection(
            schemaInfo.Types.Values,
            t =>
            {
                Assert.Equal("Person", t.Name.Value);
                Assert.Collection(
                    Assert.IsType<ObjectTypeInfo>(t).Fetchers,
                    f =>
                    {
                        Assert.Equal("id", f.Arguments.Single().Name);
                        Assert.Equal("ID!", f.Arguments.Single().Type.ToString());
                        Assert.Equal("Person.id", f.Arguments.Single().Binding.ToString());
                    });
            });
    }

    [Fact]
    public void Multiple_Fetchers_Are_Correctly_Discoverd()
    {
        // arrange
        var schema =
            @"schema @schema(name: ""Persons"") { query: Query }

            type Query {
                personById(id: ID! @is(a: ""Person.id"")) : Person
                personByAccount(accountId: ID! @is(a: ""Account.id"")) : [Person!]
            }
            
            type Person {
                id: ID!
                name: String!
            }";

        // act
        var inspector = new SchemaInspector();
        var schemaInfo = inspector.Inspect(Utf8GraphQLParser.Parse(schema));

        // assert
        Assert.Equal("Persons", schemaInfo.Name);
        Assert.NotNull(schemaInfo.Query);
        Assert.Null(schemaInfo.Mutation);
        Assert.Null(schemaInfo.Subscription);

        Assert.Collection(
            schemaInfo.Types.Values,
            t =>
            {
                Assert.Equal("Person", t.Name.Value);
                Assert.Collection(
                    Assert.IsType<ObjectTypeInfo>(t).Fetchers,
                    f =>
                    {
                        Assert.Equal("id", f.Arguments.Single().Name);
                        Assert.Equal("ID!", f.Arguments.Single().Type.ToString());
                        Assert.Equal("Person.id", f.Arguments.Single().Binding.ToString());
                    },
                    f =>
                    {
                        Assert.Equal("accountId", f.Arguments.Single().Name);
                        Assert.Equal("ID!", f.Arguments.Single().Type.ToString());
                        Assert.Equal("Account.id", f.Arguments.Single().Binding.ToString());
                    });
            });
    }
}

public class SchemaMergerTests
{
    [Fact]
    public void Single_Fetchers_Are_Correctly_Discoverd()
    {
        // arrange
        var schemaA =
            @"schema @schema(name: ""Accounts"") {
                query: Query
            }

            type Query {
                users: [User!]!
                userById(id: ID! @is(a: ""User.id"")): User! @internal
            }

            type User {
                id: ID!
                name: String!
                username: String!
                birthdate: DateTime!
            }";

        var schemaB =
            @"schema @schema(name: ""Reviews"") {
                query: Query
            }

            type Query {
                reviews: [Review!]!
                reviewsByAuthor(authorId: ID! @is(a: ""User.id"")): [Review!]! @internal
                reviewsByProduct(upc: ID! @is(a: ""Product.upc"")): [Review!]! @internal
            }

            type Review {
                id: ID!
                user: User!
                product: Product!
                body: String!
            }
            
            type User {
                id: ID!
                name: String!
            }
            
            type Product {
                upc: ID!
            }";

        var inspector = new SchemaInspector();
        var schemaInfoA = inspector.Inspect(Utf8GraphQLParser.Parse(schemaA));
        var schemaInfoB = inspector.Inspect(Utf8GraphQLParser.Parse(schemaB));

        // act
        var merger = new SchemaMerger();
        var mergedSchemaInfo = merger.Merge(new[] { schemaInfoA, schemaInfoB });

        // assert
        mergedSchemaInfo.ToSchemaDocument().ToString().MatchSnapshot();
    }
}