using System.Net;
using System.Text;
using System.Text.Json;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.AspNetCore.Tests.Utilities.TestServerExtensions;

namespace HotChocolate.Transport.Http.Tests;

public class GraphQLHttpClientTests : ServerTestBase
{
    /// <inheritdoc />
    public GraphQLHttpClientTests(TestServerFactory serverFactory) : base(serverFactory) { }

    [Fact]
    public async Task Post_Http_200_Wrong_Content_Type()
    {
        // arrange
        var httpClient = new HttpClient(new CustomHttpClientHandler(HttpStatusCode.OK));

        const string query =
            """
            query {
              hero(episode: JEDI) {
                name
              }
            }
            """;

        var client = new DefaultGraphQLHttpClient(httpClient);

        // act
        var response = await client.PostAsync(query, "http://localhost:5000/graphql");

        async Task Error() => await response.ReadAsResultAsync();

        // assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(Error);
        Assert.Equal("Received a successful response with an unexpected content type.", exception.Message);
    }

    [Fact]
    public async Task Post_Http_404_Wrong_Content_Type()
    {
        var httpClient = new HttpClient(new CustomHttpClientHandler(HttpStatusCode.NotFound));

        const string query =
            """
            query {
              hero(episode: JEDI) {
                name
              }
            }
            """;

        var client = new DefaultGraphQLHttpClient(httpClient);

        // act
        var response = await client.PostAsync(query, "http://localhost:5000/graphql");

        async Task Error() => await response.ReadAsResultAsync();

        // assert
        await Assert.ThrowsAsync<HttpRequestException>(Error);
    }

    [Fact]
    public async Task Post_Transport_Error()
    {
        var httpClient = new HttpClient(new CustomHttpClientHandler());

        const string query =
            """
            query {
              hero(episode: JEDI) {
                name
              }
            }
            """;

        var client = new DefaultGraphQLHttpClient(httpClient);

        // act
        async Task Error() => await client.PostAsync(query, "http://localhost:5000/graphql");

        // assert
        var exception = await Assert.ThrowsAsync<Exception>(Error);
        Assert.Equal("Something went wrong", exception.Message);
    }

    [Fact]
    public async Task Post_GraphQL_Query_With_RequestUri()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var testServer = CreateStarWarsServer();
        var httpClient = testServer.CreateClient();

        const string query =
            """
            query {
              hero(episode: JEDI) {
                name
              }
            }
            """;

        var client = new DefaultGraphQLHttpClient(httpClient);

        // act
        using var response = await client.PostAsync(query, new Uri(CreateUrl("/graphql")), cts.Token);

        // assert
        using var body = await response.ReadAsResultAsync(cts.Token);
        body.MatchInlineSnapshot(
            """
            {
              "data": {
                "hero": {
                  "name": "R2-D2"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Post_GraphQL_Query_With_RequestUriString()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var testServer = CreateStarWarsServer();
        var httpClient = testServer.CreateClient();

        const string query =
            """
            query {
              hero(episode: JEDI) {
                name
              }
            }
            """;

        var client = new DefaultGraphQLHttpClient(httpClient);

        // act
        using var response = await client.PostAsync(query, CreateUrl("/graphql"), cts.Token);

        // assert
        using var body = await response.ReadAsResultAsync(cts.Token);
        body.MatchInlineSnapshot(
            """
            {
              "data": {
                "hero": {
                  "name": "R2-D2"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Post_GraphQL_Query_With_BaseAddress()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var testServer = CreateStarWarsServer();
        var httpClient = testServer.CreateClient();
        httpClient.BaseAddress = new Uri(CreateUrl("/graphql"));

        const string query =
            """
            query {
              hero(episode: JEDI) {
                name
              }
            }
            """;

        var client = new DefaultGraphQLHttpClient(httpClient);

        // act
        using var response = await client.PostAsync(query, cts.Token);

        // assert
        using var body = await response.ReadAsResultAsync(cts.Token);
        body.MatchInlineSnapshot(
            """
            {
              "data": {
                "hero": {
                  "name": "R2-D2"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Post_GraphQL_Query_With_Variables_With_RequestUri()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var testServer = CreateStarWarsServer();
        var httpClient = testServer.CreateClient();
        var client = new DefaultGraphQLHttpClient(httpClient);

        const string query =
            """
            query($episode: Episode!) {
              hero(episode: $episode) {
                name
              }
            }
            """;

        var variables = new Dictionary<string, object?>
        {
            ["episode"] = "JEDI"
        };

        var requestUri = new Uri(CreateUrl("/graphql"));

        // act
        var response = await client.PostAsync(query, variables, requestUri, cts.Token);

        // assert
        using var body = await response.ReadAsResultAsync(cts.Token);
        body.MatchInlineSnapshot(
            """
            {
              "data": {
                "hero": {
                  "name": "R2-D2"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Post_GraphQL_Query_With_Variables_With_RequestUriString()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var testServer = CreateStarWarsServer();
        var httpClient = testServer.CreateClient();
        var client = new DefaultGraphQLHttpClient(httpClient);

        const string query =
            """
            query($episode: Episode!) {
              hero(episode: $episode) {
                name
              }
            }
            """;

        var variables = new Dictionary<string, object?>
        {
            ["episode"] = "JEDI"
        };

        var requestUri = CreateUrl("/graphql");

        // act
        var response = await client.PostAsync(query, variables, requestUri, cts.Token);

        // assert
        using var body = await response.ReadAsResultAsync(cts.Token);
        body.MatchInlineSnapshot(
            """
            {
              "data": {
                "hero": {
                  "name": "R2-D2"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Post_GraphQL_Query_With_JsonElement_Variable()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var testServer = CreateStarWarsServer();
        var httpClient = testServer.CreateClient();
        var client = new DefaultGraphQLHttpClient(httpClient);

        const string query =
            """
            query($traits: JSON!) {
              heroByTraits(traits: $traits) {
                name
              }
            }
            """;

        var variables = new Dictionary<string, object?>
        {
            ["traits"] = JsonSerializer.SerializeToElement(new { lastJedi = true })
        };

        var requestUri = CreateUrl("/graphql");

        // act
        var response = await client.PostAsync(query, variables, requestUri, cts.Token);

        // assert
        using var body = await response.ReadAsResultAsync(cts.Token);
        body.MatchInlineSnapshot(
            """
            {
              "data": {
                "heroByTraits": {
                  "name": "Luke Skywalker"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Post_GraphQL_Query_With_Variables_With_BaseAddress()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var testServer = CreateStarWarsServer();
        var httpClient = testServer.CreateClient();
        httpClient.BaseAddress = new Uri(CreateUrl("/graphql"));
        var client = new DefaultGraphQLHttpClient(httpClient);

        const string query =
            """
            query($episode: Episode!) {
              hero(episode: $episode) {
                name
              }
            }
            """;

        var variables = new Dictionary<string, object?>
        {
            ["episode"] = "JEDI"
        };

        // act
        var response = await client.PostAsync(query, variables, cts.Token);

        // assert
        using var body = await response.ReadAsResultAsync(cts.Token);
        body.MatchInlineSnapshot(
            """
            {
              "data": {
                "hero": {
                  "name": "R2-D2"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Post_GraphQL_Query_With_OperationName()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var testServer = CreateStarWarsServer();
        var httpClient = testServer.CreateClient();
        var client = new DefaultGraphQLHttpClient(httpClient);

        var operation = new OperationRequest(
            """
            query A($episode: Episode!) {
              hero(episode: $episode) {
                A: name
              }
            }

            query B($episode: Episode!) {
              hero(episode: $episode) {
                B: name
              }
            }
            """,
            operationName: "B",
            variables: new Dictionary<string, object?>
            {
                ["episode"] = "JEDI"
            });

        var requestUri = new Uri(CreateUrl("/graphql"));

        // act
        var response = await client.PostAsync(operation, requestUri, cts.Token);

        // assert
        using var body = await response.ReadAsResultAsync(cts.Token);
        body.MatchInlineSnapshot(
            """
            {
              "data": {
                "hero": {
                  "B": "R2-D2"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Post_GraphQL_Query_With_OnError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var server = ServerFactory.Create(
            services => services
                .AddRouting()
                .AddGraphQLServer()
                .UseRequest(next => async context =>
                {
                    context.ContextData["mode"] = context.Request.ErrorHandlingMode;

                    await next(context);
                })
                .UseDefaultPipeline()
                .AddQueryType(desc =>
                {
                    desc.Name("Query");

                    desc.Field("errorHandlingMode")
                        .Resolve(ctx => (ErrorHandlingMode?)ctx.ContextData["mode"]);
                }),
            app => app
                .UseRouting()
                .UseEndpoints(e => e.MapGraphQL()));
        var httpClient = server.CreateClient();
        httpClient.BaseAddress = new Uri(CreateUrl("/graphql"));
        var client = new DefaultGraphQLHttpClient(httpClient);

        const string query =
            """
            query {
              errorHandlingMode
            }
            """;

        // act
        var response = await client.PostAsync(
            new OperationRequest(query, onError: ErrorHandlingMode.Halt),
            cts.Token);

        // assert
        using var body = await response.ReadAsResultAsync(cts.Token);
        body.MatchInlineSnapshot(
            """
            {
              "data": {
                "errorHandlingMode": "HALT"
              }
            }
            """);
    }

    [Fact]
    public async Task Get_GraphQL_Query_With_RequestUri()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var testServer = CreateStarWarsServer();
        var httpClient = testServer.CreateClient();

        const string query =
            """
            query {
              hero(episode: JEDI) {
                name
              }
            }
            """;

        var client = new DefaultGraphQLHttpClient(httpClient);

        // act
        using var response = await client.GetAsync(query, new Uri(CreateUrl("/graphql")), cts.Token);

        // assert
        using var body = await response.ReadAsResultAsync(cts.Token);
        body.MatchInlineSnapshot(
            """
            {
              "data": {
                "hero": {
                  "name": "R2-D2"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Get_GraphQL_Query_With_RequestUriString()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var testServer = CreateStarWarsServer();
        var httpClient = testServer.CreateClient();

        const string query =
            """
            query {
              hero(episode: JEDI) {
                name
              }
            }
            """;

        var client = new DefaultGraphQLHttpClient(httpClient);

        // act
        using var response = await client.GetAsync(query, CreateUrl("/graphql"), cts.Token);

        // assert
        using var body = await response.ReadAsResultAsync(cts.Token);
        body.MatchInlineSnapshot(
            """
            {
              "data": {
                "hero": {
                  "name": "R2-D2"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Get_GraphQL_Query_With_BaseAddress()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var testServer = CreateStarWarsServer();
        var httpClient = testServer.CreateClient();
        httpClient.BaseAddress = new Uri(CreateUrl("/graphql"));

        const string query =
            """
            query {
              hero(episode: JEDI) {
                name
              }
            }
            """;

        var client = new DefaultGraphQLHttpClient(httpClient);

        // act
        using var response = await client.GetAsync(query, cts.Token);

        // assert
        using var body = await response.ReadAsResultAsync(cts.Token);
        body.MatchInlineSnapshot(
            """
            {
              "data": {
                "hero": {
                  "name": "R2-D2"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Get_GraphQL_Query_With_Variables_With_RequestUri()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var testServer = CreateStarWarsServer();
        var httpClient = testServer.CreateClient();
        var client = new DefaultGraphQLHttpClient(httpClient);

        const string query =
            """
            query($episode: Episode!) {
              hero(episode: $episode) {
                name
              }
            }
            """;

        var variables = new Dictionary<string, object?>
        {
            ["episode"] = "JEDI"
        };

        var requestUri = new Uri(CreateUrl("/graphql"));

        // act
        var response = await client.GetAsync(query, variables, requestUri, cts.Token);

        // assert
        using var body = await response.ReadAsResultAsync(cts.Token);
        body.MatchInlineSnapshot(
            """
            {
              "data": {
                "hero": {
                  "name": "R2-D2"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Get_GraphQL_Query_With_Variables_With_RequestUriString()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var testServer = CreateStarWarsServer();
        var httpClient = testServer.CreateClient();
        var client = new DefaultGraphQLHttpClient(httpClient);

        const string query =
            """
            query($episode: Episode!) {
              hero(episode: $episode) {
                name
              }
            }
            """;

        var variables = new Dictionary<string, object?>
        {
            ["episode"] = "JEDI"
        };

        var requestUri = CreateUrl("/graphql");

        // act
        var response = await client.GetAsync(query, variables, requestUri, cts.Token);

        // assert
        using var body = await response.ReadAsResultAsync(cts.Token);
        body.MatchInlineSnapshot(
            """
            {
              "data": {
                "hero": {
                  "name": "R2-D2"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Get_GraphQL_Query_With_Variables_With_BaseAddress()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var testServer = CreateStarWarsServer();
        var httpClient = testServer.CreateClient();
        httpClient.BaseAddress = new Uri(CreateUrl("/graphql"));
        var client = new DefaultGraphQLHttpClient(httpClient);

        const string query =
            """
            query($episode: Episode!) {
              hero(episode: $episode) {
                name
              }
            }
            """;

        var variables = new Dictionary<string, object?>
        {
            ["episode"] = "JEDI"
        };

        // act
        var response = await client.GetAsync(query, variables, cts.Token);

        // assert
        using var body = await response.ReadAsResultAsync(cts.Token);
        body.MatchInlineSnapshot(
            """
            {
              "data": {
                "hero": {
                  "name": "R2-D2"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Get_GraphQL_Query_With_OperationName()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var testServer = CreateStarWarsServer();
        var httpClient = testServer.CreateClient();
        var client = new DefaultGraphQLHttpClient(httpClient);

        var operation = new OperationRequest(
            """
            query A($episode: Episode!) {
              hero(episode: $episode) {
                A: name
              }
            }

            query B($episode: Episode!) {
              hero(episode: $episode) {
                B: name
              }
            }
            """,
            operationName: "B",
            variables: new Dictionary<string, object?>
            {
                ["episode"] = "JEDI"
            });

        var requestUri = new Uri(CreateUrl("/graphql"));

        // act
        var response = await client.GetAsync(operation, requestUri, cts.Token);

        // assert
        using var body = await response.ReadAsResultAsync(cts.Token);
        body.MatchInlineSnapshot(
            """
            {
              "data": {
                "hero": {
                  "B": "R2-D2"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Get_GraphQL_Query_With_OnError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var server = ServerFactory.Create(
            services => services
                .AddRouting()
                .AddGraphQLServer()
                .UseRequest(next => async context =>
                {
                    context.ContextData["mode"] = context.Request.ErrorHandlingMode;

                    await next(context);
                })
                .UseDefaultPipeline()
                .AddQueryType(desc =>
                {
                    desc.Name("Query");

                    desc.Field("errorHandlingMode")
                        .Resolve(ctx => (ErrorHandlingMode?)ctx.ContextData["mode"]);
                }),
            app => app
                .UseRouting()
                .UseEndpoints(e => e.MapGraphQL()));
        var httpClient = server.CreateClient();
        httpClient.BaseAddress = new Uri(CreateUrl("/graphql"));
        var client = new DefaultGraphQLHttpClient(httpClient);

        const string query =
            """
            query {
              errorHandlingMode
            }
            """;

        // act
        var response = await client.GetAsync(
            new OperationRequest(query, onError: ErrorHandlingMode.Halt),
            cts.Token);

        // assert
        using var body = await response.ReadAsResultAsync(cts.Token);
        body.MatchInlineSnapshot(
            """
            {
              "data": {
                "errorHandlingMode": "HALT"
              }
            }
            """);
    }

    [Fact]
    public async Task Post_Subscription_Over_SSE()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var testServer = CreateStarWarsServer();
        var httpClient = testServer.CreateClient();
        httpClient.BaseAddress = new Uri(CreateUrl("/graphql"));

        const string subscriptionRequest =
            """
            subscription {
              onReview(episode: JEDI) {
                stars
              }
            }
            """;

        var mutationRequest = new OperationRequest(
            """
            mutation CreateReviewForEpisode(
                $ep: Episode!, $review: ReviewInput!) {
                createReview(episode: $ep, review: $review) {
                    stars
                    commentary
                }
            }
            """,
            variables: new Dictionary<string, object?>
            {
                ["ep"] = "JEDI",
                ["review"] = new Dictionary<string, object?>
                {
                    ["stars"] = 5,
                    ["commentary"] = "This is a great movie!"
                }
            });

        var client = new DefaultGraphQLHttpClient(httpClient);

        // act
        var subscriptionResponse = await client.PostAsync(subscriptionRequest, cts.Token);
        var mutationResponse = await client.PostAsync(mutationRequest, cts.Token);

        mutationResponse.EnsureSuccessStatusCode();

        // assert
        await foreach (var result in subscriptionResponse.ReadAsResultStreamAsync().WithCancellation(cts.Token))
        {
            result.MatchInlineSnapshot(
                """
                {
                  "data": {
                    "onReview": {
                      "stars": 5
                    }
                  }
                }
                """);
            break;
        }
    }

    [Fact]
    public async Task Get_Subscription_Over_SSE()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var testServer = CreateStarWarsServer();
        var httpClient = testServer.CreateClient();
        httpClient.BaseAddress = new Uri(CreateUrl("/graphql"));

        const string subscriptionRequest =
            """
            subscription {
              onReview(episode: JEDI) {
                stars
              }
            }
            """;

        var mutationRequest = new OperationRequest(
            """
            mutation CreateReviewForEpisode(
                $ep: Episode!, $review: ReviewInput!) {
                createReview(episode: $ep, review: $review) {
                    stars
                    commentary
                }
            }
            """,
            variables: new Dictionary<string, object?>
            {
                ["ep"] = "JEDI",
                ["review"] = new Dictionary<string, object?>
                {
                    ["stars"] = 5,
                    ["commentary"] = "This is a great movie!"
                }
            });

        var client = new DefaultGraphQLHttpClient(httpClient);

        // act
        var subscriptionResponse = await client.GetAsync(subscriptionRequest, cts.Token);
        var mutationResponse = await client.PostAsync(mutationRequest, cts.Token);

        mutationResponse.EnsureSuccessStatusCode();

        // assert
        var canceled = false;
        try
        {
            await foreach (var result in subscriptionResponse.ReadAsResultStreamAsync().WithCancellation(cts.Token))
            {
                result.MatchInlineSnapshot(
                    """
                    {
                      "data": {
                        "onReview": {
                          "stars": 5
                        }
                      }
                    }
                    """);
                await cts.CancelAsync();
            }
        }
        catch (OperationCanceledException)
        {
            canceled = true;
        }

        Assert.True(canceled, "Cancellation was received.");
    }

    [Fact]
    public async Task Get_Subscription_Over_SSE_With_Errors()
    {
        // arrange
        var snapshot = new Snapshot();

        using var cts = TestEnvironment.CreateCancellationTokenSource();
        using var server = ServerFactory.Create(
            services => services
                .AddRouting()
                .AddGraphQLServer()
                .AddHttpResponseFormatter()
                .AddQueryType(desc =>
                {
                    desc.Name("Query");

                    desc.Field("foo")
                        .Type<StringType>()
                        .Resolve(_ => new ValueTask<object?>("bar"));
                })
                .AddSubscriptionType<ErrorSubscription>(),
            app => app
                .UseRouting()
                .UseEndpoints(e => e.MapGraphQL()));

        var httpClient = server.CreateClient();
        httpClient.BaseAddress = new Uri(CreateUrl("/graphql"));

        const string subscriptionRequest =
            """
            subscription {
              onError
            }
            """;

        var client = new DefaultGraphQLHttpClient(httpClient);

        // act
        var subscriptionResponse = await client.PostAsync(subscriptionRequest, cts.Token);

        // assert
        await foreach (var result in subscriptionResponse.ReadAsResultStreamAsync().WithCancellation(cts.Token))
        {
            snapshot.Add(result);
        }

        await snapshot.MatchMarkdownAsync(cts.Token);
    }

    [Theory]
    [InlineData((string?)null)]
    [InlineData("application/pdf")]
    public async Task Post_GraphQL_FileUpload(string? contentType)
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5000));
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQLServer("test")
                .AddType<UploadType>()
                .AddQueryType<UploadTestQuery>());
        var httpClient = server.CreateClient();
        var client = new DefaultGraphQLHttpClient(httpClient);

        var stream = new MemoryStream("abc"u8.ToArray());

        var operation = new OperationRequest(
            """
            query ($upload: Upload!) {
              singleInfoUpload(file: $upload) {
                name
                content
                contentType
              }
            }
            """,
            variables: new Dictionary<string, object?>
            {
                ["upload"] = new FileReference(() => stream, "test.txt", contentType)
            });

        var requestUri = new Uri(CreateUrl("/test"));

        var request = new GraphQLHttpRequest(operation, requestUri)
        {
            Method = GraphQLHttpMethod.Post,
            EnableFileUploads = true
        };

        // act
        var response = await client.SendAsync(request, cts.Token);

        // assert
        using var body = await response.ReadAsResultAsync(cts.Token);
        body.MatchInlineSnapshot(
            $$$"""
            {
              "data": {
                "singleInfoUpload": {
                  "name": "test.txt",
                  "content": "abc",
                  "contentType": "{{{contentType}}}"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Post_GraphQL_FileUpload_With_ObjectValueNode()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5000));
        using var testServer = CreateStarWarsServer();
        var httpClient = testServer.CreateClient();
        var client = new DefaultGraphQLHttpClient(httpClient);

        var stream = new MemoryStream("abc"u8.ToArray());

        var operation = new OperationRequest(
            """
            query ($upload: Upload!) {
              singleUpload(file: $upload)
            }
            """,
            null,
            null,
            null,
            variables: new ObjectValueNode(
                new ObjectFieldNode(
                    "upload",
                    new FileReferenceNode(() => stream, "test.txt"))),
            extensions: null);

        var requestUri = new Uri(CreateUrl("/upload"));

        var request = new GraphQLHttpRequest(operation, requestUri)
        {
            Method = GraphQLHttpMethod.Post,
            EnableFileUploads = true
        };

        // act
        var response = await client.SendAsync(request, cts.Token);

        // assert
        using var body = await response.ReadAsResultAsync(cts.Token);
        body.MatchInlineSnapshot(
            """
            {
              "data": {
                "singleUpload": "abc"
              }
            }
            """);
    }

    [Fact]
    public async Task Post_Subscription_Over_JsonLines()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var testServer = CreateStarWarsServer();
        var httpClient = testServer.CreateClient();
        httpClient.BaseAddress = new Uri(CreateUrl("/graphql"));

        var subscriptionRequest = new GraphQLHttpRequest(
            new OperationRequest(
                """
                subscription {
                  onReview(episode: JEDI) {
                    stars
                  }
                }
                """))
        {
            Method = GraphQLHttpMethod.Post,
            Accept = GraphQLHttpRequest.GraphQLOverHttp
        };

        var mutationRequest = new OperationRequest(
            """
            mutation CreateReviewForEpisode(
                $ep: Episode!, $review: ReviewInput!) {
                createReview(episode: $ep, review: $review) {
                    stars
                    commentary
                }
            }
            """,
            variables: new Dictionary<string, object?>
            {
                ["ep"] = "JEDI",
                ["review"] = new Dictionary<string, object?>
                {
                    ["stars"] = 5,
                    ["commentary"] = "This is a great movie!"
                }
            });

        var client = new DefaultGraphQLHttpClient(httpClient);

        // act
        var subscriptionResponse = await client.SendAsync(subscriptionRequest, cts.Token);
        var mutationResponse = await client.PostAsync(mutationRequest, cts.Token);

        mutationResponse.EnsureSuccessStatusCode();

        // assert
        await foreach (var result in subscriptionResponse.ReadAsResultStreamAsync().WithCancellation(cts.Token))
        {
            result.MatchInlineSnapshot(
                """
                {
                  "data": {
                    "onReview": {
                      "stars": 5
                    }
                  }
                }
                """);
            break;
        }
    }

    private class CustomHttpClientHandler(HttpStatusCode? httpStatusCode = null) : HttpClientHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (httpStatusCode.HasValue)
            {
                return Task.FromResult(new HttpResponseMessage(httpStatusCode.Value));
            }

            throw new Exception("Something went wrong");
        }
    }

    public class ErrorSubscription
    {
        public async IAsyncEnumerable<string> CreateStream()
        {
            yield return "hello1";

            yield return "hello2";

            await Task.Delay(1000);

            throw new Exception("Boom!");
        }

        [Subscribe(With = nameof(CreateStream))]
        public string OnError([EventMessage] string message)
            => message;
    }

    public class UploadTestQuery
    {
        public async Task<FileInfoOutput> SingleInfoUpload(IFile file)
        {
            await using var stream = file.OpenReadStream();
            using var sr = new StreamReader(stream, Encoding.UTF8);
            return new FileInfoOutput
            {
                Content = await sr.ReadToEndAsync(),
                ContentType = file.ContentType ?? string.Empty,
                Name = file.Name
            };
        }

        public class FileInfoOutput
        {
            public string? Content { get; init; }
            public string? ContentType { get; init; }
            public string? Name { get; init; }
        }
    }
}
