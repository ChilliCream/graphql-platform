namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerGlobalObjectIdentificationTests : SourceSchemaMergerTestBase
{
    // Node interface exists and option is set to true.
    [Fact]
    public void Merge_GlobalObjectIdentificationEnabled_MatchesSnapshot()
    {
        AssertMatches(
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
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              node(id: ID!): Node @fusion__gateway_field
              nodes(ids: [ID!]!): [Node]! @fusion__gateway_field
            }

            type Product implements Node
              @fusion__type(schema: A)
              @fusion__implements(schema: A, interface: "Node") {
              id: ID! @fusion__field(schema: A)
            }

            interface Node
              @fusion__type(schema: A)
              @fusion__lookup(
                schema: A
                key: "id"
                field: "node(id: ID!): Node"
                map: ["id"]
                path: null
                internal: false
              ) {
              id: ID! @fusion__field(schema: A)
            }
            """,
            options =>
            {
                options.EnableGlobalObjectIdentification = true;
                options.AddNodesField = true;
            });
    }

    // Node interface exists, node field has NO @lookup, option is set to true.
    // A native (non-federation) node field is never inferred as a lookup, so the
    // composed Node interface carries no @fusion__lookup (only the Apollo transform
    // infers @lookup from a bare node field).
    [Fact]
    public void Merge_GlobalObjectIdentificationEnabledNodeFieldWithoutLookup_MatchesSnapshot()
    {
        AssertMatches(
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
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              node(id: ID!): Node @fusion__gateway_field
            }

            type Product implements Node
              @fusion__type(schema: A)
              @fusion__implements(schema: A, interface: "Node") {
              id: ID! @fusion__field(schema: A)
            }

            interface Node @fusion__type(schema: A) {
              id: ID! @fusion__field(schema: A)
            }
            """,
            options => options.EnableGlobalObjectIdentification = true);
    }

    // Node interface doesn't exist and option is set to true.
    [Fact]
    public void Merge_GlobalObjectIdentificationEnabledNoNodeInterface_MatchesSnapshot()
    {
        AssertMatches(
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
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              node: SomethingElse @fusion__field(schema: A)
            }

            type SomethingElse @fusion__type(schema: A) {
              id: ID! @fusion__field(schema: A)
            }
            """,
            options => options.EnableGlobalObjectIdentification = true);
    }

    // Node interface exists and option is set to false.
    [Fact]
    public void Merge_GlobalObjectIdentificationDisabled_MatchesSnapshot()
    {
        AssertMatches(
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
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              node(id: ID! @fusion__inputField(schema: A)): Node @fusion__field(schema: A)
              nodes(ids: [ID!]! @fusion__inputField(schema: A)): [Node]!
                @fusion__field(schema: A)
            }

            type Product implements Node
              @fusion__type(schema: A)
              @fusion__implements(schema: A, interface: "Node") {
              id: ID! @fusion__field(schema: A)
            }

            interface Node @fusion__type(schema: A) {
              id: ID! @fusion__field(schema: A)
            }
            """,
            options => options.EnableGlobalObjectIdentification = false);
    }

    // Node interface exists and option is set to true with a non-GOI-shaped nodes field.
    [Fact]
    public void Merge_GlobalObjectIdentificationEnabledNonGoiNodesShape_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                type Query {
                    node(id: ID!): Node @lookup
                    nodes: [Node]
                }

                interface Node {
                    id: ID!
                }

                type Product implements Node {
                    id: ID!
                }
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              node(id: ID!): Node @fusion__gateway_field
              nodes: [Node] @fusion__field(schema: A)
            }

            type Product implements Node
              @fusion__type(schema: A)
              @fusion__implements(schema: A, interface: "Node") {
              id: ID! @fusion__field(schema: A)
            }

            interface Node
              @fusion__type(schema: A)
              @fusion__lookup(
                schema: A
                key: "id"
                field: "node(id: ID!): Node"
                map: ["id"]
                path: null
                internal: false
              ) {
              id: ID! @fusion__field(schema: A)
            }
            """,
            options => options.EnableGlobalObjectIdentification = true);
    }
}
