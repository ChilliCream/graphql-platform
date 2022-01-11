
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Stitching.SchemaBuilding;

namespace HotChocolate.Stitching.Execution;

internal static class TestHelper
{
    public static MergedSchema CreateSchemaInfo()
    {
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
                reviewsById(ids: [ID!] @is(a: ""Review.id"")): [Review!]
                # reviewsByAuthor(authorId: ID! @is(a: ""User.id"")): [Review!]! @internal
                # reviewsByProduct(upc: ID! @is(a: ""Product.upc"")): [Review!]! @internal
                productById(upc: ID! @is(a: ""Product.upc"")): Product
            }

            # reviewsById(ids: [ID!] @is(a: ""User.id"")): [Review!]
            # reviewsById all is must point to review and must return Review
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
                upc: ID
                reviews: [Review!]!
            }";

        var inspector = new SchemaInspector();
        var schemaInfoA = inspector.Inspect(Utf8GraphQLParser.Parse(schemaA));
        var schemaInfoB = inspector.Inspect(Utf8GraphQLParser.Parse(schemaB));

        var schemaNameA = schemaInfoA.Name;
        var schemaNameB = schemaInfoB.Name;

        var merger = new SchemaMerger();
        var mergedSchemaInfo = merger.Merge(new[] { schemaInfoA, schemaInfoB });

        return new MergedSchema(
            new List<NameString>
            {
                schemaNameA,
                schemaNameB
            },
            mergedSchemaInfo);
    }

    internal class MergedSchema
    {
        public MergedSchema(List<NameString> sources, SchemaInfo schemaInfo)
        {
            Sources = sources;
            SchemaInfo = schemaInfo;
        }

        public List<NameString> Sources { get; }

        public SchemaInfo SchemaInfo { get; }
    }
}
