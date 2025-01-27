using HotChocolate.Fusion.Options;
using HotChocolate.Skimmed.Serialization;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerUnionTests : CompositionTestBase
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
        var result = merger.MergeSchemas();

        // assert
        Assert.True(result.IsSuccess);
        SchemaFormatter.FormatAsString(result.Value).MatchInlineSnapshot(executionSchema);
    }

    public static TheoryData<string[], string> ExamplesData()
    {
        return new TheoryData<string[], string>
        {
            // Here, two "SearchResult" union types from different schemas are merged into a single
            // composed "SearchResult" type.
            {
                [
                    """
                    # Schema A
                    union SearchResult = Product | Order

                    type Product { id: ID! }
                    type Order { id: ID! }
                    """,
                    """
                    # Schema B
                    union SearchResult = User | Order

                    type User { id: ID! }
                    type Order { id: ID! }
                    """
                ],
                """
                type Order
                    @fusion__type(schema: A)
                    @fusion__type(schema: B) {
                    id: ID!
                        @fusion__field(schema: A)
                        @fusion__field(schema: B)
                }

                type Product
                    @fusion__type(schema: A) {
                    id: ID!
                        @fusion__field(schema: A)
                }

                type User
                    @fusion__type(schema: B) {
                    id: ID!
                        @fusion__field(schema: B)
                }

                union SearchResult
                    @fusion__type(schema: A)
                    @fusion__type(schema: B)
                    @fusion__unionMember(schema: A, member: "Order")
                    @fusion__unionMember(schema: B, member: "Order") = Order
                """
            },
            // If any of the unions is marked as @inaccessible, then the merged union is also marked
            // as @inaccessible in the execution schema.
            {
                [
                    """
                    # Schema A
                    union SearchResult = User

                    type User { id: ID! }
                    """,
                    """
                    # Schema B
                    union SearchResult @inaccessible = User

                    type User { id: ID! }
                    """
                ],
                """
                type User
                    @fusion__type(schema: A)
                    @fusion__type(schema: B) {
                    id: ID!
                        @fusion__field(schema: A)
                        @fusion__field(schema: B)
                }

                union SearchResult
                    @inaccessible
                    @fusion__type(schema: A)
                    @fusion__type(schema: B)
                    @fusion__unionMember(schema: A, member: "User")
                    @fusion__unionMember(schema: B, member: "User") = User
                """
            },
            // The first non-empty description that is found is used as the description for the
            // merged union.
            {
                [
                    """
                    # Schema A
                    union SearchResult = User

                    type User { id: ID! }
                    """,
                    """
                    # Schema B
                    "The first non-empty description."
                    union SearchResult = User

                    type User { id: ID! }
                    """
                ],
                """
                type User
                    @fusion__type(schema: A)
                    @fusion__type(schema: B) {
                    id: ID!
                        @fusion__field(schema: A)
                        @fusion__field(schema: B)
                }

                "The first non-empty description."
                union SearchResult
                    @fusion__type(schema: A)
                    @fusion__type(schema: B)
                    @fusion__unionMember(schema: A, member: "User")
                    @fusion__unionMember(schema: B, member: "User") = User
                """
            },
            // Each union's possible types are considered in turn. Only those that are not marked
            // @internal are included in the final composed union. This preserves the valid types
            // from all sources while systematically filtering out anything intended for internal
            // use only. In case there are no possible types left after filtering, the merged union
            // is considered @internal and cannot appear in the final schema.
            {
                [
                    """
                    # Schema A
                    union SearchResult = User

                    type User @internal { id: ID! }
                    """,
                    """
                    # Schema B
                    union SearchResult = User

                    type User @internal { id: ID! }
                    """
                ],
                ""
            },
            // Union member type "User" internal in one of two schemas. No remaining member types.
            {
                [
                    """
                    # Schema A
                    union SearchResult = User

                    type User { id: ID! }
                    """,
                    """
                    # Schema B
                    union SearchResult = User

                    type User @internal { id: ID! }
                    """
                ],
                """
                type User
                    @fusion__type(schema: A) {
                    id: ID!
                        @fusion__field(schema: A)
                }
                """
            },
            // Union member type "Order" internal in one of two schemas, "Product" visible in both.
            {
                [
                    """
                    # Schema A
                    union SearchResult = Product | Order

                    type Product { id: ID! }
                    type Order { id: ID! }
                    """,
                    """
                    # Schema B
                    union SearchResult = Product | Order

                    type Product { id: ID! }
                    type Order @internal { id: ID! }
                    """
                ],
                """
                type Order
                    @fusion__type(schema: A) {
                    id: ID!
                        @fusion__field(schema: A)
                }

                type Product
                    @fusion__type(schema: A)
                    @fusion__type(schema: B) {
                    id: ID!
                        @fusion__field(schema: A)
                        @fusion__field(schema: B)
                }

                union SearchResult
                    @fusion__type(schema: A)
                    @fusion__type(schema: B)
                    @fusion__unionMember(schema: A, member: "Product")
                    @fusion__unionMember(schema: B, member: "Product") = Product
                """
            },
            // @lookup
            {
                [
                    """
                    # Schema A
                    type Query {
                        animalById(id: ID!): Animal @lookup
                    }

                    union Animal = Dog | Cat

                    type Dog { id: ID! }
                    type Cat { id: ID! }
                    """
                ],
                """
                type Cat
                    @fusion__type(schema: A) {
                    id: ID!
                        @fusion__field(schema: A)
                }

                type Dog
                    @fusion__type(schema: A) {
                    id: ID!
                        @fusion__field(schema: A)
                }

                type Query
                    @fusion__type(schema: A) {
                    animalById(id: ID!
                        @fusion__inputField(schema: A)): Animal
                        @fusion__field(schema: A)
                }

                union Animal
                    @fusion__type(schema: A)
                    @fusion__unionMember(schema: A, member: "Dog")
                    @fusion__unionMember(schema: A, member: "Cat")
                    @fusion__lookup(schema: A, key: "id", field: "animalById(id: ID!): Animal", map: [ "id" ], path: null) = Dog | Cat
                """
            },
            // @lookup on union member type fields.
            {
                [
                    """
                    # Schema A
                    type Query {
                        animalById(id: ID!): Animal @lookup
                    }

                    union Animal = Dog | Cat

                    type Dog {
                        dogById(id: ID!): Dog @lookup
                    }

                    type Cat {
                        catById(id: ID!): Cat @lookup
                    }
                    """
                ],
                """
                type Cat
                    @fusion__type(schema: A)
                    @fusion__lookup(schema: A, key: "id", field: "catById(id: ID!): Cat", map: [ "id" ], path: "animalById") {
                    catById(id: ID!
                        @fusion__inputField(schema: A)): Cat
                        @fusion__field(schema: A)
                }

                type Dog
                    @fusion__type(schema: A)
                    @fusion__lookup(schema: A, key: "id", field: "dogById(id: ID!): Dog", map: [ "id" ], path: "animalById") {
                    dogById(id: ID!
                        @fusion__inputField(schema: A)): Dog
                        @fusion__field(schema: A)
                }

                type Query
                    @fusion__type(schema: A) {
                    animalById(id: ID!
                        @fusion__inputField(schema: A)): Animal
                        @fusion__field(schema: A)
                }

                union Animal
                    @fusion__type(schema: A)
                    @fusion__unionMember(schema: A, member: "Dog")
                    @fusion__unionMember(schema: A, member: "Cat")
                    @fusion__lookup(schema: A, key: "id", field: "animalById(id: ID!): Animal", map: [ "id" ], path: null) = Dog | Cat
                """
            }
        };
    }
}
