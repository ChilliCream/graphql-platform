namespace HotChocolate.Fusion;

public sealed class InterfaceObjectPlanningTests : FusionTestBase
{
    // Source schema A defines the Media interface and its implementing types; source schema B is an
    // @interfaceObject stand-in contributing "views". A value produced by B is opaque, so the
    // concrete __typename and the implementing-type fields are recovered through the covering
    // interface lookup on A.
    private const string SchemaA =
        """
        # name: a
        type Query {
          mediaById(id: ID!): Media @lookup
          allMedia: [Media!]!
        }
        interface Media {
          id: ID!
          title: String!
        }
        type Book implements Media @key(fields: "id") {
          id: ID!
          title: String!
          isbn: String!
        }
        type Movie implements Media @key(fields: "id") {
          id: ID!
          title: String!
          runtime: Int!
        }
        """;

    private const string SchemaB =
        """
        # name: b
        type Query {
          trendingMedia: [Media!]!
          mediaByKey(id: ID!): Media @lookup @internal
        }
        type Media @interfaceObject @key(fields: "id") {
          id: ID!
          views: Int!
        }
        """;

    [Fact]
    public void Plan_Should_Route_Fragment_Narrowing_To_Covering_Lookup()
    {
        // arrange
        var schema = ComposeSchema(SchemaA, SchemaB);

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              trendingMedia {
                __typename
                id
                views
                ... on Book { isbn }
                ... on Movie { runtime }
              }
            }
            """);

        // assert
        // The stand-in schema B resolves only the interface-level fields it owns; the concrete
        // __typename and the implementing-type fields are recovered through A's covering interface
        // lookup, keyed by the id available from B.
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Route_TypeName_To_Covering_Lookup()
    {
        // arrange
        var schema = ComposeSchema(SchemaA, SchemaB);

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              trendingMedia {
                __typename
                id
                views
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Skip_Covering_Lookup_When_Only_InterfaceFields_Selected()
    {
        // arrange
        var schema = ComposeSchema(SchemaA, SchemaB);

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              trendingMedia {
                id
              }
            }
            """);

        // assert
        // The client selects neither __typename nor any type-conditioned field, so identity is
        // never observed. The stand-in schema B resolves the interface-level fields on its own and
        // no covering interface lookup is planned (single fetch, Apollo parity).
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Route_ConcreteFragment_TypeNameOnly_To_Covering_Lookup()
    {
        // arrange
        var schema = ComposeSchema(SchemaA, SchemaB);

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              trendingMedia {
                ... on Book { __typename }
              }
            }
            """);

        // assert
        // A concrete fragment whose only selection is __typename still observes identity, so the
        // covering interface lookup on A recovers the concrete type even though nothing else is
        // selected inside the fragment.
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Route_ConcreteFragment_StandInField_Through_StandIn()
    {
        // arrange
        var schema = ComposeSchema(SchemaA, SchemaB);

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              trendingMedia {
                ... on Book { views }
              }
            }
            """);

        // assert
        // "views" is contributed by the stand-in schema B. Under a concrete fragment the covering
        // lookup on A recovers identity, and the projected field is still routed back to B through
        // its covering interface lookup.
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Route_ConcreteFragment_StandInField_From_NonOpaque_Root()
    {
        // arrange
        var schema = ComposeSchema(SchemaA, SchemaB);

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              allMedia {
                id
                ... on Book { isbn views }
              }
            }
            """);

        // assert
        // "allMedia" is served by the concrete-aware schema A, so identity is local; the projected
        // field "views" under the concrete fragment is routed to the stand-in schema B.
        MatchSnapshot(plan);
    }
}
