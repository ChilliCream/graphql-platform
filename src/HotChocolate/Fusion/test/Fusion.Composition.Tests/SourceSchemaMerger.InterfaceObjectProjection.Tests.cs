namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerInterfaceObjectProjectionTests : SourceSchemaMergerTestBase
{
    // Schema A defines "Media" with "Book" and "Movie". Schema B contributes "reviews" through a
    // stand-in. Schema C replaces that default on "Photo" with its own @implement implementation.
    // "Book" and "Movie" adopt the default (owner: B); "Photo.reviews" is owned by C.
    [Fact]
    public void Merge_DefaultProjectedAndReplacedOnOneType_MatchesSnapshot()
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

                type Movie implements Media @key(fields: "id") {
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
                    body: String! @shareable
                }
                """,
                """
                # Schema C
                interface Media @key(fields: "id") {
                    id: ID!
                    title: String!
                }

                type Photo implements Media @key(fields: "id") {
                    id: ID!
                    title: String!
                    reviews: [Review!]! @implement
                }

                type Review {
                    body: String! @shareable
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

            type Movie implements Media
              @fusion__type(schema: A)
              @fusion__implements(schema: A, interface: "Media") {
              id: ID! @fusion__field(schema: A)
              reviews: [Review!]! @fusion__field(schema: B)
              title: String! @fusion__field(schema: A)
            }

            type Photo implements Media
              @fusion__type(schema: C)
              @fusion__implements(schema: C, interface: "Media") {
              id: ID! @fusion__field(schema: C)
              reviews: [Review!]! @fusion__field(schema: C)
              title: String! @fusion__field(schema: C)
            }

            type Review @fusion__type(schema: B) @fusion__type(schema: C) {
              body: String! @fusion__field(schema: B) @fusion__field(schema: C)
            }

            interface Media
              @fusion__type(schema: A)
              @fusion__type(schema: B)
              @fusion__type(schema: C)
              @fusion__interfaceObject(schema: B) {
              id: ID!
                @fusion__field(schema: A)
                @fusion__field(schema: B)
                @fusion__field(schema: C)
              reviews: [Review!]! @fusion__field(schema: B)
              title: String! @fusion__field(schema: A) @fusion__field(schema: C)
            }
            """);
    }

    // A stand-in that declares only its key field is a valid reference-only entry point. It contributes
    // no default field, and "Book" gains nothing from it beyond the shared "id" key.
    [Fact]
    public void Merge_ReferenceOnlyStandIn_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                type Media @interfaceObject @key(fields: "id") {
                    id: ID!
                }

                type Rating {
                    id: ID!
                    subject: Media!
                    stars: Int!
                }
                """,
                """
                # Schema B
                interface Media @key(fields: "id") {
                    id: ID!
                }

                type Book implements Media @key(fields: "id") {
                    id: ID!
                }

                type Query {
                    mediaById(id: ID!): Media @lookup
                }
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: B) {
              mediaById(id: ID! @fusion__inputField(schema: B)): Media
                @fusion__field(schema: B)
            }

            type Book implements Media
              @fusion__type(schema: B)
              @fusion__implements(schema: B, interface: "Media") {
              id: ID! @fusion__field(schema: B)
            }

            type Rating @fusion__type(schema: A) {
              id: ID! @fusion__field(schema: A)
              stars: Int! @fusion__field(schema: A)
              subject: Media! @fusion__field(schema: A, sourceType: "Media!")
            }

            interface Media
              @fusion__type(schema: A)
              @fusion__type(schema: B)
              @fusion__interfaceObject(schema: A)
              @fusion__lookup(
                schema: B
                key: "id"
                field: "mediaById(id: ID!): Media"
                map: ["id"]
                path: null
                internal: false
              ) {
              id: ID! @fusion__field(schema: A) @fusion__field(schema: B)
            }
            """);
    }
}
