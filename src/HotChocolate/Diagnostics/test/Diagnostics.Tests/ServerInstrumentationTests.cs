using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.AspNetCore.Tests.Utilities;
using static HotChocolate.Diagnostics.ActivityTestHelper;

namespace HotChocolate.Diagnostics;

[Collection("Instrumentation")]
public class ServerInstrumentationTests : ServerTestBase
{
    public ServerInstrumentationTests(TestServerFactory serverFactory)
        : base(serverFactory)
    {
    }

    [Fact]
    public async Task Http_Post_SingleRequest_GetHeroName_Default()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateInstrumentedServer();

            // act
            await server.PostAsync(new ClientQueryRequest
            {
                Query = @"
                {
                    hero {
                        name
                    }
                }"
            });

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

            // act
            await server.PostAsync(new ClientQueryRequest
            {
                Query = @"
                {
                    hero {
                        name
                    }
                }"
            });

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

            // act
            await server.GetAsync(new ClientQueryRequest
            {
                Query = @"
                {
                    hero {
                        name
                    }
                }"
            });

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

            // act
            await server.PostAsync(new ClientQueryRequest
            {
                Query = @"
                query ($episode: Episode!) {
                    hero(episode: $episode) {
                        name
                    }
                }",
                Variables = new Dictionary<string, object?> { { "episode", "NEW_HOPE" } }
            });

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

            // act
            await server.PostAsync(new ClientQueryRequest
            {
                Query = @"
                query ($episode: Episode!) {
                    hero(episode: $episode) {
                        name
                    }
                }",
                Variables = new Dictionary<string, object?> { { "episode", "NEW_HOPE" } }
            });

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

            // act
            await server.PostAsync(new ClientQueryRequest
            {
                Query = @"
                query ($episode: Episode!) {
                    hero(episode: $episode) {
                        name
                    }
                }",
                Variables = new Dictionary<string, object?> { { "episode", "NEW_HOPE" } },
                Extensions = new Dictionary<string, object?> { { "test", "abc" } }
            });

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

            // act
            await server.PostAsync(new ClientQueryRequest
            {
                Query = @"
                query GetHero($episode: Episode!) {
                    hero(episode: $episode) {
                        name
                    }
                }",
                Variables = new Dictionary<string, object?> { { "episode", "NEW_HOPE" } },
                Extensions = new Dictionary<string, object?> { { "test", "abc" } }
            });

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

            // act
            await server.PostAsync(new ClientQueryRequest
            {
                Query = @"
                query GetHero($episode: Episode!) {
                    hero(episode: $episode) {
                        name
                    }
                }",
                Variables = new Dictionary<string, object?> { { "episode", "NEW_HOPE" } },
                Extensions = new Dictionary<string, object?> { { "test", "abc" } }
            });

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

            // act
            await server.PostAsync(new ClientQueryRequest
            {
                Query = @"
                {
                    hero {
                        name
                    }
                }"
            });

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

            // act
            await server.PostAsync(new ClientQueryRequest
            {
                Query = @"
                query GetHero {
                    hero {
                        name
                    }
                }",
                Extensions = new Dictionary<string, object?> { { "test", "abc" } }
            });

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
