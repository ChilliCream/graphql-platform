using System.Text.Json;
using HotChocolate.Diagnostics;
using HotChocolate.Resolvers;
using HotChocolate.Transport.Http;
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
    public async Task Request_Should_Be_Unset_When_Client_Disconnects()
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
    public async Task Request_Should_Be_Error_When_Timeout()
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
}
