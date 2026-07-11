namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerInterfaceObjectTests : SourceSchemaMergerTestBase
{
    // Source schema A defines the "Media" interface. Source schema B defines an @interfaceObject
    // stand-in that contributes a non-key "reviews" field. The stand-in is merged into the interface
    // (it never appears as its own object type), and "reviews" is attributed to source schema B.
    [Fact]
    public void Merge_StandInContributesFieldToInterface_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                interface Media @key(fields: "id") {
                    id: ID!
                    title: String!
                }
                """,
                """
                # Schema B
                type Media @interfaceObject @key(fields: "id") {
                    id: ID!
                    reviews: [Review!]!
                }

                type Review {
                    rating: Int!
                }
                """
            ],
            """
            type Review @fusion__type(schema: B) {
              rating: Int! @fusion__field(schema: B)
            }

            interface Media
              @fusion__type(schema: A)
              @fusion__type(schema: B)
              @fusion__interfaceObject(schema: B) {
              id: ID! @fusion__field(schema: A) @fusion__field(schema: B)
              reviews: [Review!]! @fusion__field(schema: B)
              title: String! @fusion__field(schema: A)
            }
            """);
    }

    // A field whose value is produced by the stand-in schema returns an opaque type: the stand-in
    // schema holds no authoritative concrete type. Opacity is recorded once on the interface via
    // @fusion__interfaceObject(schema:), not per field, and the field is attributed to the stand-in
    // schema. A field returning the real interface (source schema A) is not opaque.
    [Fact]
    public void Merge_FieldReturningStandIn_MarkedOpaque_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                interface Media @key(fields: "id") {
                    id: ID!
                    title: String!
                }

                type Query {
                    mediaById(id: ID!): Media
                }
                """,
                """
                # Schema B
                type Media @interfaceObject @key(fields: "id") {
                    id: ID!
                    reviews: [Review!]!
                }

                type Review {
                    rating: Int!
                }

                type Query {
                    featured: Media
                }
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              featured: Media @fusion__field(schema: B, sourceType: "Media")
              mediaById(id: ID! @fusion__inputField(schema: A)): Media
                @fusion__field(schema: A)
            }

            type Review @fusion__type(schema: B) {
              rating: Int! @fusion__field(schema: B)
            }

            interface Media
              @fusion__type(schema: A)
              @fusion__type(schema: B)
              @fusion__interfaceObject(schema: B) {
              id: ID! @fusion__field(schema: A) @fusion__field(schema: B)
              reviews: [Review!]! @fusion__field(schema: B)
              title: String! @fusion__field(schema: A)
            }
            """);
    }

    // "Book" implements "Media" in source schema A and declares no "reviews" of its own, so it
    // inherits the default contributed by source schema B's stand-in. The projected "reviews" field
    // on "Book" is routed to source schema B.
    [Fact]
    public void Merge_StandInDefaultProjectedOntoImplementingType_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                interface Media @key(fields: "id") {
                    id: ID!
                    title: String!
                }

                type Book implements Media @key(fields: "id") {
                    id: ID!
                    title: String!
                }
                """,
                """
                # Schema B
                type Media @interfaceObject @key(fields: "id") {
                    id: ID!
                    reviews: [Review!]!
                }

                type Review {
                    rating: Int!
                }
                """
            ],
            """
            type Book implements Media
              @fusion__type(schema: A)
              @fusion__implements(schema: A, interface: "Media") {
              id: ID! @fusion__field(schema: A)
              reviews: [Review!]! @fusion__field(schema: B)
              title: String! @fusion__field(schema: A)
            }

            type Review @fusion__type(schema: B) {
              rating: Int! @fusion__field(schema: B)
            }

            interface Media
              @fusion__type(schema: A)
              @fusion__type(schema: B)
              @fusion__interfaceObject(schema: B) {
              id: ID! @fusion__field(schema: A) @fusion__field(schema: B)
              reviews: [Review!]! @fusion__field(schema: B)
              title: String! @fusion__field(schema: A)
            }
            """);
    }

    // Source schema A declares that "PhysicalProduct" implements "Product", while source schema B
    // declares that "Chair" implements "PhysicalProduct" and never mentions "Product". The composite
    // "Chair" must implement both through the transitively closed implements relation.
    [Fact]
    public void Merge_TransitiveClosureAddsDerivedEdge_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                interface Product {
                    id: ID!
                }

                interface PhysicalProduct implements Product {
                    id: ID!
                    weight: Int
                }
                """,
                """
                # Schema B
                interface PhysicalProduct {
                    id: ID!
                    weight: Int
                }

                type Chair implements PhysicalProduct {
                    id: ID!
                    weight: Int
                    legs: Int
                }
                """
            ],
            """
            type Chair implements PhysicalProduct & Product
              @fusion__type(schema: B)
              @fusion__implements(schema: B, interface: "PhysicalProduct") {
              id: ID! @fusion__field(schema: B)
              legs: Int @fusion__field(schema: B)
              weight: Int @fusion__field(schema: B)
            }

            interface PhysicalProduct implements Product
              @fusion__type(schema: A)
              @fusion__type(schema: B)
              @fusion__implements(schema: A, interface: "Product") {
              id: ID! @fusion__field(schema: A) @fusion__field(schema: B)
              weight: Int @fusion__field(schema: A) @fusion__field(schema: B)
            }

            interface Product @fusion__type(schema: A) {
              id: ID! @fusion__field(schema: A)
            }
            """);
    }

    // The "Catalog" schema contributes "reviews" directly on "Book" and "Movie". The "Reviews" schema
    // takes over by declaring "Media" as a stand-in whose "reviews" field overrides "Catalog". The
    // direct "Catalog" declarations are dropped and "reviews" is projected as a default owned by
    // "Reviews".
    [Fact]
    public void Merge_StandInOverrideDropsDirectDeclarations_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                interface Media @key(fields: "id") {
                    id: ID!
                    title: String!
                }

                type Book implements Media @key(fields: "id") {
                    id: ID!
                    title: String!
                    author: String!
                    reviews: [Review!]!
                }

                type Movie implements Media @key(fields: "id") {
                    id: ID!
                    title: String!
                    director: String!
                    reviews: [Review!]!
                }

                type Review {
                    id: ID! @shareable
                    rating: Int! @shareable
                }
                """,
                """
                # Schema B
                type Media @interfaceObject @key(fields: "id") {
                    id: ID!
                    reviews: [Review!]! @override(from: "A")
                }

                type Review {
                    id: ID! @shareable
                    rating: Int! @shareable
                }
                """
            ],
            """
            type Book implements Media
              @fusion__type(schema: A)
              @fusion__implements(schema: A, interface: "Media") {
              author: String! @fusion__field(schema: A)
              id: ID! @fusion__field(schema: A)
              reviews: [Review!]! @fusion__field(schema: B)
              title: String! @fusion__field(schema: A)
            }

            type Movie implements Media
              @fusion__type(schema: A)
              @fusion__implements(schema: A, interface: "Media") {
              director: String! @fusion__field(schema: A)
              id: ID! @fusion__field(schema: A)
              reviews: [Review!]! @fusion__field(schema: B)
              title: String! @fusion__field(schema: A)
            }

            type Review @fusion__type(schema: A) @fusion__type(schema: B) {
              id: ID! @fusion__field(schema: A) @fusion__field(schema: B)
              rating: Int! @fusion__field(schema: A) @fusion__field(schema: B)
            }

            interface Media
              @fusion__type(schema: A)
              @fusion__type(schema: B)
              @fusion__interfaceObject(schema: B) {
              id: ID! @fusion__field(schema: A) @fusion__field(schema: B)
              reviews: [Review!]! @fusion__field(schema: B)
              title: String! @fusion__field(schema: A)
            }
            """);
    }
}
