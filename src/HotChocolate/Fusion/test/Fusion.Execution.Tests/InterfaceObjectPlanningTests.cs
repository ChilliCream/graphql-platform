using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Types;
using HotChocolate.Types;

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
          container: Container
        }
        type Container @key(fields: "id") {
          id: ID!
          media: Media!
        }
        type Media @interfaceObject @key(fields: "id") {
          id: ID!
          views: Int!
        }
        """;

    private const string NonResolvableSchemaA =
        """
        extend schema
          @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"])

        type Query {
          a: Node
        }

        interface Node @key(fields: "id") {
          id: ID!
        }

        type NodeImpl implements Node @key(fields: "id") {
          id: ID!
        }
        """;

    private const string NonResolvableSchemaB =
        """
        extend schema
          @link(
            url: "https://specs.apollo.dev/federation/v2.6"
            import: ["@key", "@interfaceObject"])

        type Query {
          b: Node
        }

        type Node @key(fields: "id", resolvable: false) @interfaceObject {
          id: ID!
          field: String
        }
        """;

    private const string OpaqueProductSchema =
        """
        # name: opaque
        type Query {
          product: Product
        }
        type Product @interfaceObject @key(fields: "id") {
          id: ID!
        }
        """;

    private const string ProductLookupSchema =
        """
        # name: products
        type Query {
          productById(id: ID!): Product @lookup
          breadById(id: ID!): Bread @lookup @internal
        }
        interface Product {
          id: ID!
        }
        type Bread implements Product {
          id: ID!
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
    public void Compile_Should_ExposeOpaqueInterfaceSelectionSetThroughPublicContracts()
    {
        // arrange
        var schema = ComposeSchema(SchemaA, SchemaB);
        const string sourceText =
            """
            query {
              trendingMedia {
                __typename
                id
                views
                ... on Book { isbn }
              }
            }
            """;
        var plan = PlanOperation(schema, sourceText);
        var standInNode = Assert.Single(
            plan.AllNodes.OfType<OperationExecutionNode>(),
            node => node.ResultSelectionSet
                .TryGetChild("trendingMedia")?.ProducesOpaqueElements is true);
        Assert.Equal("b", standInNode.SchemaName);

        var operation = plan.Operation;
        Assert.Equal(1, operation.RootSelectionSet.Selections.Length);
        var trendingMedia = operation.RootSelectionSet.Selections[0];
        var internalSelectionSet = operation.GetSelectionSet(
            trendingMedia,
            trendingMedia.Type.NamedType<IComplexTypeDefinition>());
        Assert.IsType<FusionInterfaceTypeDefinition>(internalSelectionSet.Type);

        // act and assert
        ISelectionSet selectionSet = internalSelectionSet;
        Assert.Equal("Media", selectionSet.Type.Name);

        var selection = Assert.Single(
            selectionSet.GetSelections(),
            selection => selection.Field.Name == "id");
        Assert.Equal("Media", selection.DeclaringSelectionSet.Type.Name);
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

    [Fact]
    public void Plan_Should_Prefer_CoveringAbstractLookup_Over_ConcreteFanOut()
    {
        // arrange
        var schema = ComposeSchema(OpaqueProductSchema, ProductLookupSchema);

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              product {
                ... on Bread {
                  id
                }
              }
            }
            """);

        // assert
        // The opaque stand-in cannot provide a concrete typename. The covering Product lookup
        // must therefore run at $.product; the Bread lookup at $.product<Bread> would be skipped
        // before identity has been recovered.
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_PreserveAliasedNestedInterfaceObjectStandIn()
    {
        // arrange
        var schema = ComposeSchema(SchemaA, SchemaB);

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              container {
                aliasedMedia: media {
                  id
                  views
                }
              }
            }
            """);

        // assert
        var operationNode = Assert.Single(plan.AllNodes.OfType<OperationExecutionNode>());
        var container = operationNode.ResultSelectionSet.TryGetChild("container");
        Assert.NotNull(container);
        var media = container.TryGetChild("aliasedMedia");
        Assert.NotNull(media);
        Assert.True(media.ProducesOpaqueElements);
    }

    [Fact]
    public void Plan_Should_Fail_When_InterfaceObjectHasNoResolvableLookup()
    {
        // arrange
        var schema = ComposeNonResolvableInterfaceObjectSchema();

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => PlanOperation(
                schema,
                """
                query {
                  a {
                    field
                  }
                }
                """));

        // assert
        Assert.Equal("No possible plan was found.", exception.Message);
    }

    private static FusionSchemaDefinition ComposeNonResolvableInterfaceObjectSchema()
    {
        var options = new SchemaComposerOptions();
        options.ApolloFederationCompatibility.AllowNonResolvableInterfaceObjects = true;

        var log = new CompositionLog();
        var result = new SchemaComposer(
            [
                new SourceSchemaText("a", NonResolvableSchemaA),
                new SourceSchemaText("b", NonResolvableSchemaB)
            ],
            options,
            log).Compose();

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Errors[0].Message);
        }

        return FusionSchemaDefinition.Create(result.Value.ToSyntaxNode());
    }
}
