using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;

namespace HotChocolate.Fusion;

public sealed class SchemaComposerTests
{
    [Fact]
    public void Compose_WithExtensions_AppliesExtensions()
    {
        // arrange
        var schemaComposer = new SchemaComposer(
            [
                new SourceSchemaText(
                    "A",
                    """
                    type Query {
                        productById1(id: ID!): Product
                        productById2(id: ID!): Product
                        lookups: InternalLookups!
                    }

                    extend type Query {
                        productById1(id: ID!): Product @lookup
                        productById2(id: ID!): Product @internal
                        lookups: InternalLookups! @internal
                    }

                    type Product {
                        id: ID!
                        hidden: Int
                    }

                    extend type Product {
                        sku: String!
                        hidden: Int @inaccessible
                    }

                    type InternalLookups {
                        productBySku(sku: ID!): Product
                    }

                    extend type InternalLookups @internal {
                        productBySku(sku: ID!): Product @lookup
                    }
                    """)
            ],
            new SchemaComposerOptions { Merger = { AddFusionDefinitions = false } },
            new CompositionLog());

        // act
        var result = schemaComposer.Compose();

        // assert
        Assert.True(result.IsSuccess);
        result.Value.MatchInlineSnapshot(
            """
            schema {
                query: Query
            }

            type Query @fusion__type(schema: A) {
                productById1(id: ID! @fusion__inputField(schema: A)): Product
                    @fusion__field(schema: A)
            }

            type Product
                @fusion__type(schema: A)
                @fusion__lookup(
                    schema: A
                    key: "id"
                    field: "productById1(id: ID!): Product"
                    map: ["id"]
                    path: null
                    internal: false
                )
                @fusion__lookup(
                    schema: A
                    key: "sku"
                    field: "productBySku(sku: ID!): Product"
                    map: ["sku"]
                    path: "lookups"
                    internal: true
                ) {
                hidden: Int @fusion__field(schema: A) @fusion__inaccessible
                id: ID! @fusion__field(schema: A)
                sku: String! @fusion__field(schema: A)
            }
            """);
    }
}
