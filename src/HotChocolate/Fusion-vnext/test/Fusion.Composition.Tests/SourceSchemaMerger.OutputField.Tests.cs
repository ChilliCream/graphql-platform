using HotChocolate.Fusion.Options;
using HotChocolate.Skimmed.Serialization;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerOutputFieldTests : CompositionTestBase
{
    [Theory]
    [MemberData(nameof(ExamplesData))]
    public void Examples(string[] sdl, string executionSchema)
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(sdl),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        // act
        var result = merger.Merge();

        // assert
        Assert.True(result.IsSuccess);
        SchemaFormatter.FormatAsString(result.Value).MatchInlineSnapshot(executionSchema);
    }

    public static TheoryData<string[], string> ExamplesData()
    {
        return new TheoryData<string[], string>
        {
            // Imagine two schemas with a "discountPercentage" field on a "Product" type that
            // slightly differ in return type.
            {
                [
                    """"
                    # Schema A
                    type Product {
                        """
                        Computes a discount as a percentage of the product's list price.
                        """
                        discountPercentage(percent: Int = 10): Int!
                    }
                    """",
                    """
                    # Schema B
                    type Product {
                        discountPercentage(percent: Int): Int
                    }
                    """
                ],
                """
                type Product
                    @fusion__type(schema: A)
                    @fusion__type(schema: B) {
                    "Computes a discount as a percentage of the product's list price."
                    discountPercentage(percent: Int = 10
                        @fusion__inputField(schema: A)
                        @fusion__inputField(schema: B)): Int
                        @fusion__field(schema: A, sourceType: "Int!")
                        @fusion__field(schema: B)
                }
                """
            },
            // If the argument is missing in one of the schemas, the composed field will not include
            // that argument.
            {
                [
                    """
                    # Schema A
                    type Product {
                        discountPercentage(percent: Int): Int
                    }
                    """,
                    """
                    # Schema B
                    type Product {
                        discountPercentage: Int
                    }
                    """
                ],
                """
                type Product
                    @fusion__type(schema: A)
                    @fusion__type(schema: B) {
                    discountPercentage: Int
                        @fusion__field(schema: A)
                        @fusion__field(schema: B)
                }
                """
            },
            // In case one argument is marked with @inaccessible, the composed field will not
            // include that argument. Note: The argument will be included as inaccessible in the
            // execution schema.
            {
                [
                    """
                    # Schema A
                    type Product {
                        discountPercentage(percent: Int): Int
                    }
                    """,
                    """
                    # Schema B
                    type Product {
                        discountPercentage(percent: Int @inaccessible): Int
                    }
                    """
                ],
                """
                type Product
                    @fusion__type(schema: A)
                    @fusion__type(schema: B) {
                    discountPercentage(percent: Int
                        @inaccessible
                        @fusion__inputField(schema: A)
                        @fusion__inputField(schema: B)): Int
                        @fusion__field(schema: A)
                        @fusion__field(schema: B)
                }
                """
            },
            // In case a schema defines a requirement through the @require directive, the composed
            // field will not include that argument.
            {
                [
                    """
                    # Schema A
                    type Product {
                        discountPercentage(percent: Int): Int
                    }
                    """,
                    """
                    # Schema B
                    type Product {
                        discountPercentage(percent: Int @require(field: "percent")): Int
                    }
                    """
                ],
                """
                type Product
                    @fusion__type(schema: A)
                    @fusion__type(schema: B) {
                    discountPercentage: Int
                        @fusion__field(schema: A)
                        @fusion__field(schema: B)
                        @fusion__requires(schema: B, field: "discountPercentage(percent: Int): Int", map: [ "percent" ])
                }
                """
            },
            // Any field marked with @internal is removed from consideration before merging begins.
            // This ensures that internal fields do not appear in the final composed schema and also
            // do not affect the merging process. Internal fields are intended for internal use only
            // and are not part of the composed schema and can collide in their definitions.
            {
                [
                    """
                    # Schema A
                    type Product {
                        discountPercentage: Int
                    }
                    """,
                    """
                    # Schema B
                    type Product {
                        discountPercentage: Int @internal
                    }
                    """
                ],
                """
                type Product
                    @fusion__type(schema: A)
                    @fusion__type(schema: B) {
                    discountPercentage: Int
                        @fusion__field(schema: A)
                }
                """
            },
            // In the case where all fields are marked with @internal, the field will not appear in
            // the composed schema.
            {
                [
                    """
                    # Schema A
                    type Product {
                        discountPercentage: Int @internal
                        name: String!
                    }
                    """,
                    """
                    # Schema B
                    type Product {
                        discountPercentage: Int @internal
                    }
                    """
                ],
                """
                type Product
                    @fusion__type(schema: A)
                    @fusion__type(schema: B) {
                    name: String!
                        @fusion__field(schema: A)
                }
                """
            },
            // If any of the fields is marked as @inaccessible, then the merged field is also marked
            // as @inaccessible in the execution schema.
            {
                [
                    """
                    # Schema A
                    type Product {
                        discountPercentage: Int
                    }
                    """,
                    """
                    # Schema B
                    type Product {
                        discountPercentage: Int @inaccessible
                    }
                    """
                ],
                """
                type Product
                    @fusion__type(schema: A)
                    @fusion__type(schema: B) {
                    discountPercentage: Int
                        @inaccessible
                        @fusion__field(schema: A)
                        @fusion__field(schema: B)
                }
                """
            },
            // @provides/@external
            {
                [
                    """
                    type Review {
                        id: ID!
                        body: String!
                        author: User @provides(fields: "email")
                    }

                    type User @key(fields: "id") {
                        id: ID!
                        email: String! @external
                        name: String!
                    }

                    type Query {
                        reviews: [Review!]
                        users: [User!]
                    }
                    """
                ],
                """
                schema {
                    query: Query
                }

                type Query
                    @fusion__type(schema: A) {
                    reviews: [Review!]
                        @fusion__field(schema: A)
                    users: [User!]
                        @fusion__field(schema: A)
                }

                type Review
                    @fusion__type(schema: A) {
                    author: User
                        @fusion__field(schema: A, provides: "email")
                    body: String!
                        @fusion__field(schema: A)
                    id: ID!
                        @fusion__field(schema: A)
                }

                type User
                    @fusion__type(schema: A) {
                    email: String!
                        @fusion__field(schema: A, external: true)
                    id: ID!
                        @fusion__field(schema: A)
                    name: String!
                        @fusion__field(schema: A)
                }
                """
            }
        };
    }
}
