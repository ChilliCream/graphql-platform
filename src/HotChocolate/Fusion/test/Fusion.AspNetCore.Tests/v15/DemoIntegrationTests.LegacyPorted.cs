using HotChocolate.Transport;
using HotChocolate.Transport.Http;

namespace HotChocolate.Fusion;

public partial class DemoIntegrationTests
{
    [Fact]
    public async Task Authors_And_Reviews_AutoCompose()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(
            DemoSubgraphs.Accounts | DemoSubgraphs.Reviews2);

        var request = new OperationRequest(
            """
            query {
              users {
                id
              }
              reviews {
                body
              }
            }
            """);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Authors_And_Reviews_And_Products_AutoCompose()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(
            DemoSubgraphs.Accounts | DemoSubgraphs.Reviews2 | DemoSubgraphs.Products);

        var request = new OperationRequest(
            """
            query {
              users {
                id
              }
              reviews {
                body
              }
              topProducts(first: 2) {
                id
              }
            }
            """);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Authors_And_Reviews_Query_GetUserReviews()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(
            DemoSubgraphs.Accounts | DemoSubgraphs.Reviews2);

        var request = new OperationRequest(
            """
            query GetUser {
              users {
                name
                reviews {
                  body
                  author {
                    name
                  }
                }
              }
            }
            """);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "Cost reporting request options are not wired for this v15 harness yet.")]
    public async Task Authors_And_Reviews_Query_GetUserReviews_Report_Cost()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(
            DemoSubgraphs.Accounts | DemoSubgraphs.Reviews2);

        var request = new OperationRequest(
            """
            query GetUser {
              users {
                name
                reviews {
                  body
                  author {
                    name
                  }
                }
              }
            }
            """);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Authors_And_Reviews_Query_GetUserReviews_Skip_Author()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(
            DemoSubgraphs.Accounts | DemoSubgraphs.Reviews2);

        var request = new OperationRequest(
            """
            query GetUser($skip: Boolean!) {
              users {
                name
                reviews {
                  body
                  author @skip(if: $skip) {
                    name
                    birthdate
                  }
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["skip"] = true });

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Authors_And_Reviews_Query_GetUserReviews_Skip_Author_ErrorField()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(
            DemoSubgraphs.Accounts | DemoSubgraphs.Reviews2);

        var request = new OperationRequest(
            """
            query GetUser($skip: Boolean!) {
              users {
                name
                reviews {
                  body
                  author {
                    name
                    birthdate
                    errorField @skip(if: $skip)
                  }
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["skip"] = true });

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Authors_And_Reviews_Query_GetUserById()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(
            DemoSubgraphs.Accounts | DemoSubgraphs.Reviews2);

        var request = new OperationRequest(
            """
            query GetUser {
              userById(id: "VXNlcjox") {
                id
              }
            }
            """);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Authors_And_Reviews_Query_GetUserById_With_Invalid_Id_Value()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(
            DemoSubgraphs.Accounts | DemoSubgraphs.Reviews2);

        var request = new OperationRequest(
            """
            query GetUser {
              userById(id: 1) {
                id
              }
            }
            """);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "Legacy V15 websocket/SSE subscription setup is not ported in this harness.")]
    public async Task Authors_And_Reviews_Subscription_OnNewReview()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(
            DemoSubgraphs.Accounts | DemoSubgraphs.Reviews2);

        var request = new OperationRequest(
            """
            subscription OnNewReview {
              onNewReview {
                body
                author {
                  name
                }
              }
            }
            """);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "Legacy V15 websocket/SSE subscription setup is not ported in this harness.")]
    public async Task Authors_And_Reviews_Subscription_OnNewReviewError()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(
            DemoSubgraphs.Accounts | DemoSubgraphs.Reviews2);

        var request = new OperationRequest(
            """
            subscription OnNewReview {
              onNewReviewError {
                body
                author {
                  name
                }
              }
            }
            """);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "Legacy V15 websocket/SSE subscription setup is not ported in this harness.")]
    public async Task Authors_And_Reviews_Subscription_OnError()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(
            DemoSubgraphs.Accounts | DemoSubgraphs.Reviews2);

        var request = new OperationRequest(
            """
            subscription OnError {
              onError {
                body
                author {
                  name
                }
              }
            }
            """);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "Legacy V15 websocket/SSE subscription setup is not ported in this harness.")]
    public async Task Authors_And_Reviews_Subscription_OnError_SSE()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(
            DemoSubgraphs.Accounts | DemoSubgraphs.Reviews2);

        var request = new OperationRequest(
            """
            subscription OnError {
              onError {
                body
                author {
                  name
                }
              }
            }
            """);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "Legacy V15 websocket/SSE subscription setup is not ported in this harness.")]
    public async Task Authors_And_Reviews_Subscription_OnNewReview_Two_Graphs()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(
            DemoSubgraphs.Accounts | DemoSubgraphs.Reviews2);

        var request = new OperationRequest(
            """
            subscription OnNewReview {
              onNewReview {
                body
                author {
                  name
                  birthdate
                }
              }
            }
            """);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Authors_And_Reviews_Query_ReviewsUser()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(
            DemoSubgraphs.Accounts | DemoSubgraphs.Reviews2);

        var request = new OperationRequest(
            """
            query GetUser {
              a: reviews {
                body
                author {
                  name
                }
              }
              b: reviews {
                body
                author {
                  name
                }
              }
              users {
                name
                reviews {
                  body
                  author {
                    name
                  }
                }
              }
            }
            """);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "Do we want to reformat ids?")]
    public async Task Authors_And_Reviews_Query_Reformat_AuthorIds()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(
            DemoSubgraphs.Accounts | DemoSubgraphs.Reviews2);

        var request = new OperationRequest(
            """
            query ReformatIds {
              reviews {
                author {
                  id
                }
              }
            }
            """);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "this does not work yet")]
    public async Task Authors_And_Reviews_Query_Reformat_AuthorIds_ReEncodeAllIds()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(
            DemoSubgraphs.Accounts | DemoSubgraphs.Reviews2);

        var request = new OperationRequest(
            """
            query ReformatIds {
              reviews {
                author {
                  id
                }
              }
            }
            """);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Authors_And_Reviews_Batch_Requests()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(
            DemoSubgraphs.Accounts | DemoSubgraphs.Reviews2);

        var request = new OperationRequest(
            """
            query GetUser {
              reviews {
                body
                author {
                  birthdate
                }
              }
            }
            """);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Authors_And_Reviews_And_Products_Query_TopProducts()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(
            DemoSubgraphs.Accounts | DemoSubgraphs.Reviews2 | DemoSubgraphs.Products);

        var request = new OperationRequest(
            """
            query TopProducts {
              topProducts(first: 2) {
                name
                reviews {
                  body
                  author {
                    name
                  }
                }
              }
            }
            """);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Authors_And_Reviews_And_Products_Query_TypeName()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(
            DemoSubgraphs.Accounts | DemoSubgraphs.Reviews2 | DemoSubgraphs.Products);

        var request = new OperationRequest(
            """
            query TopProducts {
              __typename
              topProducts(first: 2) {
                __typename
                reviews {
                  __typename
                  author {
                    __typename
                  }
                }
              }
            }
            """);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Authors_And_Reviews_And_Products_With_Variables()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(
            DemoSubgraphs.Accounts | DemoSubgraphs.Reviews2 | DemoSubgraphs.Products);

        var request = new OperationRequest(
            """
            query TopProducts($first: Int!) {
              topProducts(first: $first) {
                id
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["first"] = 2 });

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Authors_And_Reviews_And_Products_Introspection()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(
            DemoSubgraphs.Accounts | DemoSubgraphs.Reviews2 | DemoSubgraphs.Products);

        var request = new OperationRequest(
            """
            query Introspect {
              __schema {
                types {
                  name
                  kind
                  fields {
                    name
                    type {
                      name
                      kind
                    }
                  }
                }
              }
            }
            """);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Fetch_User_With_Node_Field()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(
            DemoSubgraphs.Accounts | DemoSubgraphs.Reviews2 | DemoSubgraphs.Products);

        var request = new OperationRequest(
            """
            query FetchNode($id: ID!) {
              node(id: $id) {
                ... on User {
                  id
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["id"] = "VXNlcjox" });

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Fetch_User_With_Invalid_Node_Field()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(
            DemoSubgraphs.Accounts | DemoSubgraphs.Reviews2 | DemoSubgraphs.Products);

        var request = new OperationRequest(
            """
            query FetchNode($id: ID!) {
              node(id: $id) {
                ... on User {
                  id
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["id"] = 1 });

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Fetch_User_With_Node_Field_Pass_In_Review_Id()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(
            DemoSubgraphs.Accounts | DemoSubgraphs.Reviews2 | DemoSubgraphs.Products);

        var request = new OperationRequest(
            """
            query FetchNode($id: ID!) {
              node(id: $id) {
                ... on User {
                  id
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["id"] = "UmV2aWV3OjE=" });

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Fetch_User_With_Node_Field_Pass_In_Unknown_Id()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(
            DemoSubgraphs.Accounts | DemoSubgraphs.Reviews2 | DemoSubgraphs.Products);

        var request = new OperationRequest(
            """
            query FetchNode($id: ID!) {
              node(id: $id) {
                ... on User {
                  id
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["id"] = "VW5rbm93bjox" });

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Fetch_User_With_Node_Field_From_Two_Subgraphs()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(
            DemoSubgraphs.Accounts | DemoSubgraphs.Reviews2 | DemoSubgraphs.Products);

        var request = new OperationRequest(
            """
            query FetchNode($id: ID!) {
              node(id: $id) {
                ... on User {
                  birthdate
                  reviews {
                    body
                  }
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["id"] = "VXNlcjox" });

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "Hot reload via dynamic gateway configuration is not ported in this harness.")]
    public async Task Hot_Reload()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(DemoSubgraphs.Accounts);

        var request = new OperationRequest(
            """
            {
              __type(name: "Query") {
                fields {
                  name
                }
              }
            }
            """);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task TypeName_Field_On_QueryRoot()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(
            DemoSubgraphs.Accounts | DemoSubgraphs.Reviews2 | DemoSubgraphs.Products);

        var request = new OperationRequest(
            """
            query Introspect {
              __typename
            }
            """);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Forward_Nested_Variables()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(
            DemoSubgraphs.Accounts | DemoSubgraphs.Reviews2 | DemoSubgraphs.Products);

        var request = new OperationRequest(
            """
            query ProductReviews(
              $id: ID!
              $first: Int!
            ) {
              productById(id: $id) {
                id
                repeat(num: $first)
              }
            }
            """,
            variables: new Dictionary<string, object?>
            {
                ["id"] = "UHJvZHVjdDox",
                ["first"] = 1
            });

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Forward_Nested_Variables_No_OpName()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(
            DemoSubgraphs.Accounts | DemoSubgraphs.Reviews2 | DemoSubgraphs.Products);

        var request = new OperationRequest(
            """
            query (
              $id: ID!
              $first: Int!
            ) {
              productById(id: $id) {
                id
                repeat(num: $first)
              }
            }
            """,
            variables: new Dictionary<string, object?>
            {
                ["id"] = "UHJvZHVjdDox",
                ["first"] = 1
            });

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Forward_Nested_Variables_No_OpName_Two_RootSelections()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(
            DemoSubgraphs.Accounts | DemoSubgraphs.Reviews2 | DemoSubgraphs.Products);

        var request = new OperationRequest(
            """
            query (
              $id: ID!
              $first: Int!
            ) {
              a: productById(id: $id) {
                id
                repeat(num: $first)
              }
              b: productById(id: $id) {
                id
                repeat(num: $first)
              }
            }
            """,
            variables: new Dictionary<string, object?>
            {
                ["id"] = "UHJvZHVjdDox",
                ["first"] = 1
            });

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Forward_Nested_Node_Variables()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(
            DemoSubgraphs.Accounts | DemoSubgraphs.Reviews2 | DemoSubgraphs.Products);

        var request = new OperationRequest(
            """
            query ProductReviews(
              $id: ID!
              $first: Int!
            ) {
              node(id: $id) {
                ... on Product {
                  id
                  repeat(num: $first)
                }
              }
            }
            """,
            variables: new Dictionary<string, object?>
            {
                ["id"] = "UHJvZHVjdDox",
                ["first"] = 1
            });

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Forward_Nested_Object_Variables()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(
            DemoSubgraphs.Accounts | DemoSubgraphs.Reviews2 | DemoSubgraphs.Products);

        var request = new OperationRequest(
            """
            query ProductReviews(
              $id: ID!
              $first: Int!
            ) {
              productById(id: $id) {
                id
                repeatData(data: { data: { num: $first } }) {
                  data {
                    num
                  }
                }
              }
            }
            """,
            variables: new Dictionary<string, object?>
            {
                ["id"] = "UHJvZHVjdDox",
                ["first"] = 1
            });

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Require_Data_In_Context()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(
            DemoSubgraphs.Accounts | DemoSubgraphs.Reviews2 | DemoSubgraphs.Products | DemoSubgraphs.Shipping);

        var request = new OperationRequest(
            """
            query Requires {
              reviews {
                body
                author {
                  name
                  birthdate
                }
                product {
                  name
                  deliveryEstimate(zip: "12345") {
                    min
                    max
                  }
                }
              }
            }
            """);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Require_Data_In_Context_2()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(
            DemoSubgraphs.Accounts | DemoSubgraphs.Reviews2 | DemoSubgraphs.Products | DemoSubgraphs.Shipping);

        var request = new OperationRequest(
            """
            query Requires {
              reviews {
                body
                author {
                  name
                  birthdate
                }
                product {
                  deliveryEstimate(zip: "12345") {
                    min
                    max
                  }
                }
              }
            }
            """);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Require_Data_In_Context_3()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(
            DemoSubgraphs.Accounts | DemoSubgraphs.Reviews2 | DemoSubgraphs.Products | DemoSubgraphs.Shipping);

        var request = new OperationRequest(
            """
            query Large {
              users {
                id
                name
                birthdate
                reviews {
                  body
                  author {
                    name
                    birthdate
                  }
                  product {
                    id
                    name
                    deliveryEstimate(zip: "abc") {
                      max
                    }
                  }
                }
              }
            }
            """);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task GetFirstPage_With_After_Null()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(DemoSubgraphs.Appointment);

        var request = new OperationRequest(
            """
            query AfterNull($after: String) {
              appointments(after: $after) {
                nodes {
                  id
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["after"] = null });

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task QueryType_Parallel_Multiple_SubGraphs_WithArguments()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(
            DemoSubgraphs.Accounts | DemoSubgraphs.Reviews2 | DemoSubgraphs.Products | DemoSubgraphs.Shipping);

        var request = new OperationRequest(
            """
            query TopProducts {
              topProducts(first: 5) {
                weight
                deliveryEstimate(zip: "12345") {
                  min
                  max
                }
                reviews {
                  body
                }
              }
            }
            """);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Two_Arguments_Differing_Nullability_Does_Not_Duplicate_Forwarded_Variables()
    {
        // arrange
        using var gateway = await CreateDemoGatewayAsync(DemoSubgraphs.Accounts);

        var request = new OperationRequest(
            """
            query Test($number: Int!) {
              testWithTwoArgumentsDifferingNullability(first: $number, second: $number)
            }
            """,
            variables: new Dictionary<string, object?> { ["number"] = 1 });

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(request, s_graphQLEndpoint);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    private async Task<Gateway> CreateDemoGatewayAsync(DemoSubgraphs subgraphs)
    {
        var sourceSchemas = new List<(string SchemaName, Microsoft.AspNetCore.TestHost.TestServer Server)>();

        if (subgraphs.HasFlag(DemoSubgraphs.Accounts))
        {
            sourceSchemas.Add(("A", CreateSourceSchema("A", AccountsSchema)));
        }

        if (subgraphs.HasFlag(DemoSubgraphs.Reviews2))
        {
            sourceSchemas.Add(("B", CreateSourceSchema("B", Reviews2Schema)));
        }

        if (subgraphs.HasFlag(DemoSubgraphs.Products))
        {
            sourceSchemas.Add(("C", CreateSourceSchema("C", ProductsSchema)));
        }

        if (subgraphs.HasFlag(DemoSubgraphs.Shipping))
        {
            sourceSchemas.Add(("D", CreateSourceSchema("D", ShippingSchema)));
        }

        if (subgraphs.HasFlag(DemoSubgraphs.Appointment))
        {
            sourceSchemas.Add(("E", CreateSourceSchema("E", AppointmentSchema)));
        }

        if (sourceSchemas.Count is 0)
        {
            throw new ArgumentException("At least one subgraph must be specified.", nameof(subgraphs));
        }

        return await CreateCompositeSchemaAsync(sourceSchemas.ToArray());
    }

    [Flags]
    private enum DemoSubgraphs
    {
        Accounts = 1,
        Reviews2 = 2,
        Products = 4,
        Shipping = 8,
        Appointment = 16
    }

    private static readonly Uri s_graphQLEndpoint = new("http://localhost:5000/graphql");

    private const string AccountsSchema =
        """
        type Query {
          node(id: ID!): Node @lookup @shareable
          nodes(ids: [ID!]!): [Node]! @shareable
          userById(id: ID!): User!
          users: [User!]!
          testWithTwoArgumentsDifferingNullability(first: Int!, second: Int): String!
        }

        interface Node {
          id: ID!
        }

        type User implements Node {
          id: ID!
          name: String!
          username: String!
          birthdate: String!
        }
        """;

    private const string Reviews2Schema =
        """
        type Query {
          node(id: ID!): Node @lookup @shareable
          nodes(ids: [ID!]!): [Node]! @shareable
          reviews: [Review!]!
        }

        interface Node {
          id: ID!
        }

        type Review implements Node {
          id: ID!
          body: String!
          author: User!
          product: Product!
        }

        type User implements Node {
          id: ID!
          reviews: [Review!]!
          errorField: String @error
        }

        type Product implements Node {
          id: ID!
          reviews: [Review!]!
          reviewCount: Int!
        }
        """;

    private const string ProductsSchema =
        """
        type Query {
          node(id: ID!): Node @lookup @shareable
          nodes(ids: [ID!]!): [Node]! @shareable
          productById(id: ID!): Product @lookup @shareable
          topProducts(first: Int!): [Product!]!
        }

        interface Node {
          id: ID!
        }

        type Product implements Node {
          id: ID!
          name: String!
          size: Int! @shareable
          weight: Int! @shareable
          repeat(num: Int!): String!
          repeatData(data: RepeatDataInput!): RepeatDataPayload!
        }

        input RepeatDataInput {
          data: RepeatDataDataInput!
        }

        input RepeatDataDataInput {
          num: Int!
        }

        type RepeatDataPayload {
          data: RepeatDataData!
        }

        type RepeatDataData {
          num: Int!
        }
        """;

    private const string ShippingSchema =
        """
        type Query {
          node(id: ID!): Node @lookup @shareable
          nodes(ids: [ID!]!): [Node]! @shareable
        }

        interface Node {
          id: ID!
        }

        type Product implements Node {
          id: ID!
          size: Int! @shareable
          weight: Int! @shareable
          deliveryEstimate(
            size: Int
            weight: Int
            zip: String!): DeliveryEstimate!
        }

        type DeliveryEstimate {
          min: Int!
          max: Int!
        }
        """;

    private const string AppointmentSchema =
        """
        type Query {
          appointments(after: String): AppointmentConnection
        }

        type AppointmentConnection {
          nodes: [Appointment!]
        }

        type Appointment {
          id: ID!
        }
        """;
}
