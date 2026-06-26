using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Transport.Http;
using HotChocolate.Types;
using static CookieCrumble.TestEnvironment;
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
            using var result = await client.PostAsync(request, s_url, TestContext.Current.CancellationToken);
            await result.ReadAsResultAsync(TestContext.Current.CancellationToken);

            // assert
            activities.MatchSnapshot(Postfix([NET11_0]));
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
            using var result = await client.PostAsync(request, s_url, TestContext.Current.CancellationToken);
            await result.ReadAsResultAsync(TestContext.Current.CancellationToken);

            // assert
            activities.MatchSnapshot(Postfix([NET11_0]));
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
            using var result = await client.GetAsync(request, s_url, TestContext.Current.CancellationToken);
            await result.ReadAsResultAsync(TestContext.Current.CancellationToken);

            // assert
            activities.MatchSnapshot(Postfix([NET11_0]));
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
            using var result = await client.PostAsync(request, s_url, TestContext.Current.CancellationToken);
            await result.ReadAsResultAsync(TestContext.Current.CancellationToken);

            // assert
            activities.MatchSnapshot(Postfix([NET11_0]));
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
            using var result = await client.PostAsync(request, s_url, TestContext.Current.CancellationToken);
            await result.ReadAsResultAsync(TestContext.Current.CancellationToken);

            // assert
            activities.MatchSnapshot(Postfix([NET11_0]));
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
            using var result = await client.PostAsync(request, s_url, TestContext.Current.CancellationToken);
            await result.ReadAsResultAsync(TestContext.Current.CancellationToken);

            // assert
            activities.MatchSnapshot(Postfix([NET11_0]));
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
            var response = await server.CreateClient().SendAsync(request, TestContext.Current.CancellationToken);

            // assert
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            // assert
            activities.MatchSnapshot(Postfix([NET11_0]));
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
            activities.MatchSnapshot(Postfix([NET11_0]));
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
            activities.MatchSnapshot(Postfix([NET11_0]));
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
            activities.MatchSnapshot(Postfix([NET11_0]));
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
            using var result = await client.PostAsync(request, s_url, TestContext.Current.CancellationToken);
            await result.ReadAsResultAsync(TestContext.Current.CancellationToken);

            // assert
            activities.MatchSnapshot(Postfix([NET11_0]));
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
            using var result = await client.PostAsync(request, s_url, TestContext.Current.CancellationToken);
            await result.ReadAsResultAsync(TestContext.Current.CancellationToken);

            // assert
            activities.MatchSnapshot(Postfix([NET11_0]));
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
            using var result = await client.PostAsync(request, s_url, TestContext.Current.CancellationToken);
            await result.ReadAsResultAsync(TestContext.Current.CancellationToken);

            // assert
            activities.MatchSnapshot(Postfix([NET11_0]));
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
            using var result = await client.PostAsync(request, s_url, TestContext.Current.CancellationToken);
            await result.ReadAsResultAsync(TestContext.Current.CancellationToken);

            // assert
            activities.MatchSnapshot(Postfix([NET11_0]));
        }
    }

    [Fact]
    public async Task Http_Request_Should_Be_Unset_When_Client_Disconnects()
    {
        using var guard = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        using (CaptureActivities(out var activities))
        {
            // arrange
            var signal = new HttpCancellationSignal();
            using var server = CreateInstrumentedServer(
                o => o.Scopes = ActivityScopes.All,
                b => b
                    .AddTypeExtension<CancellationQueryExtension>()
                    .Services.AddSingleton(signal));
            using var client = GraphQLHttpClient.Create(server.CreateClient());
            using var requestCts = CancellationTokenSource.CreateLinkedTokenSource(guard.Token);

            var request = new OperationRequest("{ blockUntilCancelled }");

            // act
            // start the request, wait until the resolver is actually executing, then drop
            // the connection by cancelling the client token (a dropped browser tab)
            var postTask = PostAndIgnoreCancellationAsync(client, request, requestCts.Token);
            await signal.Entered.Task.WaitAsync(guard.Token);
            await requestCts.CancelAsync();
            await postTask;

            // assert
            // the snapshot records every span status for a client disconnect mid execution
            activities.MatchSnapshot(Postfix([NET11_0]));
        }
    }

    [Fact]
    public async Task Http_Request_Should_Be_Error_When_Timeout()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            // a tiny execution timeout combined with a resolver that blocks until the
            // request token fires forces a server-side execution timeout (HC0045)
            using var server = CreateInstrumentedServer(
                o => o.Scopes = ActivityScopes.All,
                b => b
                    .AddTypeExtension<CancellationQueryExtension>()
                    .ModifyRequestOptions(o => o.ExecutionTimeout = TimeSpan.FromMilliseconds(200)));
            using var client = GraphQLHttpClient.Create(server.CreateClient());

            var request = new OperationRequest("{ blockUntilTimeout }");

            // act
            using var result = await client.PostAsync(request, s_url, TestContext.Current.CancellationToken);
            using var operationResult = await result.ReadAsResultAsync(TestContext.Current.CancellationToken);

            // assert
            // the timeout actually triggered (scenario guard); the snapshot records the
            // resulting span statuses
            var code = operationResult.Errors[0].GetProperty("extensions").GetProperty("code").GetString();
            Assert.Equal(ErrorCodes.Execution.Timeout, code);

            activities.MatchSnapshot(Postfix([NET11_0]));
        }
    }

    private static async Task PostAndIgnoreCancellationAsync(
        GraphQLHttpClient client,
        OperationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var result = await client.PostAsync(request, s_url, cancellationToken);
            await result.ReadAsResultAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // expected: the caller aborted the in-flight request
        }
        catch (InvalidOperationException)
        {
            // expected: the aborted request yields an empty response that cannot be
            // read as a GraphQL result
        }
    }

    private TestServer CreateInstrumentedServer(
        Action<InstrumentationOptions>? options = null,
        Action<IRequestExecutorBuilder>? configureBuilder = null)
        => CreateStarWarsServer(
                configureServices: services =>
                {
                    var builder = services
                        .AddGraphQLServer()
                        .AddInstrumentation(options)
                        .ModifyPagingOptions(o => o.RequirePagingBoundaries = false)
                        .ModifyOptions(
                            o =>
                            {
                                o.EnableDefer = true;
                                o.EnableStream = true;
                            });

                    configureBuilder?.Invoke(builder);
                });

    [ExtendObjectType("Query")]
    public class CancellationQueryExtension
    {
        public async Task<string> BlockUntilCancelled(
            IResolverContext context,
            [Service] HttpCancellationSignal signal)
        {
            // signal that execution has actually reached the resolver, then block
            // until the connection drops (the request abort token fires)
            signal.Entered.TrySetResult();
            await Task.Delay(Timeout.Infinite, context.RequestAborted);
            return "unreachable";
        }

        public async Task<string> BlockUntilTimeout(IResolverContext context)
        {
            // block until the execution timeout cancels the (combined) request token,
            // producing an HC0045 (timeout) result
            await Task.Delay(Timeout.Infinite, context.RequestAborted);
            return "unreachable";
        }
    }

    public sealed class HttpCancellationSignal
    {
        public TaskCompletionSource Entered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
