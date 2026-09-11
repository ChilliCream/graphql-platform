using System.Diagnostics;
using System.Text.Json;
using HotChocolate.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Rewriters;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Diagnostics.Listeners;
using HotChocolate.Language;
using HotChocolate.PersistedOperations;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Composite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using FusionPolicyDenialBehavior = HotChocolate.Fusion.Types.PolicyDenialBehavior;
using static CookieCrumble.TestEnvironment;
using static HotChocolate.Fusion.Diagnostics.ActivityTestHelper;
using static HotChocolate.Fusion.Diagnostics.HotChocolateFusionActivitySource;

namespace HotChocolate.Fusion.Diagnostics;

[Collection("Instrumentation")]
public class FusionActivityExecutionDiagnosticListenerTests : FusionTestBase
{
    private const string BatchOperationCountTag = "graphql.source_schema.batch.operation_count";

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
            var executor = await gateway.Services.GetRequestExecutorAsync(
                cancellationToken: TestContext.Current.CancellationToken);

            var request = OperationRequestBuilder.New()
                .SetDocument("{ sayHello }")
                .Build();

            // act
            await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

            // assert
            activities.MatchSnapshot(Postfix([NET11_0]));
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

            var executor = await gateway.Services.GetRequestExecutorAsync(
                cancellationToken: TestContext.Current.CancellationToken);

            var request = OperationRequestBuilder.New()
                .SetDocument("query SayHelloOperation { sayHello }")
                .Build();

            // act
            await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

            // assert
            activities.MatchSnapshot(Postfix([NET11_0]));
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

            var executor = await gateway.Services.GetRequestExecutorAsync(
                cancellationToken: TestContext.Current.CancellationToken);

            var request = OperationRequestBuilder.New()
                .SetDocument("query SayHelloOperation { sayHello_ }")
                .Build();

            // act
            await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

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

            var executor = await gateway.Services.GetRequestExecutorAsync(
                cancellationToken: TestContext.Current.CancellationToken);

            var request = OperationRequestBuilder.New()
                .SetDocument("query SayHelloOperation { causeFatalError }")
                .Build();

            // act
            await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

            // assert
            activities.MatchSnapshot(Postfix([NET11_0]));
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

            var executor = await gateway.Services.GetRequestExecutorAsync(
                cancellationToken: TestContext.Current.CancellationToken);

            var request = OperationRequestBuilder.New()
                .SetDocument("query SayHelloQuery { sayHello }")
                .Build();

            // act
            await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

            // assert
            activities.MatchSnapshot(Postfix([NET11_0]));
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

            var executor = await gateway.Services.GetRequestExecutorAsync(
                cancellationToken: TestContext.Current.CancellationToken);

            var request = OperationRequestBuilder.New()
                .SetDocument("{ sayHello sayGoodbye }")
                .Build();

            // act
            await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

            // assert
            activities.MatchSnapshot(Postfix([NET11_0]));
        }
    }

    [Fact]
    public async Task StepSpan_Should_ReportTheOperationCount_When_TheStepBatchesOperations()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server1 = CreateSourceSchema(
                "a",
                b => b.AddQueryType<BatchSourceSchemaA.Query>());

            using var server2 = CreateSourceSchema(
                "b",
                b => b.AddQueryType<BatchSourceSchemaB.Query>());

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1),
                ("b", server2)
            ],
            configureGatewayBuilder: b => b
                .AddInstrumentation(o => o.Scopes = FusionActivityScopes.All)
                .ModifyPlannerOptions(o => o.EnableRequestGrouping = true));

            var executor = await gateway.Services.GetRequestExecutorAsync(
                cancellationToken: TestContext.Current.CancellationToken);

            // Both root selections read from the same source schema, so their two operations are
            // sent by one step.
            var request = OperationRequestBuilder.New()
                .SetDocument("{ first { rating } second { deliveryEstimate } }")
                .Build();

            // act
            await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

            // assert
            var batchStep = Assert.Single(
                activities.Exported,
                a => a.GetTagItem(BatchOperationCountTag) is not null);
            Assert.Equal(2, batchStep.GetTagItem(BatchOperationCountTag));
        }
    }

    [Fact]
    public async Task StepSpan_Should_TagStandaloneApolloOperation()
    {
        // arrange
        using var server = CreateSourceSchema(
            "a",
            b => b.AddQueryType<Query>());
        using var gateway = await CreateCompositeSchemaAsync([("a", server)]);
        var executor = await gateway.Services.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        var schema = (FusionSchemaDefinition)executor.Schema;
        var planned = PlanOperationForDiagnostics(schema, "{ sayHello }");
        var operationNode = Assert.Single(planned.AllNodes.OfType<OperationExecutionNode>());
        var operationDocument =
            """
            query ApolloProducts($representations: [_Any!]!) {
              _entities(representations: $representations) {
                ... on Product {
                  name
                }
              }
            }
            """u8.ToArray();
        var operation = new OperationSourceText(
            "ApolloProducts",
            OperationType.Query,
            operationDocument,
            OperationSourceTextHash.Compute(operationDocument));
        var apolloNode = ApolloOperationExecutionNode.CreateFromParser(
            operationNode.Id,
            operation,
            "Product",
            "a",
            operationNode.Target,
            [],
            [],
            operationNode.ResultSelectionSet,
            [],
            requiresFileUpload: false,
            schema);
        using var activity = new Activity("ApolloOperationSpanTest");
        activity.Start();

        // act
        ExecutePlanNodeSpan.SetSourceSchemaTags(activity, apolloNode, "a");

        // assert
        Assert.Equal("a", activity.GetTagItem("graphql.source_schema.name"));
        Assert.Equal(
            operation.Name,
            activity.GetTagItem("graphql.source_schema.operation.name"));
        Assert.Equal(
            $"sha256:{operation.Hash.Sha256}",
            activity.GetTagItem("graphql.source_schema.operation.hash"));
    }

    [Fact]
    public void StepSpan_Should_MapEveryExecutionNodeTypeToAKindValue()
    {
        // The subscription event path routes EventStream nodes through the step span,
        // so every execution node type must resolve to a kind value instead of
        // failing the node's execution with a missing dictionary entry.
        foreach (var nodeType in Enum.GetValues<ExecutionNodeType>())
        {
            Assert.True(
                ExecutePlanNodeSpan.KindValues.TryGetValue(nodeType, out var kind),
                $"Missing step kind value for execution node type '{nodeType}'.");
            Assert.False(string.IsNullOrEmpty(kind));
        }

        Assert.Equal(
            "event_stream",
            ExecutePlanNodeSpan.KindValues[ExecutionNodeType.EventStream]);
        Assert.Equal(
            "policy",
            ExecutePlanNodeSpan.KindValues[ExecutionNodeType.Policy]);
    }

    [Fact]
    public void EvaluateRequestPolicies_Should_UseAmbientParent_WhenExecuteRequestScopeIsDisabled()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            var options = new InstrumentationOptions
            {
                Scopes = FusionActivityScopes.EvaluateRequestPolicies
            };
            var listener = new FusionActivityExecutionDiagnosticEventListener(
                new FusionActivityEnricher(options),
                options);
            var context = new PooledRequestContext();
            using var parent = new Activity("ambient-parent").Start();

            // act
            using (listener.EvaluateRequestPolicies(context))
            {
            }

            // assert
            var activity = Assert.Single(activities.Exported);
            Assert.Equal(parent.SpanId, activity.ParentSpanId);
            Assert.Equal("policy_evaluate", activity.GetTagItem("graphql.processing.type"));
        }
    }

    [Fact]
    public void PolicyCompilationError_Should_CreateParentlessErrorSpan()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            var options = new InstrumentationOptions();
            var listener = new FusionActivityExecutionDiagnosticEventListener(
                new FusionActivityEnricher(options),
                options);
            using var parent = new Activity("ambient-parent").Start();

            // act
            listener.PolicyCompilationError("CanReadSecret", new InvalidOperationException("failure"));

            // assert
            var activity = activities.Exported.Single();
            $"""
                operationName={activity.OperationName}; kind={activity.Kind}; parentSpanId={activity.ParentSpanId}; status={activity.Status}; tags={FormatTags(activity.TagObjects)}; events={FormatEvents(activity.Events)}
                """.MatchInlineSnapshot(
                """
                operationName=GraphQL Policy Compilation Error; kind=Internal; parentSpanId=0000000000000000; status=Error; tags=error.type=System.InvalidOperationException, graphql.policy.name=CanReadSecret; events=exception[exception.message=failure, exception.type=System.InvalidOperationException, exception.stacktrace=<present>]
                """);
        }
    }

    [Fact]
    public void PolicyEvents_Should_RecordLowCardinalityTagsAndOnlyAbortMarksSpanError()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            var options = new InstrumentationOptions();
            var listener = new FusionActivityExecutionDiagnosticEventListener(
                new FusionActivityEnricher(options),
                options);
            var context = new PooledRequestContext();

            // act
            using (Source.StartActivity("policy-error", ActivityKind.Internal))
            {
                listener.PolicyEvaluated(
                    context,
                    "CanReadSecret",
                    PolicyEvaluationOutcome.Denied,
                    TimeSpan.FromMilliseconds(12.5));
                listener.PolicySlotDenied(
                    context,
                    "d1",
                    "CanReadSecret",
                    "Query",
                    "secret",
                    FusionPolicyDenialBehavior.Error,
                    "secret reason",
                    Guid.NewGuid(),
                    "subject-1");
                listener.PolicyDenialApplied(
                    null!,
                    null!,
                    SelectionPath.Root.AppendField("alias"),
                    "Query",
                    "secret",
                    "CanReadSecret",
                    FusionPolicyDenialBehavior.Error,
                    deniedCount: 1,
                    totalCount: 1,
                    "secret reason",
                    Guid.NewGuid(),
                    "subject-1");
            }

            using (Source.StartActivity("policy-abort", ActivityKind.Internal))
            {
                listener.PolicyDenialApplied(
                    null!,
                    null!,
                    SelectionPath.Root.AppendField("alias"),
                    "Query",
                    "secret",
                    "CanReadSecret",
                    FusionPolicyDenialBehavior.Abort,
                    deniedCount: 1,
                    totalCount: 1,
                    "secret reason",
                    Guid.NewGuid(),
                    "subject-1");
            }

            // assert
            string.Join(
                Environment.NewLine,
                activities.Exported
                    .OrderBy(activity => activity.OperationName, StringComparer.Ordinal)
                    .Select(activity =>
                        $"{activity.OperationName}: kind={activity.Kind}; status={activity.Status}; "
                            + $"tags={FormatTags(activity.TagObjects)}; events={FormatEvents(activity.Events)}; "
                            + "reason=<absent>; reasonId=<absent>; subject=<absent>"))
                .MatchInlineSnapshot(
                """
                policy-abort: kind=Internal; status=Error; tags=error.type=UNAUTHORIZED_FIELD_OR_TYPE; events=graphql.policy.denial_applied[graphql.policy.expression=CanReadSecret, graphql.policy.on_denied=abort, graphql.selection.path=$.alias, graphql.type.name=Query, graphql.policy.denied_count=1, graphql.policy.total_count=1, graphql.field.name=secret]; reason=<absent>; reasonId=<absent>; subject=<absent>
                policy-error: kind=Internal; status=Unset; tags=<none>; events=graphql.policy.evaluated[graphql.policy.name=CanReadSecret, graphql.policy.outcome=denied, graphql.policy.duration_ms=12.5], graphql.policy.slot_denied[graphql.policy.slot_variable=d1, graphql.policy.expression=CanReadSecret, graphql.type.name=Query, graphql.policy.on_denied=error, graphql.field.name=secret], graphql.policy.denial_applied[graphql.policy.expression=CanReadSecret, graphql.policy.on_denied=error, graphql.selection.path=$.alias, graphql.type.name=Query, graphql.policy.denied_count=1, graphql.policy.total_count=1, graphql.field.name=secret]; reason=<absent>; reasonId=<absent>; subject=<absent>
                """);
        }
    }

    private static string FormatTags(IEnumerable<KeyValuePair<string, object?>> tags)
    {
        var formatted = tags.Select(tag => $"{tag.Key}={tag.Value}").ToArray();
        return formatted.Length == 0 ? "<none>" : string.Join(", ", formatted);
    }

    private static string FormatEvents(IEnumerable<ActivityEvent> events)
        => string.Join(
            ", ",
            events.Select(@event =>
            {
                var tags = @event.Tags?.Where(tag => tag.Key != "exception.stacktrace") ?? [];
                var stackTrace = @event.Tags?.Any(tag => tag.Key == "exception.stacktrace") is true
                    ? ", exception.stacktrace=<present>"
                    : string.Empty;
                return $"{@event.Name}[{FormatTags(tags)}{stackTrace}]";
            }));

    [Fact]
    public async Task PersistedOperation_LoadsFromStorage_DefaultScopes()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            var storage = new InMemoryOperationDocumentStorage();
            storage.Add("say-hello-persisted-id", "{ sayHello }");

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

            var executor = await gateway.Services.GetRequestExecutorAsync(
                cancellationToken: TestContext.Current.CancellationToken);

            // act
            await executor.ExecuteAsync(
                OperationRequest.FromId("say-hello-persisted-id"),
                TestContext.Current.CancellationToken);

            // assert
            activities.MatchSnapshot(Postfix([NET11_0]));
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

            var executor = await gateway.Services.GetRequestExecutorAsync(
                cancellationToken: TestContext.Current.CancellationToken);

            var request = OperationRequestBuilder.New()
                .SetDocumentId("a8c5e2f1d3b4a6e7c9d0f1a2b3c4d5e6")
                .Build();

            // act
            await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

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

            var executor = await gateway.Services.GetRequestExecutorAsync(
                cancellationToken: TestContext.Current.CancellationToken);

            var request = OperationRequestBuilder.New()
                .SetDocument("{ sayHello }")
                .Build();

            // act
            await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

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

            var executor = await gateway.Services.GetRequestExecutorAsync(
                cancellationToken: TestContext.Current.CancellationToken);

            var request = OperationRequestBuilder.New()
                .SetDocument("{ sayHello")
                .Build();

            // act
            await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

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

            var executor = await gateway.Services.GetRequestExecutorAsync(
                cancellationToken: TestContext.Current.CancellationToken);

            var request = OperationRequestBuilder.New()
                .SetDocument("{ unknownField123 }")
                .Build();

            // act
            await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

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

            var executor = await gateway.Services.GetRequestExecutorAsync(
                cancellationToken: TestContext.Current.CancellationToken);

            var request = OperationRequestBuilder.New()
                .SetDocument("{ sayHello }")
                .Build();

            // act
            await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

            // assert
            activities.MatchSnapshot(Postfix([NET11_0]));
        }
    }

    [Fact]
    public async Task VariableCoercion_FailingScalar_RecordsErrorOnCoercionSpan()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server1 = CreateSourceSchema(
                "a",
                b => b
                    .AddQueryType<Query>()
                    .AddType<MoodScalarType>());

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1)
            ],
            configureGatewayBuilder: b => b.AddInstrumentation(o =>
                o.Scopes = FusionActivityScopes.All));

            var executor = await gateway.Services.GetRequestExecutorAsync(
                cancellationToken: TestContext.Current.CancellationToken);

            var request = OperationRequestBuilder.New()
                .SetDocument("query($mood: Mood!) { greetMood(mood: $mood) }")
                .SetVariableValues(
                    new Dictionary<string, object?> { { "mood", "happy" } })
                .Build();

            // act
            await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

            // assert
            activities.MatchSnapshot(Postfix([NET11_0]));
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

            var executor = await gateway.Services.GetRequestExecutorAsync(
                cancellationToken: TestContext.Current.CancellationToken);

            var request = OperationRequestBuilder.New()
                .SetDocument("{ sayHello }")
                .Build();

            // act
            await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

            // assert
            activities.MatchSnapshot(Postfix([NET11_0]));
        }
    }

    [Fact]
    public async Task RequestSpanDisplayName_Should_BeOperationType_When_OperationNameInSpanNameDisabled()
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

            var executor = await gateway.Services.GetRequestExecutorAsync(
                cancellationToken: TestContext.Current.CancellationToken);

            var request = OperationRequestBuilder.New()
                .SetDocument("query GetHeroName { sayHello }")
                .Build();

            // act
            await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

            // assert
            var requestSpan = activities.Exported
                .Single(a => a.OperationName == "GraphQL Operation");
            Assert.Equal("query", requestSpan.DisplayName);
        }
    }

    [Fact]
    public async Task RequestSpanDisplayName_Should_IncludeOperationName_When_OperationNameInSpanNameEnabledAndNamed()
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
                o.IncludeOperationNameInSpanName = true;
            }));

            var executor = await gateway.Services.GetRequestExecutorAsync(
                cancellationToken: TestContext.Current.CancellationToken);

            var request = OperationRequestBuilder.New()
                .SetDocument("query GetHeroName { sayHello }")
                .Build();

            // act
            await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

            // assert
            var requestSpan = activities.Exported
                .Single(a => a.OperationName == "GraphQL Operation");
            Assert.Equal("query GetHeroName", requestSpan.DisplayName);
        }
    }

    [Fact]
    public async Task RequestSpanDisplayName_Should_FallBackToOperationType_When_OperationNameInSpanNameEnabledAndAnonymous()
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
                o.IncludeOperationNameInSpanName = true;
            }));

            var executor = await gateway.Services.GetRequestExecutorAsync(
                cancellationToken: TestContext.Current.CancellationToken);

            var request = OperationRequestBuilder.New()
                .SetDocument("{ sayHello }")
                .Build();

            // act
            await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

            // assert
            var requestSpan = activities.Exported
                .Single(a => a.OperationName == "GraphQL Operation");
            Assert.Equal("query", requestSpan.DisplayName);
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

            var executor = await gateway.Services.GetRequestExecutorAsync(
                cancellationToken: TestContext.Current.CancellationToken);

            var request = OperationRequestBuilder.New()
                .SetDocument("{ sayHello }")
                .Build();

            // act
            await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

            // assert
            activities.MatchSnapshot(Postfix([NET11_0]));
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

            var executor = await gateway.Services.GetRequestExecutorAsync(
                cancellationToken: TestContext.Current.CancellationToken);

            var request = OperationRequestBuilder.New()
                .SetDocument("{ sayHello sayGoodbye }")
                .Build();

            // act
            await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

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
            var coordination = new DeepErrorCoordination();

            using var server1 = CreateSourceSchema(
                "a",
                b => b.AddQueryType<QueryAWithSignal>(),
                configureServices: s => s.AddSingleton(coordination));

            using var server2 = CreateSourceSchema(
                "b",
                b => b.AddQueryType<QueryBWithDeepError>(),
                configureServices: s => s.AddSingleton(coordination));

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

            var executor = await gateway.Services.GetRequestExecutorAsync(
                cancellationToken: TestContext.Current.CancellationToken);

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
            await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

            // assert
            activities.MatchSnapshot(Postfix([NET11_0]));
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

        var executor = await gateway.Services.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // act - execute twice so second uses cached document
        var request = OperationRequestBuilder.New()
            .SetDocument("{ sayHello }")
            .SetDocumentHash(new OperationDocumentHash("abc", "sha256", HashFormat.Hex))
            .Build();

        await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        using (CaptureActivities(out var activities))
        {
            await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

            // assert
            activities.MatchSnapshot(Postfix([NET11_0]));
        }
    }

    [Fact]
    public async Task SubscriptionEvent_Records_Subscription_Event_Span()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

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

            var executor = await gateway.Services.GetRequestExecutorAsync(
                cancellationToken: TestContext.Current.CancellationToken);

            // act
            await using var result = await executor.ExecuteAsync(
                "subscription OnMessageSubscription { onMessage }",
                TestContext.Current.CancellationToken);
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
            activities.MatchSnapshot(Postfix([NET11_0]));
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

            var executor = await gateway.Services.GetRequestExecutorAsync(
                cancellationToken: TestContext.Current.CancellationToken);

            // act
            await using var result = await executor.ExecuteAsync(
                "subscription OnFailingMessageSubscription { onFailingMessage }",
                TestContext.Current.CancellationToken);
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

            var executor = await gateway.Services.GetRequestExecutorAsync(
                cancellationToken: TestContext.Current.CancellationToken);

            // act
            IExecutionResult? result = null;

            try
            {
                result = await executor.ExecuteAsync(
                    "subscription OnMessageSubscription { onMessage }",
                    TestContext.Current.CancellationToken);
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

    public static class BatchSourceSchemaA
    {
        [EntityKey("id")]
        public record Product(int Id);

        public sealed class Query
        {
            public Product GetFirst() => new(1);

            public Product GetSecond() => new(2);
        }
    }

    public static class BatchSourceSchemaB
    {
        [EntityKey("id")]
        public record Product(int Id)
        {
            public int Rating => Id + 4;

            public int DeliveryEstimate => Id + 1;
        }

        public sealed class Query
        {
            [Lookup]
            [Internal]
            public Product GetProductById(int id) => new(id);
        }
    }

    public class Query
    {
        public string SayHello() => "hello";

        public string CauseFatalError(IResolverContext context)
            => throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("fail")
                    .SetCode("CUSTOM_ERROR_CODE")
                    .SetPath(context.Path)
                    .Build());

        public Deep Deep() => new();

        public string GreetMood([GraphQLType<MoodScalarType>] string mood)
            => $"Greetings, {mood}!";
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

    [GraphQLName("Query")]
    public class QueryA
    {
        public string SayHello() => "hello";
    }

    [GraphQLName("Query")]
    public class QueryAWithSignal
    {
        public string SayHello([Service] DeepErrorCoordination coordination)
        {
            coordination.SignalSourceACompleted();
            return "hello";
        }
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
        public async Task<string> CauseFatalError(
            IResolverContext context,
            [Service] DeepErrorCoordination coordination)
        {
            await coordination.WaitForSourceACompletedAsync();

            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("deep fail")
                    .SetCode("CUSTOM_ERROR_CODE")
                    .SetPath(context.Path)
                    .Build());
        }
    }

    public sealed class DeepErrorCoordination
    {
        private readonly TaskCompletionSource _sourceACompleted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void SignalSourceACompleted()
            => _sourceACompleted.TrySetResult();

        public Task WaitForSourceACompletedAsync()
            => _sourceACompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));
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
        public string OnFailingMessage([EventMessage] string message, IResolverContext context)
            => throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Subscription event failed.")
                    .SetCode("CUSTOM_ERROR_CODE")
                    .SetPath(context.Path)
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

    private static OperationPlan PlanOperationForDiagnostics(
        FusionSchemaDefinition schema,
        string operationText)
    {
        var pool = new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
            new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>());
        var operationDocument = Utf8GraphQLParser.Parse(operationText);
        var rewriter = new DocumentRewriter(schema);
        var rewritten = rewriter.RewriteDocument(operationDocument, operationName: null);
        var operation = rewritten.Definitions.OfType<OperationDefinitionNode>().First();
        var compiler = new OperationCompiler(schema, pool);
        var planner = new OperationPlanner(schema, compiler);

        return planner.CreatePlan("123456789101112", "123456789101112", "123456789101112", operation);
    }
}
