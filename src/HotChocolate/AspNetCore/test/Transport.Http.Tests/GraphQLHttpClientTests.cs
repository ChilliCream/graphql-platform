using System.Text.Json;
using CookieCrumble;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Language;
using static HotChocolate.AspNetCore.Tests.Utilities.TestServerExtensions;

namespace HotChocolate.Transport.Http.Tests;

public class GraphQLHttpClientTests : ServerTestBase
{
    /// <inheritdoc />
    public GraphQLHttpClientTests(TestServerFactory serverFactory) : base(serverFactory) { }

    [Fact]
    public async Task Post_GraphQL_Query_With_RequestUri()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var testServer = CreateStarWarsServer();
        var httpClient = testServer.CreateClient();

        var query =
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
            Data: {"hero":{"name":"R2-D2"}}
            """);
    }

    [Fact]
    public async Task Post_GraphQL_Query_With_RequestUriString()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var testServer = CreateStarWarsServer();
        var httpClient = testServer.CreateClient();

        var query =
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
            Data: {"hero":{"name":"R2-D2"}}
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

        var query =
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
            Data: {"hero":{"name":"R2-D2"}}
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

        var query =
            """
            query($episode: Episode!) {
              hero(episode: $episode) {
                name
              }
            }
            """;

        var variables = new Dictionary<string, object?>
        {
            ["episode"] = "JEDI",
        };

        var requestUri = new Uri(CreateUrl("/graphql"));

        // act
        var response = await client.PostAsync(query, variables, requestUri, cts.Token);

        // assert
        using var body = await response.ReadAsResultAsync(cts.Token);
        body.MatchInlineSnapshot(
            """
            Data: {"hero":{"name":"R2-D2"}}
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

        var query =
            """
            query($episode: Episode!) {
              hero(episode: $episode) {
                name
              }
            }
            """;

        var variables = new Dictionary<string, object?>
        {
            ["episode"] = "JEDI",
        };

        var requestUri = CreateUrl("/graphql");

        // act
        var response = await client.PostAsync(query, variables, requestUri, cts.Token);

        // assert
        using var body = await response.ReadAsResultAsync(cts.Token);
        body.MatchInlineSnapshot(
            """
            Data: {"hero":{"name":"R2-D2"}}
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

        var query =
            """
            query($traits: JSON!) {
              heroByTraits(traits: $traits) {
                name
              }
            }
            """;

        var variables = new Dictionary<string, object?>
        {
            ["traits"] = JsonSerializer.SerializeToElement(new { lastJedi = true, }),
        };

        var requestUri = CreateUrl("/graphql");

        // act
        var response = await client.PostAsync(query, variables, requestUri, cts.Token);

        // assert
        using var body = await response.ReadAsResultAsync(cts.Token);
        body.MatchInlineSnapshot(
            """
            Data: {"heroByTraits":{"name":"Luke Skywalker"}}
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

        var query =
            """
            query($episode: Episode!) {
              hero(episode: $episode) {
                name
              }
            }
            """;

        var variables = new Dictionary<string, object?>
        {
            ["episode"] = "JEDI",
        };

        // act
        var response = await client.PostAsync(query, variables, cts.Token);

        // assert
        using var body = await response.ReadAsResultAsync(cts.Token);
        body.MatchInlineSnapshot(
            """
            Data: {"hero":{"name":"R2-D2"}}
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
            variables: new Dictionary<string, object?>
            {
                ["episode"] = "JEDI",
            },
            operationName: "B");

        var requestUri = new Uri(CreateUrl("/graphql"));

        // act
        var response = await client.PostAsync(operation, requestUri, cts.Token);

        // assert
        using var body = await response.ReadAsResultAsync(cts.Token);
        body.MatchInlineSnapshot(
            """
            Data: {"hero":{"B":"R2-D2"}}
            """);
    }

    [Fact]
    public async Task Get_GraphQL_Query_With_RequestUri()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var testServer = CreateStarWarsServer();
        var httpClient = testServer.CreateClient();

        var query =
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
            Data: {"hero":{"name":"R2-D2"}}
            """);
    }

    [Fact]
    public async Task Get_GraphQL_Query_With_RequestUriString()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var testServer = CreateStarWarsServer();
        var httpClient = testServer.CreateClient();

        var query =
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
            Data: {"hero":{"name":"R2-D2"}}
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

        var query =
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
            Data: {"hero":{"name":"R2-D2"}}
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

        var query =
            """
            query($episode: Episode!) {
              hero(episode: $episode) {
                name
              }
            }
            """;

        var variables = new Dictionary<string, object?>
        {
            ["episode"] = "JEDI",
        };

        var requestUri = new Uri(CreateUrl("/graphql"));

        // act
        var response = await client.GetAsync(query, variables, requestUri, cts.Token);

        // assert
        using var body = await response.ReadAsResultAsync(cts.Token);
        body.MatchInlineSnapshot(
            """
            Data: {"hero":{"name":"R2-D2"}}
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

        var query =
            """
            query($episode: Episode!) {
              hero(episode: $episode) {
                name
              }
            }
            """;

        var variables = new Dictionary<string, object?>
        {
            ["episode"] = "JEDI",
        };

        var requestUri = CreateUrl("/graphql");

        // act
        var response = await client.GetAsync(query, variables, requestUri, cts.Token);

        // assert
        using var body = await response.ReadAsResultAsync(cts.Token);
        body.MatchInlineSnapshot(
            """
            Data: {"hero":{"name":"R2-D2"}}
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

        var query =
            """
            query($episode: Episode!) {
              hero(episode: $episode) {
                name
              }
            }
            """;

        var variables = new Dictionary<string, object?>
        {
            ["episode"] = "JEDI",
        };

        // act
        var response = await client.GetAsync(query, variables, cts.Token);

        // assert
        using var body = await response.ReadAsResultAsync(cts.Token);
        body.MatchInlineSnapshot(
            """
            Data: {"hero":{"name":"R2-D2"}}
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
            variables: new Dictionary<string, object?>
            {
                ["episode"] = "JEDI",
            },
            operationName: "B");

        var requestUri = new Uri(CreateUrl("/graphql"));

        // act
        var response = await client.GetAsync(operation, requestUri, cts.Token);

        // assert
        using var body = await response.ReadAsResultAsync(cts.Token);
        body.MatchInlineSnapshot(
            """
            Data: {"hero":{"B":"R2-D2"}}
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

        var subscriptionRequest =
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
                    ["commentary"] = "This is a great movie!",
                },
            });

        var client = new DefaultGraphQLHttpClient(httpClient);

        // act
        var subscriptionResponse = await client.PostAsync(subscriptionRequest, cts.Token);
        var mutationResponse = await client.PostAsync(mutationRequest, cts.Token);

        mutationResponse.EnsureSuccessStatusCode();

        // assert
        await foreach (var result in subscriptionResponse.ReadAsResultStreamAsync(cts.Token))
        {
            result.MatchInlineSnapshot(
                """
                Data: {"onReview":{"stars":5}}
                """);
            cts.Cancel();
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

        var subscriptionRequest =
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
                    ["commentary"] = "This is a great movie!",
                },
            });

        var client = new DefaultGraphQLHttpClient(httpClient);

        // act
        var subscriptionResponse = await client.GetAsync(subscriptionRequest, cts.Token);
        var mutationResponse = await client.PostAsync(mutationRequest, cts.Token);

        mutationResponse.EnsureSuccessStatusCode();

        // assert
        await foreach (var result in subscriptionResponse.ReadAsResultStreamAsync(cts.Token))
        {
            result.MatchInlineSnapshot(
                """
                Data: {"onReview":{"stars":5}}
                """);
            cts.Cancel();
        }
    }

    [Fact]
    public async Task Post_GraphQL_FileUpload()
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
            variables: new Dictionary<string, object?>
            {
                ["upload"] = new FileReference(() => stream, "test.txt"),
            });

        var requestUri = new Uri(CreateUrl("/upload"));

        var request = new GraphQLHttpRequest(operation, requestUri)
        {
            Method = GraphQLHttpMethod.Post,
            EnableFileUploads = true,
        };

        // act
        var response = await client.SendAsync(request, cts.Token);

        // assert
        using var body = await response.ReadAsResultAsync(cts.Token);
        body.MatchInlineSnapshot(
            """
            Data: {"singleUpload":"abc"}
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
            variables: new ObjectValueNode(
                new ObjectFieldNode(
                    "upload",
                    new FileReferenceNode(() => stream, "test.txt"))),
            extensions: null);

        var requestUri = new Uri(CreateUrl("/upload"));

        var request = new GraphQLHttpRequest(operation, requestUri)
        {
            Method = GraphQLHttpMethod.Post,
            EnableFileUploads = true,
        };

        // act
        var response = await client.SendAsync(request, cts.Token);

        // assert
        using var body = await response.ReadAsResultAsync(cts.Token);
        body.MatchInlineSnapshot(
            """
            Data: {"singleUpload":"abc"}
            """);
    }
}
