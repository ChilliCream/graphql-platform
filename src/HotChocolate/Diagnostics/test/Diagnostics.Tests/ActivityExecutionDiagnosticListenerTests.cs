using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.PersistedOperations;
using HotChocolate.Resolvers;
using HotChocolate.Subscriptions;
using HotChocolate.Types;
using static CookieCrumble.TestEnvironment;
using static HotChocolate.Diagnostics.ActivityTestHelper;

namespace HotChocolate.Diagnostics;

[Collection("Instrumentation")]
public partial class ActivityExecutionDiagnosticListenerTests
{
    [Fact]
    public async Task Track_Events_Of_A_Simple_Query_Default()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation()
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("{ sayHello }");

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Allow_Document_To_Be_Captured()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o =>
                {
                    o.Scopes = ActivityScopes.All;
                    o.IncludeDocument = true;
                })
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("query SayHelloOperation { sayHello }");

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Ensure_That_The_Validation_Activity_Has_An_Error_Status()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o =>
                {
                    o.Scopes = ActivityScopes.All;
                    o.IncludeDocument = true;
                })
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("query SayHelloOperation { sayHello_ }");

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Cause_A_Resolver_Error_That_Deletes_The_Whole_Result()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o =>
                {
                    o.Scopes = ActivityScopes.All;
                    o.IncludeDocument = true;
                })
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("query SayHelloOperation { causeFatalError }");

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
            storage.Add("say-hello-persisted-id", "{ sayHello }");

            var services = new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation()
                .AddQueryType<SimpleQuery>()
                .UsePersistedOperationPipeline()
                .ConfigureSchemaServices(
                    s => s.AddSingleton<IOperationDocumentStorage>(storage))
                .Services
                .BuildServiceProvider();

            var executor = await services.GetRequestExecutorAsync();

            // act
            await executor.ExecuteAsync(OperationRequest.FromId("say-hello-persisted-id"));

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
            var services = new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o => o.Scopes = ActivityScopes.All)
                .AddQueryType<SimpleQuery>()
                .UsePersistedOperationPipeline()
                .ConfigureSchemaServices(
                    s => s.AddSingleton<IOperationDocumentStorage>(new NoopOperationDocumentStorage()))
                .Services
                .BuildServiceProvider();

            var executor = await services.GetRequestExecutorAsync();

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
            var services = new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o => o.Scopes = ActivityScopes.All)
                .AddQueryType<SimpleQuery>()
                .ModifyRequestOptions(o => o.PersistedOperations.OnlyAllowPersistedDocuments = true)
                .UsePersistedOperationPipeline()
                .ConfigureSchemaServices(
                    s => s.AddSingleton<IOperationDocumentStorage>(new NoopOperationDocumentStorage()))
                .Services
                .BuildServiceProvider();

            var executor = await services.GetRequestExecutorAsync();

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
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o => o.Scopes = ActivityScopes.All)
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("{ sayHello");

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task ValidationError_UnknownField_ReportsErrorStatus()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o => o.Scopes = ActivityScopes.All)
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("{ unknownField123 }");

            // assert
            activities.MatchSnapshot();
        }
    }

    // TODO: Not sure if we want this
    [Fact]
    public async Task DefaultScopes_ExcludesExecuteRequestAndParseDocumentSpans()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation()
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("{ sayHello }");

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task AllScopes_IncludesExecuteRequestAndParseDocumentSpans()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o => o.Scopes = ActivityScopes.All)
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("{ sayHello }");

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task CustomScopes_OnlyValidateAndCompile_LimitsSpans()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o =>
                    o.Scopes = ActivityScopes.ValidateDocument | ActivityScopes.CompileOperation)
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("{ sayHello }");

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task ResolverError_AtRootLevel_MarksOperationAsError()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o =>
                {
                    o.Scopes = ActivityScopes.All;
                    o.IncludeDocument = true;
                })
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("{ causeFatalError }");

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task ResolverError_DeepInTree_MarksNestedFieldAsError()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o =>
                {
                    o.Scopes = ActivityScopes.All;
                    o.IncludeDocument = true;
                })
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync(
                    """
                    {
                        deep {
                            deeper {
                                deeps {
                                    deeper {
                                        deeps {
                                            causeFatalError
                                        }
                                    }
                                }
                            }
                        }
                    }
                    """);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task MaxErrorEvents_CapsErrorEventsOnRootSpan()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o =>
                {
                    o.Scopes = ActivityScopes.ExecuteRequest | ActivityScopes.ExecuteOperation;
                    o.MaxErrorEvents = 2;
                })
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("{ failingItems(count: 5) { fail } }");

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task GraphQLError_WithExtensionsCode_SetsErrorTypeFromCode()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o => o.Scopes = ActivityScopes.All)
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("{ causeCodedError }");

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task GraphQLError_WithoutExtensionsCode_FallsBackToExecutionError()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o =>
                    o.Scopes = ActivityScopes.ExecuteRequest | ActivityScopes.ResolveFieldValue)
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("{ causeUncodedError }");

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task ResolverException_OnNullableField_SetsErrorTypeToExceptionTypeName()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o => o.Scopes = ActivityScopes.All)
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("{ throwInvalidOperation }");

            // assert
            activities.MatchSnapshot(Postfix([NET11_0]));
        }
    }

    [Fact]
    public async Task ComplexityAnalysis_Enabled_RecordsCostInSpan()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o =>
                    o.Scopes = ActivityScopes.All)
                .AddCostAnalyzer()
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("{ sayHello }");

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task DataLoader_BatchExecution_RecordsBatchSpan()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o => o.Scopes = ActivityScopes.All)
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("{ dataLoader(key: \"abc\") }");

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task DataLoader_BatchExecution_With_Keys_RecordsBatchSpan()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o =>
                {
                    o.Scopes = ActivityScopes.All;
                    o.IncludeDataLoaderKeys = true;
                })
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync("{ dataLoader(key: \"abc\") }");

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task VariableCoercion_FailingScalar_RecordsErrorOnCoercionSpan()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o => o.Scopes = ActivityScopes.All)
                .AddQueryType<MoodQuery>()
                .AddType<MoodScalarType>()
                .ExecuteRequestAsync(
                    OperationRequestBuilder.New()
                        .SetDocument("query($mood: Mood!) { greetMood(mood: $mood) }")
                        .SetVariableValues(
                            new Dictionary<string, object?> { { "mood", "happy" } })
                        .Build());

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task VariableCoercion_WithAllScopes_RecordsCoercionSpan()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o => o.Scopes = ActivityScopes.All)
                .AddQueryType<SimpleQuery>()
                .ExecuteRequestAsync(
                    OperationRequestBuilder.New()
                        .SetDocument("query($name: String!) { greeting(name: $name) }")
                        .SetVariableValues(
                            new Dictionary<string, object?> { { "name", "World" } })
                        .Build());

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task DocumentCache_SecondExecution_RecordsCacheHitEvent()
    {
        // arrange
        var services = new ServiceCollection()
            .AddGraphQL()
            .AddInstrumentation(o => o.Scopes = ActivityScopes.All)
            .AddQueryType<SimpleQuery>()
            .Services
            .BuildServiceProvider();

        var executor = await services.GetRequestExecutorAsync();

        var request = OperationRequestBuilder.New()
            .SetDocument("{ sayHello }")
            .SetDocumentHash(new OperationDocumentHash("abc", "sha256", HashFormat.Hex))
            .Build();

        // act - execute twice so second uses cached document
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
            var services = new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o => o.Scopes = ActivityScopes.All)
                .AddQueryType<SimpleQuery>()
                .AddSubscriptionType<SimpleSubscription>()
                .AddInMemorySubscriptions()
                .Services
                .BuildServiceProvider();

            var executor = await services.GetRequestExecutorAsync();
            var sender = services.GetRequiredService<ITopicEventSender>();

            await using var result = await executor.ExecuteAsync(
                "subscription OnMessageSubscription { onMessage }");
            await using var responseStream = result.ExpectResponseStream();

            var results = responseStream.ReadResultsAsync().GetAsyncEnumerator(cts.Token);

            try
            {
                var moveNext = results.MoveNextAsync().AsTask();
                await sender.SendAsync("OnMessage", "hello", cts.Token);
                Assert.True(await moveNext);
                await sender.CompleteAsync("OnMessage");
            }
            finally
            {
                await results.DisposeAsync();
            }

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task SubscriptionEventError_Records_Subscription_Event_Error()
    {
        using var cts = new CancellationTokenSource(5000);

        using (CaptureActivities(out var activities))
        {
            // arrange
            var services = new ServiceCollection()
                .AddGraphQL()
                .AddInstrumentation(o => o.Scopes = ActivityScopes.All)
                .AddQueryType<SimpleQuery>()
                .AddSubscriptionType<SimpleSubscription>()
                .AddInMemorySubscriptions()
                .Services
                .BuildServiceProvider();

            var executor = await services.GetRequestExecutorAsync();
            var sender = services.GetRequiredService<ITopicEventSender>();

            await using var result = await executor.ExecuteAsync(
                "subscription OnFailingMessageSubscription { onFailingMessage }");
            await using var responseStream = result.ExpectResponseStream();

            var results = responseStream.ReadResultsAsync().GetAsyncEnumerator(cts.Token);

            try
            {
                var moveNext = results.MoveNextAsync().AsTask();
                await sender.SendAsync("OnFailingMessage", "hello", cts.Token);
                Assert.True(await moveNext);
                await sender.CompleteAsync("OnFailingMessage");
            }
            finally
            {
                await results.DisposeAsync();
            }

            // assert
            activities.MatchSnapshot(Postfix([NET11_0]));
        }
    }

    public class SimpleQuery
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

        public string CauseUncodedError(IResolverContext context)
            => throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("fail")
                    .SetPath(context.Path)
                    .Build());

        public string CauseCodedError(IResolverContext context)
            => throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("invalid input")
                    .SetCode("INVALID_INPUT")
                    .SetPath(context.Path)
                    .Build());

        public string? ThrowInvalidOperation()
            => throw new InvalidOperationException("custom resolver failure");

        public IEnumerable<FailingItem> FailingItems(int count)
        {
            for (var i = 0; i < count; i++)
            {
                yield return new FailingItem(i);
            }
        }

        public Deep Deep() => new();

        public Task<string?> DataLoader(CustomDataLoader dataLoader, string key)
            => dataLoader.LoadAsync(key);
    }

    public sealed class MoodScalarType : StringType
    {
        public MoodScalarType()
            : base("Mood")
        {
        }

        protected override string OnCoerceInputLiteral(StringValueNode valueLiteral)
            => throw new FormatException(
                $"'{valueLiteral.Value}' is not a recognized mood.");

        protected override string OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
            => throw new FormatException(
                $"'{inputValue.GetString()}' is not a recognized mood.");
    }

    public class MoodQuery
    {
        public string GreetMood([GraphQLType<MoodScalarType>] string mood)
            => $"Greetings, {mood}!";
    }

    public class FailingItem(int index)
    {
        public int Index { get; } = index;

        public string? Fail(IResolverContext context)
            => throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage($"fail-{Index}")
                    .SetCode("CUSTOM_ERROR_CODE")
                    .SetPath(context.Path)
                    .Build());
    }

    public class Deep
    {
        public Deeper Deeper() => new();

        public string CauseFatalError(IResolverContext context)
            => throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("fail")
                    .SetCode("CUSTOM_ERROR_CODE")
                    .SetPath(context.Path)
                    .Build());
    }

    public class Deeper
    {
        public Deep[] Deeps() => [new Deep()];
    }

    public class SimpleSubscription
    {
        [Subscribe]
        public string OnMessage([EventMessage] string message) => message;

        [Subscribe]
        public string OnFailingMessage([EventMessage] string message)
            => throw new InvalidOperationException("Subscription event failed.");
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
