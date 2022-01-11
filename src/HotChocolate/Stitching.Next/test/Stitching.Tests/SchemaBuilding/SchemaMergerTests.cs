
using System.Linq;
using HotChocolate.Language;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Stitching.SchemaBuilding;

public class SchemaMergerTests
{
    [Fact]
    public void Merge_Two_Schemas()
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
        Assert.Equal("Merged", mergedSchemaInfo.Name);
        Assert.NotNull(mergedSchemaInfo.Query);
        Assert.Null(mergedSchemaInfo.Mutation);
        Assert.Null(mergedSchemaInfo.Subscription);

        Assert.Collection(
            mergedSchemaInfo.Types.Values.OrderBy(t => t.Name.Value),
            t =>
            {
                Assert.Equal("Product", t.Name.Value);
                Assert.Empty(Assert.IsType<ObjectTypeInfo>(t).Fetchers);
            },
            t =>
            {
                Assert.Equal("Review", t.Name.Value);
                Assert.Collection(
                    Assert.IsType<ObjectTypeInfo>(t).Fetchers,
                    f =>
                    {
                        Assert.Equal("Reviews", f.Source);
                        var field = Assert.IsType<FieldDefinitionNode>(f.Selections);
                        Assert.Equal("reviewsByAuthor", field.Name.Value);
                        Assert.Equal("authorId", f.Arguments.Single().Name);
                        Assert.Equal("ID!", f.Arguments.Single().Type.ToString());
                        Assert.Equal("User.id", f.Arguments.Single().Binding.ToString());
                    },
                    f =>
                    {
                        Assert.Equal("Reviews", f.Source);
                        var field = Assert.IsType<FieldDefinitionNode>(f.Selections);
                        Assert.Equal("reviewsByProduct", field.Name.Value);
                        Assert.Equal("upc", f.Arguments.Single().Name);
                        Assert.Equal("ID!", f.Arguments.Single().Type.ToString());
                        Assert.Equal("Product.upc", f.Arguments.Single().Binding.ToString());
                    });
            },
            t =>
            {
                Assert.Equal("User", t.Name.Value);
                Assert.Collection(
                    Assert.IsType<ObjectTypeInfo>(t).Fetchers,
                    f =>
                    {
                        Assert.Equal("Accounts", f.Source);
                        var field = Assert.IsType<FieldDefinitionNode>(f.Selections);
                        Assert.Equal("userById", field.Name.Value);
                        Assert.Equal("id", f.Arguments.Single().Name);
                        Assert.Equal("ID!", f.Arguments.Single().Type.ToString());
                        Assert.Equal("User.id", f.Arguments.Single().Binding.ToString());
                    });
            });

        Assert.Collection(
            mergedSchemaInfo.Query.Bindings,
            t =>
            {
                Assert.Equal("Accounts", t.Source);

                Assert.Collection(
                    t.Fields, 
                    f => Assert.Equal("users", f), 
                    f => Assert.Equal("userById", f));
            },
            t =>
            {
                Assert.Equal("Reviews", t.Source);

                Assert.Collection(
                    t.Fields, 
                    f => Assert.Equal("reviews", f),
                    f => Assert.Equal("reviewsByAuthor", f), 
                    f => Assert.Equal("reviewsByProduct", f));
            });

        Assert.Collection(
            ((ObjectTypeInfo)mergedSchemaInfo.Types["User"]).Bindings,
            t =>
            {
                Assert.Equal("Accounts", t.Source);

                Assert.Collection(
                    t.Fields, 
                    f => Assert.Equal("id", f), 
                    f => Assert.Equal("name", f), 
                    f => Assert.Equal("username", f), 
                    f => Assert.Equal("birthdate", f));
            },
            t =>
            {
                Assert.Equal("Reviews", t.Source);

                Assert.Collection(
                    t.Fields, 
                    f => Assert.Equal("id", f),
                    f => Assert.Equal("name", f));
            });

        mergedSchemaInfo.ToSchemaDocument().ToString()
            .MatchSnapshot(new SnapshotNameExtension("Merged"));
    }
}
