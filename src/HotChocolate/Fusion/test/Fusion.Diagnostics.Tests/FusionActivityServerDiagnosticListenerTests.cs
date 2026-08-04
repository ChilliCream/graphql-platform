using System.Runtime.CompilerServices;
using HotChocolate.Diagnostics;
using HotChocolate.Resolvers;
using HotChocolate.Transport.Http;
using HotChocolate.Types;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;
using static CookieCrumble.TestEnvironment;
using static HotChocolate.Fusion.Diagnostics.ActivityTestHelper;
using OperationRequest = HotChocolate.Transport.OperationRequest;

namespace HotChocolate.Fusion.Diagnostics;

[Collection("Instrumentation")]
public class FusionActivityServerDiagnosticListenerTests : FusionTestBase
{
    private static readonly Uri s_url = new("http://localhost:5000/graphql");

    [Fact]
    public async Task Http_Post_Single_Request_Default()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
                [("a", server)],
                configureGatewayBuilder: b => b.AddInstrumentation());

            using var client = GraphQLHttpClient.Create(gateway.CreateClient());

            var request = new OperationRequest("{ sayHello }");

            // act
            using var result = await client.PostAsync(request, s_url, TestContext.Current.CancellationToken);
            await result.ReadAsResultAsync(TestContext.Current.CancellationToken);

            // assert
            activities.MatchSnapshot(Postfix([NET11_0]));
        }
    }

    [Fact]
    public async Task Http_Post_Single_Request()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
                [("a", server)],
                configureGatewayBuilder: b => b.AddInstrumentation(
                    o => o.Scopes = FusionActivityScopes.All));

            using var client = GraphQLHttpClient.Create(gateway.CreateClient());

            var request = new OperationRequest("{ sayHello }");

            // act
            using var result = await client.PostAsync(request, s_url, TestContext.Current.CancellationToken);
            await result.ReadAsResultAsync(TestContext.Current.CancellationToken);

            // assert
            activities.MatchSnapshot(Postfix([NET11_0]));
        }
    }

    [Fact]
    public async Task Http_Get_Single_Request()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
                [("a", server)],
                configureGatewayBuilder: b => b.AddInstrumentation(
                    o => o.Scopes = FusionActivityScopes.All));

            using var client = GraphQLHttpClient.Create(gateway.CreateClient());

            var request = new OperationRequest("{ sayHello }");

            // act
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
            using var server = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
                [("a", server)],
                configureGatewayBuilder: b => b.AddInstrumentation(
                    o => o.Scopes = FusionActivityScopes.All));

            using var client = GraphQLHttpClient.Create(gateway.CreateClient());

            var request = new OperationRequest(
                query: """
                    query ($name: String!) {
                        greeting(name: $name)
                    }
                    """,
                variables: new Dictionary<string, object?> { { "name", "World" } });

            // act
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
            using var server = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
                [("a", server)],
                configureGatewayBuilder: b => b.AddInstrumentation(
                    o =>
                    {
                        o.Scopes = FusionActivityScopes.All;
                        o.RequestDetails = RequestDetails.Default | RequestDetails.Variables;
                    }));

            using var client = GraphQLHttpClient.Create(gateway.CreateClient());

            var request = new OperationRequest(
                query: """
                    query ($name: String!) {
                        greeting(name: $name)
                    }
                    """,
                variables: new Dictionary<string, object?> { { "name", "World" } });

            // act
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
            using var server = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
                [("a", server)],
                configureGatewayBuilder: b => b.AddInstrumentation(
                    o => o.Scopes = FusionActivityScopes.All));

            using var client = GraphQLHttpClient.Create(gateway.CreateClient());

            var request = new OperationRequest(
                query: """
                    query ($name: String!) {
                        greeting(name: $name)
                    }
                    """,
                variables: new Dictionary<string, object?> { { "name", "World" } },
                extensions: new Dictionary<string, object?> { { "test", "abc" } });

            // act
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
            using var server = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
                [("a", server)],
                configureGatewayBuilder: b => b.AddInstrumentation(
                    o => o.Scopes = FusionActivityScopes.All));

            var httpClient = gateway.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:5000/graphql?sdl");

            // act
            var response = await httpClient.SendAsync(request, TestContext.Current.CancellationToken);

            // assert
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            activities.MatchSnapshot(Postfix([NET11_0]));
        }
    }

    [Fact]
    public async Task Http_Post_Parser_Error()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
                [("a", server)],
                configureGatewayBuilder: b => b.AddInstrumentation(
                    o => o.Scopes = FusionActivityScopes.All));

            using var client = GraphQLHttpClient.Create(gateway.CreateClient());

            // lang=text
            var request = new OperationRequest(
                """
                {
                    deep {
                        deeper {
                            1deeps {
                                name
                            }
                        }
                    }
                }
                """);

            // act
            using var result = await client.PostAsync(request, s_url, TestContext.Current.CancellationToken);
            await result.ReadAsResultAsync(TestContext.Current.CancellationToken);

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
            using var server = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
                [("a", server)],
                configureGatewayBuilder: b => b.AddInstrumentation(
                    o =>
                    {
                        o.Scopes = FusionActivityScopes.All;
                        o.RequestDetails = RequestDetails.None;
                    }));

            using var client = GraphQLHttpClient.Create(gateway.CreateClient());

            var request = new OperationRequest(
                query: """
                    query GetGreeting($name: String!) {
                        greeting(name: $name)
                    }
                    """,
                variables: new Dictionary<string, object?> { { "name", "World" } },
                extensions: new Dictionary<string, object?> { { "test", "abc" } });

            // act
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
            using var server = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
                [("a", server)],
                configureGatewayBuilder: b => b.AddInstrumentation(
                    o =>
                    {
                        o.Scopes = FusionActivityScopes.All;
                        o.RequestDetails = RequestDetails.All;
                    }));

            using var client = GraphQLHttpClient.Create(gateway.CreateClient());

            var request = new OperationRequest(
                query: """
                    query GetGreeting($name: String!) {
                        greeting(name: $name)
                    }
                    """,
                variables: new Dictionary<string, object?> { { "name", "World" } },
                extensions: new Dictionary<string, object?> { { "test", "abc" } });

            // act
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
            using var server = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
                [("a", server)],
                configureGatewayBuilder: b => b.AddInstrumentation(
                    o =>
                    {
                        o.Scopes = FusionActivityScopes.All;
                        o.RequestDetails = RequestDetails.Document;
                    }));

            using var client = GraphQLHttpClient.Create(gateway.CreateClient());

            var request = new OperationRequest("{ sayHello }");

            // act
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
            using var server = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
                [("a", server)],
                configureGatewayBuilder: b => b.AddInstrumentation(
                    o => o.Scopes = FusionActivityScopes.All));

            using var client = GraphQLHttpClient.Create(gateway.CreateClient());

            var request = new OperationRequest(
                query: """
                    query GetGreeting {
                        sayHello
                    }
                    """,
                extensions: new Dictionary<string, object?> { { "test", "abc" } });

            // act
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
            var signal = new BlockingSignal();
            using var server = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>(),
                configureServices: s => s.AddSingleton(signal));

            using var gateway = await CreateCompositeSchemaAsync(
                [("a", server)],
                configureGatewayBuilder: b => b.AddInstrumentation(
                    o => o.Scopes = FusionActivityScopes.All));

            using var client = GraphQLHttpClient.Create(gateway.CreateClient());
            using var requestCts = CancellationTokenSource.CreateLinkedTokenSource(guard.Token);

            var request = new OperationRequest("{ blockUntilCancelled }");

            // act
            // start the request, wait until the source resolver is actually executing, then
            // drop the connection by cancelling the client token (a dropped browser tab)
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
            using var server = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
                [("a", server)],
                configureGatewayBuilder: b => b
                    .AddInstrumentation(o => o.Scopes = FusionActivityScopes.All)
                    .ModifyRequestOptions(o => o.ExecutionTimeout = TimeSpan.FromMilliseconds(200)));

            using var client = GraphQLHttpClient.Create(gateway.CreateClient());

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
            using var server1 = CreateSourceSchema(
                "a",
                b => b
                    .AddQueryType<Query>()
                    .AddSubscriptionType<Subscription>());

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1)
            ],
            configureGatewayBuilder: b => b.AddInstrumentation(o =>
                o.Scopes = FusionActivityScopes.All));

            using var client = GraphQLHttpClient.Create(gateway.CreateClient());

            var request = new OperationRequest("subscription OnMessageSubscription { onMessage }");

            using var result = await client.PostAsync(request, s_url, guard.Token);
            var results = result.ReadAsResultStreamAsync().GetAsyncEnumerator(guard.Token);

            // act
            // the subgraph emits one event then completes its stream, so receive the
            // single event and then drain the stream so the gateway closes the SSE
            // response gracefully (no exception, no abort)
            try
            {
                Assert.True(await results.MoveNextAsync());
                Assert.False(await results.MoveNextAsync());
            }
            finally
            {
                await IgnoreCancellationAsync(results.DisposeAsync().AsTask());
            }

            // assert
            // the snapshot records the gateway request and subscription event span
            // status for a graceful close
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
            using var server1 = CreateSourceSchema(
                "a",
                b => b
                    .AddQueryType<Query>()
                    .AddSubscriptionType<Subscription>());

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1)
            ],
            configureGatewayBuilder: b => b.AddInstrumentation(o =>
                o.Scopes = FusionActivityScopes.All));

            using var client = GraphQLHttpClient.Create(gateway.CreateClient());
            using var requestCts = CancellationTokenSource.CreateLinkedTokenSource(guard.Token);

            var request = new OperationRequest("subscription OnIdleMessageSubscription { onIdleMessage }");

            using var result = await client.PostAsync(request, s_url, requestCts.Token);
            var results = result.ReadAsResultStreamAsync().GetAsyncEnumerator(requestCts.Token);

            try
            {
                // receive one event successfully while the connection is alive
                Assert.True(await results.MoveNextAsync());

                // act
                // the subscription is now idle, waiting for the next event.
                // drop the connection (close the tab) by cancelling the client token,
                // which fires the gateway's HttpContext.RequestAborted.
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
            // the snapshot records the subscription event span status for a client
            // abort while the subscription is idle
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
            // a cross-schema subscription: source "a" emits a book event whose author
            // name is fetched from source "b". The downstream lookup blocks, so the
            // event is genuinely in flight when the connection drops.
            var signal = new BlockingSubscriptionSignal();

            using var server1 = CreateSourceSchema(
                "a",
                b => b
                    .AddQueryType<BookQuery>()
                    .AddSubscriptionType<BookSubscription>());

            using var server2 = CreateSourceSchema(
                "b",
                b => b.AddQueryType<AuthorQuery>(),
                configureServices: s => s.AddSingleton(signal));

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1),
                ("b", server2)
            ],
            configureGatewayBuilder: b => b.AddInstrumentation(o =>
                o.Scopes = FusionActivityScopes.All));

            using var client = GraphQLHttpClient.Create(gateway.CreateClient());
            using var requestCts = CancellationTokenSource.CreateLinkedTokenSource(guard.Token);

            var request = new OperationRequest(
                "subscription OnBookCreatedSubscription { onBookCreated { author { name } } }");

            using var result = await client.PostAsync(request, s_url, requestCts.Token);
            var results = result.ReadAsResultStreamAsync().GetAsyncEnumerator(requestCts.Token);

            try
            {
                // start processing the event; the downstream author lookup blocks, so
                // the event is in flight when we abort
                var next = results.MoveNextAsync().AsTask();

                // wait until the downstream lookup actually started
                await signal.Entered.Task.WaitAsync(guard.Token);

                // act
                // drop the connection (close the tab) while the event is in flight by
                // cancelling the client token, which fires HttpContext.RequestAborted.
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
            // the snapshot records the subscription event span status for a client
            // abort while an event is in flight
            activities.MatchSnapshot(Postfix([NET11_0]));
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

    public class Query
    {
        public string SayHello() => "hello";

        public string Greeting(string name) => $"Hello, {name}!";

        public string CauseFatalError(IResolverContext context)
            => throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("fail")
                    .SetCode("CUSTOM_ERROR_CODE")
                    .SetPath(context.Path)
                    .Build());

        public Deep Deep() => new();

        public async Task<string> BlockUntilCancelled(
            [Service] BlockingSignal signal,
            CancellationToken cancellationToken)
        {
            // signal that execution has actually reached the resolver, then block until
            // the connection drops (the request abort token fires)
            signal.Entered.TrySetResult();
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return "unreachable";
        }

        public async Task<string> BlockUntilTimeout(CancellationToken cancellationToken)
        {
            // block until the execution timeout cancels the (combined) request token,
            // producing an HC0045 (timeout) result
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return "unreachable";
        }
    }

    public sealed class BlockingSignal
    {
        public TaskCompletionSource Entered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public class Deep
    {
        public string Name => "deep";

        public Deeper Deeper() => new();
    }

    public class Deeper
    {
        public string Name => "deeper";

        public Deep[] Deeps() => [new Deep()];
    }

    public class Subscription
    {
        public async IAsyncEnumerable<string> OnMessageStream()
        {
            yield return "hello";
            await Task.CompletedTask;
        }

        [Subscribe(With = nameof(OnMessageStream))]
        public string OnMessage([EventMessage] string message) => message;

        public async IAsyncEnumerable<string> OnIdleMessageStream(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            // deliver one event, then stay open and idle until the connection drops
            yield return "hello";
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        [Subscribe(With = nameof(OnIdleMessageStream))]
        public string OnIdleMessage([EventMessage] string message) => message;
    }

    public sealed class BlockingSubscriptionSignal
    {
        public TaskCompletionSource Entered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    [GraphQLName("Query")]
    public class BookQuery
    {
        public string SayHello() => "hello";
    }

    [GraphQLName("Subscription")]
    public class BookSubscription
    {
        public async IAsyncEnumerable<Book> OnBookCreatedStream()
        {
            yield return new Book(1, new Author(1));
            await Task.CompletedTask;
        }

        [Subscribe(With = nameof(OnBookCreatedStream))]
        public Book OnBookCreated([EventMessage] Book book) => book;
    }

    public record Book([property: ID] int Id, [property: Shareable] Author Author);

    [EntityKey("id")]
    public record Author([property: ID] int Id);

    [GraphQLName("Query")]
    public class AuthorQuery
    {
        [Internal]
        public AuthorLookups Lookups { get; } = new();
    }

    [Internal]
    public class AuthorLookups
    {
        [Lookup]
        public async Task<AuthorWithName?> GetAuthorById(
            [ID] int id,
            [Service] BlockingSubscriptionSignal signal,
            CancellationToken cancellationToken)
        {
            // block while the event is in flight so the connection can be dropped
            // before the author name is delivered
            signal.Entered.TrySetResult();
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return new AuthorWithName(id, "JRR Tolkien");
        }
    }

    [GraphQLName("Author")]
    public record AuthorWithName([property: ID] int Id, string Name);
}
