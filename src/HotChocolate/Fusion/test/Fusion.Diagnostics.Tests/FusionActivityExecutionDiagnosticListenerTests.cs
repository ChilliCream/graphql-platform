using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.PersistedOperations;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Fusion.Diagnostics.ActivityTestHelper;

namespace HotChocolate.Fusion.Diagnostics;

[Collection("Instrumentation")]
public class FusionActivityExecutionDiagnosticListenerTests : FusionTestBase
{
    [Fact]
    public async Task Track_Events_Of_A_Simple_Query_Default()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server1 = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1)
            ],
            configureGatewayBuilder: b => b.AddInstrumentation());

            // act
            var executor = await gateway.Services.GetRequestExecutorAsync();

            var request = OperationRequestBuilder.New()
                .SetDocument("{ sayHello }")
                .Build();

            // act
            await executor.ExecuteAsync(request);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Allow_Document_To_Be_Captured()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server1 = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1)
            ],
            configureGatewayBuilder: b => b.AddInstrumentation(o =>
            {
                o.Scopes = FusionActivityScopes.All;
                o.IncludeDocument = true;
            }));

            var executor = await gateway.Services.GetRequestExecutorAsync();

            var request = OperationRequestBuilder.New()
                .SetDocument("query SayHelloOperation { sayHello }")
                .Build();

            // act
            await executor.ExecuteAsync(request);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Ensure_That_The_Validation_Activity_Has_An_Error_Status()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server1 = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1)
            ],
            configureGatewayBuilder: b => b.AddInstrumentation(o =>
            {
                o.Scopes = FusionActivityScopes.All;
                o.IncludeDocument = true;
            }));

            var executor = await gateway.Services.GetRequestExecutorAsync();

            var request = OperationRequestBuilder.New()
                .SetDocument("query SayHelloOperation { sayHello_ }")
                .Build();

            // act
            await executor.ExecuteAsync(request);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Cause_A_Resolver_Error_That_Deletes_The_Whole_Result()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server1 = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1)
            ],
            configureGatewayBuilder: b => b.AddInstrumentation(o =>
            {
                o.Scopes = FusionActivityScopes.All;
                o.IncludeDocument = true;
            }));

            var executor = await gateway.Services.GetRequestExecutorAsync();

            var request = OperationRequestBuilder.New()
                .SetDocument("query SayHelloOperation { causeFatalError }")
                .Build();

            // act
            await executor.ExecuteAsync(request);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Source_Schema_Transport_Error()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server1 = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>(),
                isOffline: true);

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1)
            ],
            configureGatewayBuilder: b => b.AddInstrumentation(o =>
            {
                o.Scopes = FusionActivityScopes.All;
                o.IncludeDocument = true;
            }));

            var executor = await gateway.Services.GetRequestExecutorAsync();

            var request = OperationRequestBuilder.New()
                .SetDocument("{ sayHello }")
                .Build();

            // act
            await executor.ExecuteAsync(request);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Track_Events_Of_A_Query_With_Multiple_Sources()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server1 = CreateSourceSchema(
                "a",
                b => b.AddQueryType<QueryA>());

            using var server2 = CreateSourceSchema(
                "b",
                b => b.AddQueryType<QueryB>());

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1),
                ("b", server2)
            ],
            configureGatewayBuilder: b => b.AddInstrumentation(o =>
                o.Scopes = FusionActivityScopes.All));

            var executor = await gateway.Services.GetRequestExecutorAsync();

            var request = OperationRequestBuilder.New()
                .SetDocument("{ sayHello sayGoodbye }")
                .Build();

            // act
            await executor.ExecuteAsync(request);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task PersistedOperation_LoadsFromStorage_DefaultScopes()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            var storage = new InMemoryOperationDocumentStorage();
            storage.Add("sayHelloOp", "{ sayHello }");

            using var server1 = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1)
            ],
            configureGatewayBuilder: b => b
                .AddInstrumentation()
                .ConfigureSchemaServices(
                    (_, s) => s.AddSingleton<IOperationDocumentStorage>(storage))
                .UsePersistedOperationPipeline());

            var executor = await gateway.Services.GetRequestExecutorAsync();

            // act
            await executor.ExecuteAsync(OperationRequest.FromId("sayHelloOp"));

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task DocumentNotFoundInStorage_RecordsEvent()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server1 = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1)
            ],
            configureGatewayBuilder: b => b
                .AddInstrumentation(o => o.Scopes = FusionActivityScopes.All)
                .ConfigureSchemaServices(
                    (_, s) => s.AddSingleton<IOperationDocumentStorage>(new NoopOperationDocumentStorage()))
                .UsePersistedOperationPipeline());

            var executor = await gateway.Services.GetRequestExecutorAsync();

            var request = OperationRequestBuilder.New()
                .SetDocumentId("a8c5e2f1d3b4a6e7c9d0f1a2b3c4d5e6")
                .Build();

            // act
            await executor.ExecuteAsync(request);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task UntrustedDocumentRejected_RecordsEvent()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server1 = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1)
            ],
            configureGatewayBuilder: b => b
                .AddInstrumentation(o => o.Scopes = FusionActivityScopes.All)
                .ModifyRequestOptions(o => o.PersistedOperations.OnlyAllowPersistedDocuments = true)
                .ConfigureSchemaServices(
                    (_, s) => s.AddSingleton<IOperationDocumentStorage>(new NoopOperationDocumentStorage()))
                .UsePersistedOperationPipeline());

            var executor = await gateway.Services.GetRequestExecutorAsync();

            var request = OperationRequestBuilder.New()
                .SetDocument("{ sayHello }")
                .Build();

            // act
            await executor.ExecuteAsync(request);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task ParsingError_InvalidGraphQLDocument_ReportsErrorStatus()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server1 = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1)
            ],
            configureGatewayBuilder: b => b.AddInstrumentation(o =>
                o.Scopes = FusionActivityScopes.All));

            var executor = await gateway.Services.GetRequestExecutorAsync();

            var request = OperationRequestBuilder.New()
                .SetDocument("{ sayHello")
                .Build();

            // act
            await executor.ExecuteAsync(request);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task ValidationError_UnknownField_ReportsErrorStatus()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server1 = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1)
            ],
            configureGatewayBuilder: b => b.AddInstrumentation(o =>
                o.Scopes = FusionActivityScopes.All));

            var executor = await gateway.Services.GetRequestExecutorAsync();

            var request = OperationRequestBuilder.New()
                .SetDocument("{ unknownField123 }")
                .Build();

            // act
            await executor.ExecuteAsync(request);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task DefaultScopes_ExcludesExecuteRequestAndParseDocumentSpans()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server1 = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1)
            ],
            configureGatewayBuilder: b => b.AddInstrumentation());

            var executor = await gateway.Services.GetRequestExecutorAsync();

            var request = OperationRequestBuilder.New()
                .SetDocument("{ sayHello }")
                .Build();

            // act
            await executor.ExecuteAsync(request);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task AllScopes_IncludesAllSpans()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server1 = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1)
            ],
            configureGatewayBuilder: b => b.AddInstrumentation(o =>
                o.Scopes = FusionActivityScopes.All));

            var executor = await gateway.Services.GetRequestExecutorAsync();

            var request = OperationRequestBuilder.New()
                .SetDocument("{ sayHello }")
                .Build();

            // act
            await executor.ExecuteAsync(request);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task CustomScopes_OnlyValidateAndPlan_LimitsSpans()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server1 = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1)
            ],
            configureGatewayBuilder: b => b.AddInstrumentation(o =>
                o.Scopes = FusionActivityScopes.ValidateDocument
                    | FusionActivityScopes.PlanOperation));

            var executor = await gateway.Services.GetRequestExecutorAsync();

            var request = OperationRequestBuilder.New()
                .SetDocument("{ sayHello }")
                .Build();

            // act
            await executor.ExecuteAsync(request);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact(Skip = "This is flaky")]
    public async Task MultipleSources_HttpRequestError_MarksNodeSpanAsError()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server1 = CreateSourceSchema(
                "a",
                b => b.AddQueryType<QueryA>());

            using var server2 = CreateSourceSchema(
                "b",
                b => b.AddQueryType<QueryB>(),
                isOffline: true);

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1),
                ("b", server2)
            ],
            configureGatewayBuilder: b => b.AddInstrumentation(o =>
            {
                o.Scopes = FusionActivityScopes.All;
                o.IncludeDocument = true;
            }));

            var executor = await gateway.Services.GetRequestExecutorAsync();

            var request = OperationRequestBuilder.New()
                .SetDocument("{ sayHello sayGoodbye }")
                .Build();

            // act
            await executor.ExecuteAsync(request);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task MultipleSources_SourceSchemaResolverError_RecordsDeeplyNestedError()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server1 = CreateSourceSchema(
                "a",
                b => b.AddQueryType<QueryA>());

            using var server2 = CreateSourceSchema(
                "b",
                b => b.AddQueryType<QueryBWithDeepError>());

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1),
                ("b", server2)
            ],
            configureGatewayBuilder: b => b.AddInstrumentation(o =>
            {
                o.Scopes = FusionActivityScopes.All;
                o.IncludeDocument = true;
            }));

            var executor = await gateway.Services.GetRequestExecutorAsync();

            var request = OperationRequestBuilder.New()
                .SetDocument(
                    """
                    {
                        sayHello
                        deepB {
                            deeperB {
                                causeFatalError
                            }
                        }
                    }
                    """)
                .Build();

            // act
            await executor.ExecuteAsync(request);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task DocumentCache_SecondExecution_RecordsCacheHitEvent()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<Query>());

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1)
            ],
            configureGatewayBuilder: b => b.AddInstrumentation(o =>
                o.Scopes = FusionActivityScopes.All));

        var executor = await gateway.Services.GetRequestExecutorAsync();

        // act - execute twice so second uses cached document
        var request = OperationRequestBuilder.New()
            .SetDocument("{ sayHello }")
            .SetDocumentHash(new OperationDocumentHash("abc", "sha256", HashFormat.Hex))
            .Build();

        await executor.ExecuteAsync(request);

        using (CaptureActivities(out var activities))
        {
            await executor.ExecuteAsync(request);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task SubscriptionEvent_Records_Subscription_Event_Span()
    {
        using var cts = new CancellationTokenSource(5000);

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

            var executor = await gateway.Services.GetRequestExecutorAsync();

            // act
            await using var result = await executor.ExecuteAsync(
                "subscription OnMessageSubscription { onMessage }");
            await using var responseStream = result.ExpectResponseStream();
            var results = responseStream.ReadResultsAsync().GetAsyncEnumerator(cts.Token);

            try
            {
                Assert.True(await results.MoveNextAsync());
            }
            finally
            {
                await results.DisposeAsync();
            }

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact(Skip = "Errors are not correctly triggered")]
    public async Task SubscriptionEventError_Records_Subscription_Event_Error()
    {
        using var cts = new CancellationTokenSource(5000);

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

            var executor = await gateway.Services.GetRequestExecutorAsync();

            // act
            await using var result = await executor.ExecuteAsync(
                "subscription OnFailingMessageSubscription { onFailingMessage }");
            await using var responseStream = result.ExpectResponseStream();
            var results = responseStream.ReadResultsAsync().GetAsyncEnumerator(cts.Token);

            try
            {
                Assert.True(await results.MoveNextAsync());
            }
            finally
            {
                await results.DisposeAsync();
            }

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact(Skip = "Errors are not correctly triggered")]
    public async Task SubscriptionRequestFails_When_SourceSchema_Is_Offline()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server1 = CreateSourceSchema(
                "a",
                b => b
                    .AddQueryType<Query>()
                    .AddSubscriptionType<Subscription>(),
                isOffline: true);

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1)
            ],
            configureGatewayBuilder: b => b.AddInstrumentation(o =>
                o.Scopes = FusionActivityScopes.All));

            var executor = await gateway.Services.GetRequestExecutorAsync();

            // act
            IExecutionResult? result = null;

            try
            {
                result = await executor.ExecuteAsync(
                    "subscription OnMessageSubscription { onMessage }");
            }
            catch
            {
                // expected for failed subscription handshake.
            }
            finally
            {
                if (result is not null)
                {
                    await result.DisposeAsync();
                }
            }

            // assert
            activities.MatchSnapshot();
        }
    }

    public class Query
    {
        public string SayHello() => "hello";

        public string CauseFatalError()
            => throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("fail")
                    .SetCode("CUSTOM_ERROR_CODE")
                    .Build());

        public Deep Deep() => new();
    }

    [GraphQLName("Query")]
    public class QueryA
    {
        public string SayHello() => "hello";
    }

    [GraphQLName("Query")]
    public class QueryB
    {
        public string SayGoodbye() => "goodbye";
    }

    [GraphQLName("Query")]
    public class QueryBWithDeepError
    {
        public string SayGoodbye() => "goodbye";

        public DeepB DeepB() => new();
    }

    public class DeepB
    {
        public DeeperB DeeperB() => new();
    }

    public class DeeperB
    {
        public string CauseFatalError()
            => throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("deep fail")
                    .SetCode("CUSTOM_ERROR_CODE")
                    .Build());
    }

    public class Deep
    {
        public Deeper Deeper() => new();

        public string CauseFatalError()
            => throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("fail")
                    .SetCode("CUSTOM_ERROR_CODE")
                    .Build());
    }

    public class Deeper
    {
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

        public async IAsyncEnumerable<string> OnFailingMessageStream()
        {
            yield return "hello";
            await Task.CompletedTask;
        }

        [Subscribe(With = nameof(OnFailingMessageStream))]
        public string OnFailingMessage([EventMessage] string message)
            => throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Subscription event failed.")
                    .SetCode("CUSTOM_ERROR_CODE")
                    .Build());
    }

    private sealed class NoopOperationDocumentStorage : IOperationDocumentStorage
    {
        public ValueTask<IOperationDocument?> TryReadAsync(
            OperationDocumentId documentId,
            CancellationToken cancellationToken = default)
            => new(default(IOperationDocument));

        public ValueTask SaveAsync(
            OperationDocumentId documentId,
            IOperationDocument document,
            CancellationToken cancellationToken = default)
            => default;
    }

    private sealed class InMemoryOperationDocumentStorage : IOperationDocumentStorage
    {
        private readonly Dictionary<string, DocumentNode> _cache = [];

        public void Add(string id, string document)
            => _cache[id] = Utf8GraphQLParser.Parse(document);

        public ValueTask<IOperationDocument?> TryReadAsync(
            OperationDocumentId documentId,
            CancellationToken cancellationToken = default)
        {
            if (_cache.TryGetValue(documentId.Value, out var document))
            {
                return new ValueTask<IOperationDocument?>(new OperationDocument(document));
            }

            return new ValueTask<IOperationDocument?>(default(IOperationDocument));
        }

        public ValueTask SaveAsync(
            OperationDocumentId documentId,
            IOperationDocument document,
            CancellationToken cancellationToken = default)
        {
            _cache[documentId.Value] = Utf8GraphQLParser.Parse(document.AsSpan());
            return default;
        }
    }
}
