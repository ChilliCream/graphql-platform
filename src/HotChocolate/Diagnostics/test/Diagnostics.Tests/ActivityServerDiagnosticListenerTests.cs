using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Transport.Http;
using static HotChocolate.Diagnostics.ActivityTestHelper;
using OperationRequest = HotChocolate.Transport.OperationRequest;

namespace HotChocolate.Diagnostics;

[Collection("Instrumentation")]
public class ActivityServerDiagnosticListenerTests(TestServerFactory serverFactory) : ServerTestBase(serverFactory)
{
    private static readonly Uri s_url = new("http://localhost:5000/graphql");

    [Fact]
    public async Task Http_Post_SingleRequest_GetHeroName_Default()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateInstrumentedServer();
            using var client = GraphQLHttpClient.Create(server.CreateClient());

            // act
            var request = new OperationRequest(
                @"
                {
                    hero {
                        name
                    }
                }");
            using var result = await client.PostAsync(request, s_url);
            await result.ReadAsResultAsync();

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Http_Post_SingleRequest_GetHeroName()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateInstrumentedServer(
                o => o.Scopes = ActivityScopes.All);
            using var client = GraphQLHttpClient.Create(server.CreateClient());

            // act
            var request = new OperationRequest(
                @"
                {
                    hero {
                        name
                    }
                }");
            using var result = await client.PostAsync(request, s_url);
            await result.ReadAsResultAsync();

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Http_Get_SingleRequest_GetHeroName()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateInstrumentedServer(
                o => o.Scopes = ActivityScopes.All);
            using var client = GraphQLHttpClient.Create(server.CreateClient());

            // act
            var request = new OperationRequest(
                @"
                {
                    hero {
                        name
                    }
                }");
            using var result = await client.GetAsync(request, s_url);
            await result.ReadAsResultAsync();

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Http_Post_Variables_Are_Not_Automatically_Added_To_Activities()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateInstrumentedServer(
                o => o.Scopes = ActivityScopes.All);
            using var client = GraphQLHttpClient.Create(server.CreateClient());

            // act
            var request = new OperationRequest(
                query: @"
                query ($episode: Episode!) {
                    hero(episode: $episode) {
                        name
                    }
                }",
                variables: new Dictionary<string, object?> { { "episode", "NEW_HOPE" } });
            using var result = await client.PostAsync(request, s_url);
            await result.ReadAsResultAsync();

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Http_Post_Add_Variables_To_Http_Activity()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateInstrumentedServer(
                o =>
                {
                    o.Scopes = ActivityScopes.All;
                    o.RequestDetails = RequestDetails.Default | RequestDetails.Variables;
                });
            using var client = GraphQLHttpClient.Create(server.CreateClient());

            // act
            var request = new OperationRequest(
                query: @"
                query ($episode: Episode!) {
                    hero(episode: $episode) {
                        name
                    }
                }",
                variables: new Dictionary<string, object?> { { "episode", "NEW_HOPE" } });
            using var result = await client.PostAsync(request, s_url);
            await result.ReadAsResultAsync();

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Http_Post_With_Extensions_Map()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateInstrumentedServer(
                o => o.Scopes = ActivityScopes.All);
            using var client = GraphQLHttpClient.Create(server.CreateClient());

            // act
            var request = new OperationRequest(
                query: @"
                query ($episode: Episode!) {
                    hero(episode: $episode) {
                        name
                    }
                }",
                variables: new Dictionary<string, object?> { { "episode", "NEW_HOPE" } },
                extensions: new Dictionary<string, object?> { { "test", "abc" } });
            using var result = await client.PostAsync(request, s_url);
            await result.ReadAsResultAsync();

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Http_Get_SDL_Download()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateInstrumentedServer(o => o.Scopes = ActivityScopes.All);
            var url = TestServerExtensions.CreateUrl("/graphql?sdl");
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // act
            var response = await server.CreateClient().SendAsync(request);

            // assert
            await response.Content.ReadAsStringAsync();

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Http_Post_Capture_Deferred_Response()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateInstrumentedServer(o => o.Scopes = ActivityScopes.All);

            // act
            await server.PostRawAsync(new ClientQueryRequest
            {
                Query =
                    """
                    {
                        hero(episode: NEW_HOPE) {
                            name
                            ... on Droid @defer(label: "my_id") {
                                id
                            }
                        }
                    }
                    """
            });

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Http_Post_Ensure_List_Path_Is_Correctly_Built()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateInstrumentedServer(o => o.Scopes = ActivityScopes.All);

            // act
            await server.PostRawAsync(new ClientQueryRequest
            {
                Query = @"
                {
                    hero(episode: NEW_HOPE)
                    {
                        name
                        friends {
                            nodes {
                                name
                                friends {
                                    nodes {
                                        name
                                    }
                                }
                            }
                        }
                    }
                }"
            });

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Http_Post_Parser_Error()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateInstrumentedServer(o => o.Scopes = ActivityScopes.All);

            // act
            await server.PostRawAsync(new ClientQueryRequest
            {
                // lang=text
                Query = @"
                {
                    hero(episode: NEW_HOPE)
                    {
                        name
                        friends {
                            nodes {
                                name
                                friends {
                                    1nodes {
                                        name
                                    }
                                }
                            }
                        }
                    }
                }"
            });

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task RequestDetails_None_ExcludesAllDetails()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateInstrumentedServer(
                o =>
                {
                    o.Scopes = ActivityScopes.All;
                    o.RequestDetails = RequestDetails.None;
                });
            using var client = GraphQLHttpClient.Create(server.CreateClient());

            // act
            var request = new OperationRequest(
                query: @"
                query GetHero($episode: Episode!) {
                    hero(episode: $episode) {
                        name
                    }
                }",
                variables: new Dictionary<string, object?> { { "episode", "NEW_HOPE" } },
                extensions: new Dictionary<string, object?> { { "test", "abc" } });
            using var result = await client.PostAsync(request, s_url);
            await result.ReadAsResultAsync();

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task RequestDetails_All_IncludesAllDetails()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateInstrumentedServer(
                o =>
                {
                    o.Scopes = ActivityScopes.All;
                    o.RequestDetails = RequestDetails.All;
                });
            using var client = GraphQLHttpClient.Create(server.CreateClient());

            // act
            var request = new OperationRequest(
                query: @"
                query GetHero($episode: Episode!) {
                    hero(episode: $episode) {
                        name
                    }
                }",
                variables: new Dictionary<string, object?> { { "episode", "NEW_HOPE" } },
                extensions: new Dictionary<string, object?> { { "test", "abc" } });
            using var result = await client.PostAsync(request, s_url);
            await result.ReadAsResultAsync();

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task RequestDetails_DocumentOnly_IncludesDocumentTag()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateInstrumentedServer(
                o =>
                {
                    o.Scopes = ActivityScopes.All;
                    o.RequestDetails = RequestDetails.Document;
                });
            using var client = GraphQLHttpClient.Create(server.CreateClient());

            // act
            var request = new OperationRequest(
                @"
                {
                    hero {
                        name
                    }
                }");
            using var result = await client.PostAsync(request, s_url);
            await result.ReadAsResultAsync();

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task RequestDetails_Default_IncludesIdHashOperationNameExtensions()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateInstrumentedServer(
                o => o.Scopes = ActivityScopes.All);
            using var client = GraphQLHttpClient.Create(server.CreateClient());

            // act
            var request = new OperationRequest(
                query: @"
                query GetHero {
                    hero {
                        name
                    }
                }",
                extensions: new Dictionary<string, object?> { { "test", "abc" } });
            using var result = await client.PostAsync(request, s_url);
            await result.ReadAsResultAsync();

            // assert
            activities.MatchSnapshot();
        }
    }

    private TestServer CreateInstrumentedServer(
        Action<InstrumentationOptions>? options = null)
        => CreateStarWarsServer(
                configureServices: services =>
                    services
                        .AddGraphQLServer()
                        .AddInstrumentation(options)
                        .ModifyPagingOptions(o => o.RequirePagingBoundaries = false)
                        .ModifyOptions(
                            o =>
                            {
                                o.EnableDefer = true;
                                o.EnableStream = true;
                            }));
}
