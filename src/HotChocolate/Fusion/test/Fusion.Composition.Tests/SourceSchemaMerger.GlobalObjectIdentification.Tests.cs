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

    // The node field is inaccessible in the only source schema. The canonical gateway node
    // field carries the merged inaccessible marker, the GOI-shaped nodes field is dropped.
    [Fact]
    public void Merge_Should_MarkNodeFieldInaccessible_When_TheOnlySourceSchemaMarksItInaccessible()
    {
        AssertMatches(
            [
                """
                # Schema A
                type Query {
                    node(id: ID!): Node @lookup @inaccessible
                    nodes(ids: [ID!]!): [Node]! @inaccessible
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
              node(id: ID!): Node @fusion__gateway_field @fusion__inaccessible
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

    // The node field is inaccessible in one source schema and accessible in another. The
    // existing merge semantics apply: inaccessible if any source schema marks it inaccessible.
    [Fact]
    public void Merge_Should_MarkNodeFieldInaccessible_When_OneOfTwoSourceSchemasMarksItInaccessible()
    {
        AssertMatches(
            [
                """
                # Schema A
                type Query {
                    node(id: ID!): Node @lookup @shareable @inaccessible
                }

                interface Node {
                    id: ID!
                }

                type Product implements Node {
                    id: ID!
                }
                """,
                """
                # Schema B
                type Query {
                    node(id: ID!): Node @lookup @shareable
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

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              node(id: ID!): Node @fusion__gateway_field @fusion__inaccessible
            }

            type Product implements Node
              @fusion__type(schema: A)
              @fusion__type(schema: B)
              @fusion__implements(schema: A, interface: "Node")
              @fusion__implements(schema: B, interface: "Node") {
              id: ID! @fusion__field(schema: A) @fusion__field(schema: B)
            }

            interface Node
              @fusion__type(schema: A)
              @fusion__type(schema: B)
              @fusion__lookup(
                schema: A
                key: "id"
                field: "node(id: ID!): Node"
                map: ["id"]
                path: null
                internal: false
              )
              @fusion__lookup(
                schema: B
                key: "id"
                field: "node(id: ID!): Node"
                map: ["id"]
                path: null
                internal: false
              ) {
              id: ID! @fusion__field(schema: A) @fusion__field(schema: B)
            }
            """,
            options => options.EnableGlobalObjectIdentification = true);
    }
}
