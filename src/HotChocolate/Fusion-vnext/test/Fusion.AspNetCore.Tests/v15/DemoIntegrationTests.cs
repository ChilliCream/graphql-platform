using HotChocolate.Fusion.Packaging;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;

namespace HotChocolate.Fusion;

public partial class DemoIntegrationTests : FusionTestBase
{
    [Fact]
    public async Task Same_Selection_On_Two_Object_Types_That_Require_Data_From_Another_Subgraph()
    {
        // arrange
        var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              item1: Item1!
              item2: Item2!
            }

            type Item1 {
              product: Product!
            }

            type Item2 {
              product: Product!
            }

            type Product implements Node {
              id: ID!
            }

            interface Node @key(fields: "id") {
              id: ID!
            }
            """);

        var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              node(id: ID!): Node @lookup
              nodes(ids: [ID!]!): [Node]!
            }

            type Product implements Node {
              id: ID!
              name: String!
            }

            interface Node {
              id: ID!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              item1 {
                product {
                  id
                  name
                }
              }
              item2 {
                product {
                  id
                  name
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Same_Selection_On_Two_List_Fields_That_Require_Data_From_Another_Subgraph()
    {
        // arrange
        var server1 = CreateSourceSchema(
            "A",
            """
            interface Node {
              id: ID!
            }

            type Query {
              productsA: [Product]
              productsB: [Product]
              productById(id: ID!): Product @lookup @internal
            }

            type Product implements Node {
              id: ID!
              name: String!
            }
            """);

        var server2 = CreateSourceSchema(
            "B",
            """
            interface Node {
              id: ID!
            }

            type Query {
              node(id: ID!): Node @lookup
              nodes(ids: [ID!]!): [Node]!
            }

            type Product implements Node {
              id: ID!
              price: Float!
              reviewCount: Int!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productsA {
                id
                name
                price
                reviewCount
              }
              productsB {
                id
                name
                price
                reviewCount
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task BatchExecutionState_With_Multiple_Variable_Values()
    {
        // arrange
        var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup @shareable
              nodes(ids: [ID!]!): [Node]! @shareable
            }

            interface Node {
              id: ID!
            }

            type User implements Node {
              id: ID!
              displayName: String!
            }
            """);
        var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              node(id: ID!): Node @lookup @shareable
              nodes(ids: [ID!]!): [Node]! @shareable
              userBySlug(slug: String!): User
            }

            interface Node {
              id: ID!
            }

            type User implements Node {
              relativeUrl: String!
              id: ID!
            }
            """);
        var server3 = CreateSourceSchema(
            "C",
            """
            type Query {
              node(id: ID!): Node @lookup @shareable
              nodes(ids: [ID!]!): [Node]! @shareable
            }

            interface Node {
              id: ID!
            }

            type User implements Node {
              id: ID!
              feedbacks: FeedbacksConnection
            }

            type FeedbacksConnection {
              edges: [FeedbacksEdge!]
            }

            type FeedbacksEdge {
              node: ResaleFeedback!
            }

            type ResaleFeedback implements Node {
              feedback: ResaleSurveyFeedback
              id: ID!
            }

            type ResaleSurveyFeedback {
              buyer: User
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2),
            ("C", server3)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              userBySlug(slug: "me") {
                feedbacks {
                  edges {
                    node {
                      feedback {
                        buyer {
                          relativeUrl
                          displayName
                        }
                      }
                    }
                  }
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task BatchExecutionState_With_Multiple_Variable_Values_Some_Items_Null()
    {
        // arrange
        var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup @shareable
              nodes(ids: [ID!]!): [Node]! @shareable
            }

            interface Node {
              id: ID!
            }

            type User implements Node {
              id: ID!
              displayName: String!
            }
            """);
        var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              node(id: ID!): Node @lookup @shareable
              # TODO: This will not work like this
              nodes(ids: [ID!]!): [Node]! @null(atIndex: 1) @shareable
              userBySlug(slug: String!): User
            }

            interface Node {
              id: ID!
            }

            type User implements Node {
              relativeUrl: String!
              id: ID!
            }
            """);
        var server3 = CreateSourceSchema(
            "C",
            """
            type Query {
              node(id: ID!): Node @lookup @shareable
              nodes(ids: [ID!]!): [Node]! @shareable
            }

            interface Node {
              id: ID!
            }

            type User implements Node {
              id: ID!
              feedbacks: FeedbacksConnection
            }

            type FeedbacksConnection {
              edges: [FeedbacksEdge!]
            }

            type FeedbacksEdge {
              node: ResaleFeedback!
            }

            type ResaleFeedback implements Node {
              feedback: ResaleSurveyFeedback
              id: ID!
            }

            type ResaleSurveyFeedback {
              buyer: User
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2),
            ("C", server3)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              userBySlug(slug: "me") {
                feedbacks {
                  edges {
                    node {
                      feedback {
                        buyer {
                          relativeUrl
                          displayName
                        }
                      }
                    }
                  }
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task BatchExecutionState_With_Multiple_Variable_Values_And_Forwarded_Variable()
    {
        // arrange
        var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup @shareable
              nodes(ids: [ID!]!): [Node]! @shareable
            }

            interface Node {
              id: ID!
            }

            type User implements Node {
              id: ID!
              displayName(arg: String): String!
            }
            """);
        var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              node(id: ID!): Node @lookup @shareable
              nodes(ids: [ID!]!): [Node]! @shareable
              userBySlug(slug: String!): User
            }

            interface Node {
              id: ID!
            }

            type User implements Node {
              relativeUrl(arg: String): String!
              id: ID!
            }
            """);
        var server3 = CreateSourceSchema(
            "C",
            """
            type Query {
              node(id: ID!): Node @lookup @shareable
              nodes(ids: [ID!]!): [Node]! @shareable
            }

            interface Node {
              id: ID!
            }

            type User implements Node {
              id: ID!
              feedbacks: FeedbacksConnection
            }

            type FeedbacksConnection {
              edges: [FeedbacksEdge!]
            }

            type FeedbacksEdge {
              node: ResaleFeedback!
            }

            type ResaleFeedback implements Node {
              feedback: ResaleSurveyFeedback
              id: ID!
            }

            type ResaleSurveyFeedback {
              buyer: User
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2),
            ("C", server3)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query($arg1: String, $arg2: String) {
              userBySlug(slug: "me") {
                feedbacks {
                  edges {
                    node {
                      feedback {
                        buyer {
                          relativeUrl(arg: $arg1)
                          displayName(arg: $arg2)
                        }
                      }
                    }
                  }
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["arg1"] = "abc", ["arg2"] = "def" });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Field_Below_Shared_Field_Only_Available_On_One_Subgraph_Type_Of_Shared_Field_Not_Node()
    {
        // arrange
        var server1 = CreateSourceSchema(
            "A",
            """
            interface Node {
              id: ID!
            }

            type Product implements Node {
              id: ID!
              subgraph1Only: ProductAvailability
            }

            type ProductAvailability implements Node {
              id: ID!
              sharedLinked: ProductAvailabilityMail! @shareable
            }

            type ProductAvailabilityMail {
              subgraph1Only: String!
            }

            type Query {
              node(id: ID!): Node @lookup @shareable
              productById(id: ID!): Product @lookup
              productAvailabilityById(id: ID!): ProductAvailability @lookup @shareable
            }
            """);

        var server2 = CreateSourceSchema(
            "B",
            """
            interface Node {
              id: ID!
            }

            type ProductAvailability implements Node {
              id: ID!
              sharedLinked: ProductAvailabilityMail! @shareable
            }

            type ProductAvailabilityMail {
              subgraph2Only: Boolean!
            }

            type Query {
              node("ID of the object." id: ID!): Node @lookup @shareable
              productAvailabilityById(id: ID!): ProductAvailability @lookup @shareable
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query($productId: ID!) {
              productById(id: $productId) {
                subgraph1Only {
                  sharedLinked {
                    subgraph2Only
                  }
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["productId"] = "UHJvZHVjdAppMzg2MzE4NTk=" });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Field_Below_Shared_Field_Only_Available_On_One_Subgraph_Type_Of_Shared_Field_Not_Node_2()
    {
        // arrange
        var server1 = CreateSourceSchema(
            "A",
            """
            interface Node {
              id: ID!
            }

            type Product implements Node {
              id: ID!
              subgraph1Only: ProductAvailability
            }

            type ProductAvailability implements Node {
              id: ID!
              sharedLinked: ProductAvailabilityMail! @shareable
            }

            type ProductAvailabilityMail {
              sharedScalar: String! @shareable
            }

            type Query {
              node(id: ID!): Node @lookup @shareable
              productById(id: ID!): Product @lookup
              productAvailabilityById(id: ID!): ProductAvailability @lookup @shareable
            }
            """);

        var server2 = CreateSourceSchema(
            "B",
            """
            interface Node {
              id: ID!
            }

            type ProductAvailability implements Node {
              sharedLinked: ProductAvailabilityMail! @shareable
              subgraph2Only: Boolean!
              id: ID!
            }

            type ProductAvailabilityMail {
              subgraph2Only: Boolean!
              sharedScalar: String! @shareable
            }

            type Query {
              node(id: ID!): Node @lookup @shareable
              productAvailabilityById(id: ID!): ProductAvailability @lookup @shareable
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query($productId: ID!) {
              productById(id: $productId) {
                subgraph1Only {
                  subgraph2Only
                  sharedLinked {
                    subgraph2Only
                    sharedScalar
                  }
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["productId"] = "UHJvZHVjdAppMzg2MzE4NTk=" });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Field_Below_Shared_Field_Only_Available_On_One_Subgraph_Type_Of_Shared_Field_Not_Node_3()
    {
        // arrange
        var server1 = CreateSourceSchema(
            "A",
            """
            interface Node {
              id: ID!
            }

            type Product implements Node {
              id: ID!
              subgraph1Only: ProductAvailability
            }

            type ProductAvailability implements Node {
              id: ID!
              sharedLinked: ProductAvailabilityMail! @shareable
              subgraph1Only: Boolean!
            }

            type ProductAvailabilityMail {
              sharedScalar: String! @shareable
              subgraph1Only: String!
            }

            type Query {
              node(id: ID!): Node @lookup @shareable
              productById(id: ID!): Product @lookup
              productAvailabilityById(id: ID!): ProductAvailability @lookup @shareable
            }
            """);

        var server2 = CreateSourceSchema(
            "B",
            """
            interface Node {
              id: ID!
            }

            type ProductAvailability implements Node {
              sharedLinked: ProductAvailabilityMail! @shareable
              subgraph2Only: Boolean!
              id: ID!
            }

            type ProductAvailabilityMail {
              subgraph2Only: Boolean!
              sharedScalar: String! @shareable
            }

            type Query {
              node(id: ID!): Node @lookup @shareable
              productAvailabilityById(id: ID!): ProductAvailability @lookup @shareable
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query($productId: ID!) {
              productById(id: $productId) {
                subgraph1Only {
                  subgraph2Only
                  subgraph1Only
                  sharedLinked {
                    subgraph2Only
                    sharedScalar
                    subgraph1Only
                  }
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["productId"] = "UHJvZHVjdAppMzg2MzE4NTk=" });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Viewer_Bug_1()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              exclusiveSubgraphA: ExclusiveSubgraphA
              viewer: Viewer @shareable
            }

            type ExclusiveSubgraphA {
              id: ID!
            }

            type Viewer {
              name: String
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              viewer: Viewer @shareable
            }

            type Viewer {
              exclusiveSubgraphB: String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery {
              exclusiveSubgraphA {
                __typename
              }
              viewer {
                exclusiveSubgraphB
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Viewer_Bug_2()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              exclusiveSubgraphA: ExclusiveSubgraphA
              viewer: Viewer @shareable
            }

            type ExclusiveSubgraphA {
              id: ID!
            }

            type Viewer {
              subType: SubType @shareable
            }

            type SubType {
              subgraphA: String
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              viewer: Viewer @shareable
            }

            type Viewer {
              subType: SubType @shareable
            }

            type SubType {
              subgraphB: String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery {
              exclusiveSubgraphA {
                __typename
              }
              viewer {
                subType {
                  subgraphB
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Viewer_Bug_3()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer! @shareable
            }

            type Viewer {
              subgraphA: String!
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              viewer: Viewer! @shareable
            }

            type Viewer {
              subgraphB: String!
            }
            """);

        using var serverC = CreateSourceSchema(
            "C",
            """
            type Query {
              subgraphC: SubgraphC!
            }

            type SubgraphC {
              someField: String!
              anotherField: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB),
            ("C", serverC)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              viewer {
                subgraphA
                subgraphB
              }
              subgraphC {
                someField
                anotherField
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Unresolvable_Subgraph_Is_Not_Chosen_If_Data_Is_Available_In_Resolvable_Subgraph()
    {
        // arrange
        var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              product: Product!
            }

            type Product implements Node {
              id: ID!
            }

            interface Node @key(fields: "id") {
              id: ID!
            }
            """);

        var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              test: Test!
            }

            type Test {
              id: ID!
            }

            type Product @key(fields: "id") {
              id: ID!
              name: String! @shareable
            }
            """);

        var server3 = CreateSourceSchema(
            "C",
            """
            type Query {
              node(id: ID!): Node @lookup
            }

            type Product implements Node {
              id: ID!
              name: String! @shareable
            }

            interface Node {
              id: ID!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2),
            ("C", server3)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              viewer {
                product {
                  id
                  name
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Subgraph_Containing_More_Selections_Is_Chosen()
    {
        // arrange
        var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              productBySlug: Product
            }

            type Product @key(fields: "id") {
              id: ID!
            }
            """);

        var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup @shareable
            }

            type Product {
              id: ID!
              author: Author @shareable
            }

            type Author {
              name: String! @shareable
            }
            """);

        var server3 = CreateSourceSchema(
            "C",
            """
            type Query {
              productById(id: ID!): Product @lookup @shareable
            }

            type Product {
              id: ID!
              author: Author @shareable
            }

            type Author {
              name: String! @shareable
              rating: Int!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2),
            ("C", server3)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productBySlug {
                author {
                  name
                  rating
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    public sealed class HotReloadConfiguration : IObservable<GatewayConfiguration>
    {
        private GatewayConfiguration _configuration;
        private Session? _session;

        public HotReloadConfiguration(GatewayConfiguration configuration)
        {
            _configuration = configuration ??
                throw new ArgumentNullException(nameof(configuration));
        }

        public void SetConfiguration(GatewayConfiguration configuration)
        {
            _configuration = configuration ??
                throw new ArgumentNullException(nameof(configuration));
            _session?.Update();
        }

        public IDisposable Subscribe(IObserver<GatewayConfiguration> observer)
        {
            var session = _session = new Session(this, observer);
            session.Update();
            return session;
        }

        private sealed class Session : IDisposable
        {
            private readonly HotReloadConfiguration _owner;
            private readonly IObserver<GatewayConfiguration> _observer;

            public Session(HotReloadConfiguration owner, IObserver<GatewayConfiguration> observer)
            {
                _owner = owner;
                _observer = observer;
            }

            public void Update()
            {
                _observer.OnNext(_owner._configuration);
            }

            public void Dispose() { }
        }
    }
}
