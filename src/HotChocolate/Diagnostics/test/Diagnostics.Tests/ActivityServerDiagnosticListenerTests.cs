using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Subscriptions;
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

    [Fact]
    public async Task Http_Subscription_Should_Be_Ok_When_Server_Completes()
    {
        using var guard = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        using (CaptureActivities(out var activities))
        {
            // arrange
            var signal = new HttpSubscriptionSignal();
            using var server = CreateInstrumentedServer(
                o => o.Scopes = ActivityScopes.All,
                b => b
                    .AddTypeExtension<SubscriptionDiagnosticsExtension>()
                    .Services.AddSingleton(signal));
            using var client = GraphQLHttpClient.Create(server.CreateClient());
            var sender = server.Services.GetRequiredService<ITopicEventSender>();

            var request = new OperationRequest("subscription OnMessageSubscription { onMessage }");

            using var result = await client.PostAsync(request, s_url, guard.Token);
            var results = result.ReadAsResultStreamAsync().GetAsyncEnumerator(guard.Token);

            // act
            // wait until the server subscribed to the topic, push one event, then
            // complete the topic so the client observes a clean, graceful close
            try
            {
                var moveNext = results.MoveNextAsync().AsTask();
                await signal.Subscribed.Task.WaitAsync(guard.Token);
                await sender.SendAsync("OnMessage", "hello", guard.Token);
                Assert.True(await moveNext);
                await sender.CompleteAsync("OnMessage");
                Assert.False(await results.MoveNextAsync());
            }
            finally
            {
                await IgnoreCancellationAsync(results.DisposeAsync().AsTask());
            }

            // assert
            activities.MatchSnapshot(Postfix([NET11_0]));
        }
    }

    [Fact]
    public async Task Http_Subscription_Should_Be_Unset_When_Client_Disconnects()
    {
        using var guard = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        using (CaptureActivities(out var activities))
        {
            // arrange
            var signal = new HttpSubscriptionSignal();
            using var server = CreateInstrumentedServer(
                o => o.Scopes = ActivityScopes.All,
                b => b
                    .AddTypeExtension<SubscriptionDiagnosticsExtension>()
                    .Services.AddSingleton(signal));
            using var client = GraphQLHttpClient.Create(server.CreateClient());
            using var requestCts = CancellationTokenSource.CreateLinkedTokenSource(guard.Token);
            var sender = server.Services.GetRequiredService<ITopicEventSender>();

            var request = new OperationRequest("subscription OnMessageSubscription { onMessage }");

            using var result = await client.PostAsync(request, s_url, requestCts.Token);
            var results = result.ReadAsResultStreamAsync().GetAsyncEnumerator(requestCts.Token);

            try
            {
                // receive one event successfully while the connection is alive
                var moveNext = results.MoveNextAsync().AsTask();
                await signal.Subscribed.Task.WaitAsync(guard.Token);
                await sender.SendAsync("OnMessage", "hello", guard.Token);
                Assert.True(await moveNext);

                // act
                // the subscription is now idle, waiting for the next event.
                // drop the connection (close the tab) by aborting the request.
                var next = results.MoveNextAsync().AsTask();
                await requestCts.CancelAsync();

                // tear down must complete promptly; guard against a hang
                var completed = await Task.WhenAny(next, Task.Delay(2000, guard.Token));
                Assert.Same(next, completed);
                await IgnoreCancellationAsync(next);
            }
            finally
            {
                await IgnoreCancellationAsync(results.DisposeAsync().AsTask());
            }

            // assert
            activities.MatchSnapshot(Postfix([NET11_0]));
        }
    }

    [Fact]
    public async Task Http_Subscription_Should_Be_Unset_When_Client_Disconnects_During_Event()
    {
        using var guard = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        using (CaptureActivities(out var activities))
        {
            // arrange
            var signal = new HttpSubscriptionSignal();
            using var server = CreateInstrumentedServer(
                o => o.Scopes = ActivityScopes.All,
                b => b
                    .AddTypeExtension<SubscriptionDiagnosticsExtension>()
                    .Services.AddSingleton(signal));
            using var client = GraphQLHttpClient.Create(server.CreateClient());
            using var requestCts = CancellationTokenSource.CreateLinkedTokenSource(guard.Token);
            var sender = server.Services.GetRequiredService<ITopicEventSender>();

            var request = new OperationRequest(
                "subscription OnBlockingMessageSubscription { onBlockingMessage }");

            using var result = await client.PostAsync(request, s_url, requestCts.Token);
            var results = result.ReadAsResultStreamAsync().GetAsyncEnumerator(requestCts.Token);

            try
            {
                // start processing an event; the resolver blocks until the
                // connection drops, so the event is in flight when we abort
                var next = results.MoveNextAsync().AsTask();
                await signal.Subscribed.Task.WaitAsync(guard.Token);
                await sender.SendAsync("OnBlockingMessage", "hello", guard.Token);

                // wait until execution has actually entered the blocking resolver
                await signal.Entered.Task.WaitAsync(guard.Token);

                // act
                // drop the connection (close the tab) while the event is in flight
                await requestCts.CancelAsync();

                // tear down must complete promptly; guard against a hang
                var completed = await Task.WhenAny(next, Task.Delay(2000, guard.Token));
                Assert.Same(next, completed);
                await IgnoreCancellationAsync(next);
            }
            finally
            {
                await IgnoreCancellationAsync(results.DisposeAsync().AsTask());
            }

            // assert
            activities.MatchSnapshot(Postfix([NET11_0]));
        }
    }

    [Fact]
    public async Task Http_Subscription_Event_Should_Be_Error_When_Timeout()
    {
        using var guard = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        using (CaptureActivities(out var activities))
        {
            // arrange
            // a blocking resolver combined with a tiny per-event timeout forces a
            // server-side event timeout (not a client abort): the request is never
            // aborted by the caller
            var signal = new HttpSubscriptionSignal();
            using var server = CreateInstrumentedServer(
                o => o.Scopes = ActivityScopes.All,
                b => b
                    .AddTypeExtension<SubscriptionDiagnosticsExtension>()
                    .ModifyRequestOptions(o => o.ExecutionTimeout = TimeSpan.FromMilliseconds(200))
                    .Services.AddSingleton(signal));
            using var client = GraphQLHttpClient.Create(server.CreateClient());
            var sender = server.Services.GetRequiredService<ITopicEventSender>();

            var request = new OperationRequest(
                "subscription OnBlockingMessageSubscription { onBlockingMessage }");

            using var result = await client.PostAsync(request, s_url, guard.Token);
            var results = result.ReadAsResultStreamAsync().GetAsyncEnumerator(guard.Token);

            try
            {
                // start processing an event that blocks past the per-event timeout
                var first = results.MoveNextAsync().AsTask();
                await signal.Subscribed.Task.WaitAsync(guard.Token);
                await sender.SendAsync("OnBlockingMessage", "hello", guard.Token);

                // wait until execution actually entered the blocking resolver
                await signal.Entered.Task.WaitAsync(guard.Token);

                // act
                // let the per-event timeout elapse, then complete the topic so the
                // stream ends cleanly once the errored event span is recorded
                var completed = await Task.WhenAny(first, Task.Delay(5000, guard.Token));
                Assert.Same(first, completed);
                await IgnoreCancellationAsync(first);
                await sender.CompleteAsync("OnBlockingMessage");
                await DrainAsync(results, guard.Token);
            }
            finally
            {
                await IgnoreCancellationAsync(results.DisposeAsync().AsTask());
            }

            // assert
            // the snapshot records the subscription event span status for a
            // server-side event timeout
            activities.MatchSnapshot(Postfix([NET11_0]));
        }
    }

    private static async Task DrainAsync(
        IAsyncEnumerator<HotChocolate.Transport.OperationResult> results,
        CancellationToken cancellationToken)
    {
        try
        {
            while (await results.MoveNextAsync().AsTask().WaitAsync(cancellationToken))
            {
            }
        }
        catch (OperationCanceledException)
        {
            // expected: the streamed read was torn down before a clean close
        }
        catch (IOException)
        {
            // expected: tearing down an in-flight SSE read can surface as an I/O failure
        }
    }

    private static async Task IgnoreCancellationAsync(Task task)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
            // expected: the streamed read was aborted by the client
        }
        catch (IOException)
        {
            // expected: aborting an in-flight SSE read can surface as an I/O failure
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

    [ExtendObjectType("Subscription")]
    public class SubscriptionDiagnosticsExtension
    {
        public async ValueTask<ISourceStream<string>> SubscribeToMessages(
            [Service] ITopicEventReceiver receiver,
            [Service] HttpSubscriptionSignal signal,
            CancellationToken cancellationToken)
        {
            // subscribe first, then signal so the test only pushes an event once
            // the server is guaranteed to receive it (no pre-subscription drop)
            var stream = await receiver.SubscribeAsync<string>("OnMessage", cancellationToken);
            signal.Subscribed.TrySetResult();
            return stream;
        }

        [Subscribe(With = nameof(SubscribeToMessages))]
        public string OnMessage([EventMessage] string message) => message;

        public async ValueTask<ISourceStream<string>> SubscribeToBlockingMessages(
            [Service] ITopicEventReceiver receiver,
            [Service] HttpSubscriptionSignal signal,
            CancellationToken cancellationToken)
        {
            var stream = await receiver.SubscribeAsync<string>("OnBlockingMessage", cancellationToken);
            signal.Subscribed.TrySetResult();
            return stream;
        }

        [Subscribe(With = nameof(SubscribeToBlockingMessages))]
        public async Task<string> OnBlockingMessage(
            [EventMessage] string message,
            [Service] HttpSubscriptionSignal signal,
            CancellationToken cancellationToken)
        {
            // signal that the event resolver started, then block until the event is
            // torn down (client disconnect or per-event timeout)
            signal.Entered.TrySetResult();
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return message;
        }
    }

    public sealed class HttpSubscriptionSignal
    {
        public TaskCompletionSource Subscribed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Entered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
