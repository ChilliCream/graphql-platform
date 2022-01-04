using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.AspNetCore.Utilities;
using Snapshooter.Xunit;
using Xunit;
using static HotChocolate.Diagnostics.ActivityTestHelper;
using System;
using System.Net.Http;

namespace HotChocolate.Diagnostics;

public class ServerInstrumentationTests : ServerTestBase
{
    public ServerInstrumentationTests(TestServerFactory serverFactory)
        : base(serverFactory)
    {
    }

    [Fact]
    public async Task Http_Post_SingleRequest_GetHeroName()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using TestServer server = CreateInstrumentedServer();

            // act
            ClientQueryResult result =
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
            using TestServer server = CreateInstrumentedServer();

            // act
            ClientQueryResult result =
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
    public async Task Http_Post_variables_are_not_automatically_added_to_activities()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using TestServer server = CreateInstrumentedServer();

            // act
            ClientQueryResult result =
                await server.PostAsync(new ClientQueryRequest
                {
                    Query = @"
                    query ($episode: Episode!) {
                        hero(episode: $episode) {
                            name
                        }
                    }",
                    Variables = new Dictionary<string, object> { { "episode", "NEW_HOPE" } }
                });

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Http_Post_add_variables_to_http_activity()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using TestServer server = CreateInstrumentedServer(
                o => o.RequestDetails = RequestDetails.Default | RequestDetails.Variables);

            // act
            ClientQueryResult result =
                await server.PostAsync(new ClientQueryRequest
                {
                    Query = @"
                    query ($episode: Episode!) {
                        hero(episode: $episode) {
                            name
                        }
                    }",
                    Variables = new Dictionary<string, object> { { "episode", "NEW_HOPE" } }
                });

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Http_Post_add_query_to_http_activity()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using TestServer server = CreateInstrumentedServer(
                o => o.RequestDetails = RequestDetails.Default | RequestDetails.Query);

            // act
            ClientQueryResult result =
                await server.PostAsync(new ClientQueryRequest
                {
                    Query = @"
                    query ($episode: Episode!) {
                        hero(episode: $episode) {
                            name
                        }
                    }",
                    Variables = new Dictionary<string, object> { { "episode", "NEW_HOPE" } }
                });

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Http_Post_with_extensions_map()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using TestServer server = CreateInstrumentedServer();

            // act
            ClientQueryResult result =
                await server.PostAsync(new ClientQueryRequest
                {
                    Query = @"
                    query ($episode: Episode!) {
                        hero(episode: $episode) {
                            name
                        }
                    }",
                    Variables = new Dictionary<string, object> { { "episode", "NEW_HOPE" } },
                    Extensions = new Dictionary<string, object> { { "test", "abc" } }
                });

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Http_Get_SDL_download()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using TestServer server = CreateInstrumentedServer();
            var url = TestServerExtensions.CreateUrl("/graphql?sdl");
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // act
            HttpResponseMessage response = await server.CreateClient().SendAsync(request);

            // assert
            await response.Content.ReadAsStringAsync();
         
            // assert
            activities.MatchSnapshot();
        }
    }

    private TestServer CreateInstrumentedServer(
        Action<InstrumentationOptions>? options = default)
        => CreateStarWarsServer(
                configureServices: services =>
                    services
                        .AddGraphQLServer()
                        .AddInstrumentation(options));
}
