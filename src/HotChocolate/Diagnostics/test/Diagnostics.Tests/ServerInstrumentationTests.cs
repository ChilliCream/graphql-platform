using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.AspNetCore.Tests.Utilities;
using Snapshooter;
using Snapshooter.Xunit;
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
                }",
            });

            // assert
#if NET7_0_OR_GREATER
            activities.MatchSnapshot(new SnapshotNameExtension("_NET7"));
#else
            activities.MatchSnapshot();
#endif
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
                }",
            });

            // assert
#if NET7_0_OR_GREATER
            activities.MatchSnapshot(new SnapshotNameExtension("_NET7"));
#else
            activities.MatchSnapshot();
#endif
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
                }",
            });

            // assert
#if NET7_0_OR_GREATER
            activities.MatchSnapshot(new SnapshotNameExtension("_NET7"));
#else
            activities.MatchSnapshot();
#endif
        }
    }

    [Fact]
    public async Task Http_Post_variables_are_not_automatically_added_to_activities()
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
                Variables = new Dictionary<string, object?> { { "episode", "NEW_HOPE" }, },
            });

            // assert
#if NET7_0_OR_GREATER
            activities.MatchSnapshot(new SnapshotNameExtension("_NET7"));
#else
            activities.MatchSnapshot();
#endif
        }
    }

    [Fact]
    public async Task Http_Post_add_variables_to_http_activity()
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
                Variables = new Dictionary<string, object?> { { "episode", "NEW_HOPE" }, },
            });

            // assert
#if NET7_0_OR_GREATER
            activities.MatchSnapshot(new SnapshotNameExtension("_NET7"));
#else
            activities.MatchSnapshot();
#endif
        }
    }

    [Fact]
    public async Task Http_Post_add_query_to_http_activity()
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
                Variables = new Dictionary<string, object?> { { "episode", "NEW_HOPE" }, },
            });

            // assert
#if NET7_0_OR_GREATER
            activities.MatchSnapshot(new SnapshotNameExtension("_NET7"));
#else
            activities.MatchSnapshot();
#endif
        }
    }

    [Fact]
    public async Task Http_Post_with_extensions_map()
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
                Variables = new Dictionary<string, object?> { { "episode", "NEW_HOPE" }, },
                Extensions = new Dictionary<string, object?> { { "test", "abc" }, },
            });

            // assert
#if NET7_0_OR_GREATER
            activities.MatchSnapshot(new SnapshotNameExtension("_NET7"));
#else
            activities.MatchSnapshot();
#endif
        }
    }

    [Fact]
    public async Task Http_Get_SDL_download()
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
#if NET7_0_OR_GREATER
            activities.MatchSnapshot(new SnapshotNameExtension("_NET7"));
#else
            activities.MatchSnapshot();
#endif
        }
    }

    [Fact]
    public async Task Http_Post_capture_deferred_response()
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
                        ... on Droid @defer(label: ""my_id"")
                        {
                            id
                        }
                    }
                }",
            });

            // assert
#if NET7_0_OR_GREATER
            activities.MatchSnapshot(new SnapshotNameExtension("_NET7"));
#else
            activities.MatchSnapshot();
#endif
        }
    }

    [Fact]
    public async Task Http_Post_ensure_list_path_is_correctly_built()
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
                }",
            });

            // assert
#if NET7_0_OR_GREATER
            activities.MatchSnapshot(new SnapshotNameExtension("_NET7"));
#else
            activities.MatchSnapshot();
#endif
        }
    }

    [Fact]
    public async Task Http_Post_parser_error()
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
                                    1nodes {
                                        name
                                    }
                                }
                            }
                        }
                    }
                }",
            });

            // assert
#if NET7_0_OR_GREATER
            activities.MatchSnapshot(new SnapshotNameExtension("_NET7"));
#else
            activities.MatchSnapshot();
#endif
        }
    }

    [Fact]
    public async Task Parsing_error_when_rename_root_is_activated()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateInstrumentedServer(
                o =>
                {
                    o.Scopes = ActivityScopes.All;
                    o.RenameRootActivity = true;
                });

            // act
            await server.PostRawAsync(new ClientQueryRequest
            {
                Query = @"
                {
                    1
                }",
            });

            // assert
#if NET7_0_OR_GREATER
            activities.MatchSnapshot(new SnapshotNameExtension("_NET7"));
#else
            activities.MatchSnapshot();
#endif
        }
    }

    [Fact]
    public async Task Validation_error_when_rename_root_is_activated()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateInstrumentedServer(
                o =>
                {
                    o.Scopes = ActivityScopes.All;
                    o.RenameRootActivity = true;
                });

            // act
            await server.PostRawAsync(new ClientQueryRequest
            {
                Query = @"
                {
                    abc
                }",
            });

            // assert
#if NET7_0_OR_GREATER
            activities.MatchSnapshot(new SnapshotNameExtension("_NET7"));
#else
            activities.MatchSnapshot();
#endif
        }
    }

    private TestServer CreateInstrumentedServer(
        Action<InstrumentationOptions>? options = default)
        => CreateStarWarsServer(
                configureServices: services =>
                    services
                        .AddGraphQLServer()
                        .AddInstrumentation(options)
                        .ModifyOptions(
                            o =>
                            {
                                o.EnableDefer = true;
                                o.EnableStream = true;
                            }));
}
