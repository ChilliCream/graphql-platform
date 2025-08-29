using HotChocolate.Fusion.Options;
using HotChocolate.Types.Mutable.Serialization;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerGlobalObjectIdentificationTests
{
    [Theory]
    [MemberData(nameof(ExamplesData))]
    public void Examples(string[] sdl, bool enableGlobalObjectIdentification, string executionSchema)
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(sdl),
            new SourceSchemaMergerOptions
            {
                RemoveUnreferencedTypes = false,
                AddFusionDefinitions = false,
                EnableGlobalObjectIdentification = enableGlobalObjectIdentification
            });

        // act
        var result = merger.Merge();

        // assert
        Assert.True(result.IsSuccess);
        SchemaFormatter.FormatAsString(result.Value).MatchInlineSnapshot(executionSchema);
    }

    public static TheoryData<string[], bool, string> ExamplesData()
    {
        return new TheoryData<string[], bool, string>
        {
            // Node interface exists and option is set to true
            {
                [
                    """
                    # Schema A
                    type Query {
                        node(id: ID!): Node @lookup
                        nodes(ids: [ID!]!): [Node]!
                    }

                    interface Node {
                        id: ID!
                    }

                    type Product implements Node {
                        id: ID!
                    }
                    """
                ],
                true,
                """
                schema {
                  query: Query
                }

                type Query
                  @fusion__type(schema: A) {
                  node(id: ID!): Node
                }

                type Product implements Node
                  @fusion__type(schema: A)
                  @fusion__implements(schema: A, interface: "Node") {
                  id: ID!
                    @fusion__field(schema: A)
                }

                interface Node
                  @fusion__type(schema: A)
                  @fusion__lookup(schema: A, key: "id", field: "node(id: ID!): Node", map: [ "id" ], path: null, internal: false) {
                  id: ID!
                    @fusion__field(schema: A)
                }
                """
            },
            // Node interface doesn't exist and option is set to true
            {
                [
                    """
                    # Schema A
                    type Query {
                        node: SomethingElse
                    }

                    type SomethingElse {
                        id: ID!
                    }
                    """
                ],
                true,
                """
                schema {
                  query: Query
                }

                type Query
                  @fusion__type(schema: A) {
                  node: SomethingElse
                    @fusion__field(schema: A)
                }

                type SomethingElse
                  @fusion__type(schema: A) {
                  id: ID!
                    @fusion__field(schema: A)
                }
                """
            },
            // Node interface exists and option is set to false
            {
                [
                    """
                    # Schema A
                    type Query {
                        node(id: ID!): Node
                        nodes(ids: [ID!]!): [Node]!
                    }

                    interface Node {
                        id: ID!
                    }

                    type Product implements Node {
                        id: ID!
                    }
                    """
                ],
                false,
                """
                schema {
                  query: Query
                }

                type Query
                  @fusion__type(schema: A) {

                }

                type Product implements Node
                  @fusion__type(schema: A)
                  @fusion__implements(schema: A, interface: "Node") {
                  id: ID!
                    @fusion__field(schema: A)
                }

                interface Node
                  @fusion__type(schema: A) {
                  id: ID!
                    @fusion__field(schema: A)
                }
                """
            }
        };
    }
}
