using HotChocolate.Fusion.Options;
using HotChocolate.Types.Mutable.Serialization;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerInterfaceTests
{
    [Theory]
    [MemberData(nameof(ExamplesData))]
    public void Examples(string[] sdl, string executionSchema)
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(sdl),
            new SourceSchemaMergerOptions
            {
                RemoveUnreferencedTypes = false,
                AddFusionDefinitions = false
            });

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
            // In this example, the "Product" interface type from two schemas is merged. The "id"
            // field is shared across both schemas, while "name" and "createdAt" fields are
            // contributed by the individual source schemas. The resulting composed type includes
            // all fields.
            {
                [
                    """
                    # Schema A
                    interface Product {
                        id: ID!
                        name: String
                    }
                    """,
                    """
                    # Schema B
                    interface Product {
                        id: ID!
                        createdAt: String
                    }
                    """
                ],
                """
                interface Product
                    @fusion__type(schema: A)
                    @fusion__type(schema: B) {
                    createdAt: String
                        @fusion__field(schema: B)
                    id: ID!
                        @fusion__field(schema: A)
                        @fusion__field(schema: B)
                    name: String
                        @fusion__field(schema: A)
                }
                """
            },
            // The following example shows how the description is retained when merging interface
            // types.
            {
                [
                    """"
                    # Schema A
                    """
                    First description
                    """
                    interface Product {
                        id: ID!
                    }
                    """",
                    """"
                    # Schema B
                    """
                    Second description
                    """
                    interface Product {
                        id: ID!
                    }
                    """"
                ],
                """
                "First description"
                interface Product
                    @fusion__type(schema: A)
                    @fusion__type(schema: B) {
                    id: ID!
                        @fusion__field(schema: A)
                        @fusion__field(schema: B)
                }
                """
            },
            // If any of the interfaces is marked as @inaccessible, then the merged interface is
            // also marked as @inaccessible in the execution schema.
            {
                [
                    """
                    # Schema A
                    interface Product {
                        id: ID!
                    }
                    """,
                    """
                    # Schema B
                    interface Product @inaccessible {
                        id: ID!
                    }
                    """
                ],
                """
                interface Product
                    @fusion__type(schema: A)
                    @fusion__type(schema: B)
                    @fusion__inaccessible {
                    id: ID!
                        @fusion__field(schema: A)
                        @fusion__field(schema: B)
                }
                """
            },
            // Implemented interfaces. I2 is inaccessible.
            {
                [
                    """
                    # Schema A
                    interface I1 {
                        id: ID!
                    }

                    interface I2 {
                        id: ID!
                    }

                    interface Product implements I1 & I2 {
                        id: ID!
                    }
                    """,
                    """
                    interface I1 {
                        id: ID!
                    }

                    interface I2 @inaccessible {
                        id: ID!
                    }

                    interface I3 {
                        id: ID!
                    }

                    interface Product implements I1 & I2 & I3 {
                        id: ID!
                    }
                    """
                ],
                """
                interface I1
                    @fusion__type(schema: A)
                    @fusion__type(schema: B) {
                    id: ID!
                        @fusion__field(schema: A)
                        @fusion__field(schema: B)
                }

                interface I2
                    @fusion__type(schema: A)
                    @fusion__type(schema: B)
                    @fusion__inaccessible {
                    id: ID!
                        @fusion__field(schema: A)
                        @fusion__field(schema: B)
                }

                interface I3
                    @fusion__type(schema: B) {
                    id: ID!
                        @fusion__field(schema: B)
                }

                interface Product implements I1 & I3
                    @fusion__type(schema: A)
                    @fusion__type(schema: B)
                    @fusion__implements(schema: A, interface: "I1")
                    @fusion__implements(schema: B, interface: "I1")
                    @fusion__implements(schema: B, interface: "I3") {
                    id: ID!
                        @fusion__field(schema: A)
                        @fusion__field(schema: B)
                }
                """
            },
            // @lookup
            {
                [
                    """
                    # Schema A
                    type Query {
                        productById(id: ID!): Product @lookup
                        productByName(name: String!): Product @lookup
                    }

                    interface Product @key(fields: "id") @key(fields: "name") {
                        id: ID!
                        name: String!
                    }
                    """
                ],
                """
                schema {
                    query: Query
                }

                type Query
                    @fusion__type(schema: A) {
                    productById(id: ID!
                        @fusion__inputField(schema: A)): Product
                        @fusion__field(schema: A)
                    productByName(name: String!
                        @fusion__inputField(schema: A)): Product
                        @fusion__field(schema: A)
                }

                interface Product
                    @fusion__type(schema: A)
                    @fusion__lookup(schema: A, key: "id", field: "productById(id: ID!): Product", map: [ "id" ], path: null, internal: false)
                    @fusion__lookup(schema: A, key: "name", field: "productByName(name: String!): Product", map: [ "name" ], path: null, internal: false) {
                    id: ID!
                        @fusion__field(schema: A)
                    name: String!
                        @fusion__field(schema: A)
                }
                """
            },
            // @lookup on fields of types implementing an interface.
            {
                [
                    """
                    # Schema A
                    type Query {
                        animalById(id: ID!): Animal @lookup
                    }

                    interface Animal {
                        id: ID!
                    }

                    type Dog implements Animal {
                        id: ID!
                        dogById(id: ID!): Dog @lookup
                    }

                    type Cat implements Animal {
                        id: ID!
                        catById(id: ID!): Cat @lookup
                    }
                    """
                ],
                """
                schema {
                    query: Query
                }

                type Query
                    @fusion__type(schema: A) {
                    animalById(id: ID!
                        @fusion__inputField(schema: A)): Animal
                        @fusion__field(schema: A)
                }

                type Cat implements Animal
                    @fusion__type(schema: A)
                    @fusion__implements(schema: A, interface: "Animal")
                    @fusion__lookup(schema: A, key: "id", field: "catById(id: ID!): Cat", map: [ "id" ], path: "animalById", internal: false) {
                    catById(id: ID!
                        @fusion__inputField(schema: A)): Cat
                        @fusion__field(schema: A)
                    id: ID!
                        @fusion__field(schema: A)
                }

                type Dog implements Animal
                    @fusion__type(schema: A)
                    @fusion__implements(schema: A, interface: "Animal")
                    @fusion__lookup(schema: A, key: "id", field: "dogById(id: ID!): Dog", map: [ "id" ], path: "animalById", internal: false) {
                    dogById(id: ID!
                        @fusion__inputField(schema: A)): Dog
                        @fusion__field(schema: A)
                    id: ID!
                        @fusion__field(schema: A)
                }

                interface Animal
                    @fusion__type(schema: A)
                    @fusion__lookup(schema: A, key: "id", field: "animalById(id: ID!): Animal", map: [ "id" ], path: null, internal: false) {
                    id: ID!
                        @fusion__field(schema: A)
                }
                """
            }
        };
    }
}
