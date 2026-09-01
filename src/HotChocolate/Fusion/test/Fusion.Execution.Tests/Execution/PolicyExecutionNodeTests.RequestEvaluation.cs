using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution;

public sealed partial class PolicyExecutionNodeTests
{
    private static void AddRootResult(
        OperationPlanContext context,
        OperationPlan plan,
        byte[] payload)
    {
        var operation = plan.AllNodes.OfType<OperationExecutionNode>().Single();
        var arena = context.MemorySource.GetNextArena();
        var document = SourceResultDocument.Parse(arena, payload, payload.Length);
        context.AddPartialResult(
            operation.Source,
            new SourceSchemaResult(CompactPath.Root, document),
            operation.ResultSelectionSet,
            containsErrors: false);
    }

    [Fact]
    public async Task ExecuteAsync_Should_SkipDataBearingResidual_When_RequestGroupAllows()
    {
        // arrange
        var requestPolicy = new SwitchableCountingPolicy("CanRequest");
        var dataPolicy = new CountingRequirementPolicy("CanResource");
        var executor = await CreateExpressionExecutorAsync(
            """@policy(names: [["CanRequest"], ["CanResource"]], onDenied: ERROR)""",
            requestPolicy,
            dataPolicy);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ secret }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal((1, 0), (requestPolicy.EvaluationCount, dataPolicy.EvaluationCount));
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "secret": "classified"
              }
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_EvaluateDataBearingResidual_When_RequestGroupDenies()
    {
        // arrange
        var requestPolicy = new SwitchableCountingPolicy("CanRequest") { Deny = true };
        var dataPolicy = new CountingRequirementPolicy("CanResource");
        var executor = await CreateExpressionExecutorAsync(
            """@policy(names: [["CanRequest"], ["CanResource"]], onDenied: ERROR)""",
            requestPolicy,
            dataPolicy);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ secret }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal((1, 1), (requestPolicy.EvaluationCount, dataPolicy.EvaluationCount));
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "secret": "classified"
              }
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_PrecreateRequestMemoForResidualOnlyMixedExpressionVariableBatch()
    {
        // arrange
        var requestPolicy = new BlockingPolicy("CanRequest");
        var dataPolicy = new CountingRequirementPolicy("CanResource");
        var executor = await CreateExpressionExecutorAsync(
            """@policy(names: [["CanRequest"], ["CanResource"]], onDenied: ERROR)""",
            requestPolicy,
            dataPolicy);
        const string operation =
            """
            query($include: Boolean!) {
              secret @include(if: $include)
            }
            """;
        var plan = PlanOperation(Assert.IsType<FusionSchemaDefinition>(executor.Schema), operation);
        using var variableValues = JsonDocument.Parse(
            """[{"include":true},{"include":true}]""");
        var request = VariableBatchRequest.FromSourceText(operation, variableValues);

        // act
        var execution = executor.ExecuteAsync(request, TestContext.Current.CancellationToken);
        await requestPolicy.Started.Task.WaitAsync(TestContext.Current.CancellationToken);
        requestPolicy.Release.TrySetResult();
        await using var result = await execution;

        // assert
        Assert.Empty(plan.PolicySlots);
        Assert.Equal(["CanRequest"], plan.RequestPolicyNames);
        Assert.Equal((1, 2), (requestPolicy.EvaluationCount, dataPolicy.EvaluationCount));
        var batch = Assert.IsType<OperationResultBatch>(result);
        batch.Results.Select(current => current.ToJson()).ToArray().MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "secret": "classified"
              }
            }
            """,
            """
            {
              "data": {
                "secret": "classified"
              }
            }
            """
        ]);
    }

    [Fact]
    public async Task EvaluateRequestPolicyAsync_Should_ShareInFlightRequestStateEvaluation()
    {
        // arrange
        var policy = new BlockingPolicy();
        IServiceProvider? requestServices = null;
        var executor = await CreateExecutorAsync(
            PolicyDenialBehavior.Null,
            policy,
            captureRequestServices: services => requestServices = services);
        var schema = Assert.IsType<FusionSchemaDefinition>(executor.Schema);
        var plan = PlanOperation(schema, "{ secret }");
        var requestContextPool = schema.Services.GetRequiredService<ObjectPool<PooledRequestContext>>();
        var requestContext = requestContextPool.Get();
        using var executionCts = new CancellationTokenSource();
        var context = new OperationPlanContext(
            schema.Services.GetRequiredService<INodeIdParser>(),
            schema.Services.GetRequiredService<IFusionExecutionDiagnosticEvents>(),
            schema.Services.GetRequiredService<IErrorHandler>());

        try
        {
            requestContext.Initialize(
                schema,
                executor.Version,
                OperationRequestBuilder.New().SetDocument("{ secret }").Build(),
                requestIndex: 0,
                requestServices: requestServices!,
                requestAborted: CancellationToken.None);
            requestContext.SetPolicySnapshot(schema.Policies.GetSnapshot());
            PolicyRequestState.GetOrCreate(
                requestContext,
                plan,
                schema.Services.GetRequiredService<IFusionExecutionDiagnosticEvents>());
            context.Initialize(
                requestContext,
                VariableValueCollection.Empty,
                plan,
                executionCts,
                new MemoryArena());

            // act
            var first = context.EvaluateRequestPolicyAsync(
                "CanReadSecret",
                new ClaimsPrincipal(),
                TestContext.Current.CancellationToken).AsTask();
            await policy.Started.Task.WaitAsync(TestContext.Current.CancellationToken);
            var second = context.EvaluateRequestPolicyAsync(
                "CanReadSecret",
                new ClaimsPrincipal(),
                TestContext.Current.CancellationToken).AsTask();
            policy.Release.TrySetResult();
            var decisions = await Task.WhenAll(first, second);

            // assert
            Assert.Equal(1, policy.EvaluationCount);
            Assert.Equal((true, true), (decisions[0].IsDenied, decisions[1].IsDenied));
        }
        finally
        {
            await context.DisposeAsync();
            context.Destroy();
            requestContextPool.Return(requestContext);
        }
    }

    [Fact]
    public async Task EvaluateRequestPolicyAsync_Should_CacheFailureInRequestState()
    {
        // arrange
        var policy = new ThrowingPolicy();
        IServiceProvider? requestServices = null;
        var executor = await CreateExecutorAsync(
            PolicyDenialBehavior.Null,
            policy,
            captureRequestServices: services => requestServices = services);
        var schema = Assert.IsType<FusionSchemaDefinition>(executor.Schema);
        var plan = PlanOperation(schema, "{ secret }");
        var requestContextPool = schema.Services.GetRequiredService<ObjectPool<PooledRequestContext>>();
        var requestContext = requestContextPool.Get();
        using var executionCts = new CancellationTokenSource();
        var context = new OperationPlanContext(
            schema.Services.GetRequiredService<INodeIdParser>(),
            schema.Services.GetRequiredService<IFusionExecutionDiagnosticEvents>(),
            schema.Services.GetRequiredService<IErrorHandler>());

        try
        {
            requestContext.Initialize(
                schema,
                executor.Version,
                OperationRequestBuilder.New().SetDocument("{ secret }").Build(),
                requestIndex: 0,
                requestServices: requestServices!,
                requestAborted: CancellationToken.None);
            requestContext.SetPolicySnapshot(schema.Policies.GetSnapshot());
            PolicyRequestState.GetOrCreate(
                requestContext,
                plan,
                schema.Services.GetRequiredService<IFusionExecutionDiagnosticEvents>());
            context.Initialize(
                requestContext,
                VariableValueCollection.Empty,
                plan,
                executionCts,
                new MemoryArena());

            // act
            var firstError = await Assert.ThrowsAsync<InvalidOperationException>(
                () => context.EvaluateRequestPolicyAsync(
                    "CanReadSecret",
                    new ClaimsPrincipal(),
                    TestContext.Current.CancellationToken).AsTask());
            var secondError = await Assert.ThrowsAsync<InvalidOperationException>(
                () => context.EvaluateRequestPolicyAsync(
                    "CanReadSecret",
                    new ClaimsPrincipal(),
                    TestContext.Current.CancellationToken).AsTask());

            // assert
            Assert.Equal(1, policy.EvaluationCount);
            Assert.Same(firstError, secondError);
        }
        finally
        {
            await context.DisposeAsync();
            context.Destroy();
            requestContextPool.Return(requestContext);
        }
    }

    [Fact]
    public async Task ExecuteAsync_Should_EvaluateRequirementFreePolicyOnceAcrossVariableBatch()
    {
        // arrange
        var policy = new CountingDenyPolicy();
        var executor = await CreateExecutorAsync(PolicyDenialBehavior.Abort, policy);
        using var variableValues = JsonDocument.Parse(
            """[{"include":false},{"include":true}]""");
        var request = VariableBatchRequest.FromSourceText(
            "query($include: Boolean!) { secret @include(if: $include) }",
            variableValues);

        // act
        await using var result = await executor.ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, policy.EvaluationCount);
        var batch = Assert.IsType<OperationResultBatch>(result);
        var results = batch.Results
            .Select((current, index) => index == 0
                ? current.ToJson()
                : NormalizeReasonId(current.ToJson()))
            .ToArray();
        results.MatchInlineSnapshots(
        [
            """
            {
              "data": {}
            }
            """,
            """
            {
              "variableIndex": 1,
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "extensions": {
                    "code": "UNAUTHORIZED_FIELD_OR_TYPE",
                    "reasonId": "00000000-0000-0000-0000-000000000000"
                  }
                }
              ],
              "data": null
            }
            """
        ]);
    }

    [Fact]
    public async Task ExecuteAsync_Should_SkipPolicyEvaluationForWarmupRequest()
    {
        // arrange
        var policy = new CountingDenyPolicy();
        var executor = await CreateExecutorAsync(PolicyDenialBehavior.Abort, policy);
        var request = OperationRequestBuilder.New()
            .SetDocument("{ secret }")
            .MarkAsWarmupRequest()
            .Build();

        // act
        await using var result = await executor.ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<WarmupExecutionResult>(result);
        Assert.Equal(0, policy.EvaluationCount);
    }

    [Fact]
    public async Task ExecuteAsync_Should_NotEvaluateReusedNodeLookupGate_When_ParentIsExcluded()
    {
        // arrange
        var policy = new CountingDenyPolicy();
        var executor = await CreateAbstractTypePolicyExecutorAsync(
            policy,
            """
            type Query {
              node(id: ID!): Node @lookup
            }

            interface Node {
              id: ID!
            }

            type Product implements Node @policy(names: "CanReadSecret") {
              id: ID!
              secret: String @policy(names: "CanReadSecret")
            }
            """,
            """{"data":{"node":{"__typename":"Product","id":"1","secret":"classified"}}}""");
        const string operation =
            """
            query($include: Boolean!) {
              node(id: "1") @include(if: $include) {
                ... on Product {
                  secret
                }
              }
            }
            """;
        var plan = PlanOperation(Assert.IsType<FusionSchemaDefinition>(executor.Schema), operation);
        var slot = Assert.Single(plan.PolicySlots);
        Assert.Equal(new ulong[] { 1 }, slot.GuardMasks);
        Assert.Equal(2, slot.Coordinates.Length);
        var request = OperationRequestBuilder.New()
            .SetDocument(operation)
            .SetVariableValues(new Dictionary<string, object?> { ["include"] = false })
            .Build();

        // act
        await using var result = await executor.ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(0, policy.EvaluationCount);
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {}
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_NotEvaluateReentrantLookupGate_When_ParentIsExcluded()
    {
        // arrange
        var policy = new CountingDenyPolicy();
        var downstreamClient = new RecordingLookupClient();
        var executor = await CreateLookupExecutorAsync(
            downstreamClient,
            policy: policy);
        var request = OperationRequestBuilder.New()
            .SetDocument(
                """
                query($include: Boolean!) {
                  topProducts @include(if: $include) {
                    price
                  }
                }
                """)
            .SetVariableValues(new Dictionary<string, object?> { ["include"] = false })
            .Build();

        // act
        await using var result = await executor.ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal((0, 0), (policy.EvaluationCount, downstreamClient.ExecutionCount));
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {}
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReportRequestPolicyDiagnosticsOnce()
    {
        // arrange
        var policy = new CountingDenyPolicy();
        var listener = new RequestPolicyDiagnosticListener();
        var executor = await CreateExecutorAsync(
            PolicyDenialBehavior.Abort,
            policy,
            diagnosticListener: listener);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ secret }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(
            (Scopes: 1, Evaluations: 1),
            (Scopes: listener.ScopeCount, Evaluations: listener.EvaluationCount));
        listener.Denials.MatchInlineSnapshots(
        [
            """
            __fusion_policy_0|CanReadSecret|Query.secret|Abort|denied by counting policy
            """
        ]);
    }

    [Theory]
    [InlineData(true, "name-id")]
    [InlineData(false, "subject-id")]
    public async Task ExecuteAsync_Should_ReportStableSubjectIdentity(
        bool includeNameIdentifier,
        string expectedSubjectId)
    {
        // arrange
        var listener = new RequestPolicyDiagnosticListener();
        var executor = await CreateExecutorAsync(
            PolicyDenialBehavior.Error,
            new CountingDenyPolicy(),
            diagnosticListener: listener);
        var claims = new List<Claim> { new("sub", "subject-id") };
        if (includeNameIdentifier)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, "name-id"));
        }
        var request = OperationRequestBuilder.New()
            .SetDocument("{ secret }")
            .SetUser(new ClaimsPrincipal(new ClaimsIdentity(claims, "test")))
            .Build();

        // act
        await using var result = await executor.ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(expectedSubjectId, Assert.Single(listener.Events).SubjectId);
    }

    [Fact]
    public async Task ExecuteAsync_Should_HydrateUserForDataBearingOnlyPlan()
    {
        // arrange
        var policy = new PrincipalRequirementPolicy();
        var executor = await CreateExecutorAsync(PolicyDenialBehavior.Error, policy);
        var schema = Assert.IsType<FusionSchemaDefinition>(executor.Schema);
        var plan = PlanOperation(schema, "{ secret }");
        var user = new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim("sub", "subject-id"),
                new Claim(ClaimTypes.NameIdentifier, "name-id")
            ],
            "test"));
        var request = OperationRequestBuilder.New()
            .SetDocument("{ secret }")
            .SetUser(user)
            .Build();

        // act
        await using var result = await executor.ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);

        // assert
        $"slots={plan.PolicySlots.Length}; subject={policy.SubjectId}"
            .MatchInlineSnapshot("slots=0; subject=name-id");
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "secret": "classified"
              }
            }
            """);
    }

    [Fact]
    public async Task OperationPlanContextPool_Should_ClearPolicyDenialsBeforeUnprotectedReuse()
    {
        // arrange
        var listener = new RequestPolicyDiagnosticListener();
        IServiceProvider? requestServices = null;
        var executor = await CreateExecutorAsync(
            PolicyDenialBehavior.Error,
            new CountingDenyPolicy(),
            diagnosticListener: listener,
            captureRequestServices: services => requestServices = services);
        var schema = Assert.IsType<FusionSchemaDefinition>(executor.Schema);
        var deniedPlan = PlanOperation(schema, "{ secret }");
        var unprotectedPlan = PlanOperation(schema, "{ role }");
        var contextPool = schema.Services.GetRequiredService<OperationPlanContextPool>();
        var requestContextPool = schema.Services.GetRequiredService<ObjectPool<PooledRequestContext>>();
        var firstRequestContext = requestContextPool.Get();
        var firstContext = contextPool.Rent();
        OperationPlanContext reusedContext;
        string deniedJson;

        try
        {
            firstRequestContext.Initialize(
                schema,
                executor.Version,
                OperationRequestBuilder.New().SetDocument("{ secret }").Build(),
                requestIndex: 0,
                requestServices: requestServices!,
                requestAborted: CancellationToken.None);
            using var firstCts = new CancellationTokenSource();
            using var firstMemory = new MemoryArena();
            firstContext.Initialize(
                firstRequestContext,
                VariableValueCollection.Empty,
                deniedPlan,
                firstCts,
                firstMemory);
            await firstContext.ReevaluatePolicySlotsAsync(TestContext.Current.CancellationToken);
            AddRootResult(
                firstContext,
                deniedPlan,
                """{"data":{"secret":"classified"}}"""u8.ToArray());
            await using var deniedResult = firstContext.Complete();
            deniedJson = NormalizeReasonId(deniedResult.ToJson());
            reusedContext = firstContext;
        }
        finally
        {
            await firstContext.DisposeAsync();
            requestContextPool.Return(firstRequestContext);
        }

        // act
        var secondRequestContext = requestContextPool.Get();
        var secondContext = contextPool.Rent();
        string actual;
        try
        {
            secondRequestContext.Initialize(
                schema,
                executor.Version,
                OperationRequestBuilder.New().SetDocument("{ role }").Build(),
                requestIndex: 0,
                requestServices: requestServices!,
                requestAborted: CancellationToken.None);
            using var secondCts = new CancellationTokenSource();
            using var secondMemory = new MemoryArena();
            secondContext.Initialize(
                secondRequestContext,
                VariableValueCollection.Empty,
                unprotectedPlan,
                secondCts,
                secondMemory);
            AddRootResult(
                secondContext,
                unprotectedPlan,
                """{"data":{"role":"admin"}}"""u8.ToArray());
            await using var result = secondContext.Complete();
            actual =
                $"same={ReferenceEquals(reusedContext, secondContext)}; "
                + $"denyFlags={secondContext.PolicyDenyFlags}; events={listener.Events.Count}\n"
                + $"denied={deniedJson}\nreused={result.ToJson()}";
        }
        finally
        {
            await secondContext.DisposeAsync();
            requestContextPool.Return(secondRequestContext);
        }

        // assert
        actual.MatchInlineSnapshot(
            """
            same=True; denyFlags=0; events=1
            denied={
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "path": [
                    "secret"
                  ],
                  "extensions": {
                    "code": "UNAUTHORIZED_FIELD_OR_TYPE",
                    "reasonId": "00000000-0000-0000-0000-000000000000"
                  }
                }
              ],
              "data": {
                "secret": null
              }
            }
            reused={
              "data": {
                "role": "admin"
              }
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ShareSchemaCoordinateReasonIdAcrossAliases()
    {
        // arrange
        var listener = new RequestPolicyDiagnosticListener();
        var executor = await CreateExecutorAsync(
            PolicyDenialBehavior.Error,
            new CountingDenyPolicy(),
            diagnosticListener: listener);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ first: secret second: secret }",
            TestContext.Current.CancellationToken);

        // assert
        var json = result.ToJson();
        var reasonIds = ReasonIdPattern().Matches(json)
            .Select(match => Guid.Parse(match.Value))
            .Distinct()
            .ToArray();
        Assert.Single(listener.Events);
        Assert.Equal([listener.Events[0].ReasonId], reasonIds);
        NormalizeReasonId(json).MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "path": [
                    "first"
                  ],
                  "extensions": {
                    "code": "UNAUTHORIZED_FIELD_OR_TYPE",
                    "reasonId": "00000000-0000-0000-0000-000000000000"
                  }
                },
                {
                  "message": "The current user is not authorized to access this resource.",
                  "path": [
                    "second"
                  ],
                  "extensions": {
                    "code": "UNAUTHORIZED_FIELD_OR_TYPE",
                    "reasonId": "00000000-0000-0000-0000-000000000000"
                  }
                }
              ],
              "data": {
                "first": null,
                "second": null
              }
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ApplyErrorFilterEquallyToSlotAndNodeAbort()
    {
        // arrange
        static IError Filter(IError error)
            => error.WithMessage("filtered denial").WithCode("FILTERED_DENIAL");
        var slotExecutor = await CreateExecutorAsync(
            PolicyDenialBehavior.Abort,
            new CountingDenyPolicy(),
            errorFilter: Filter);
        var nodeExecutor = await CreateExecutorAsync(
            PolicyDenialBehavior.Abort,
            new DenyRequirementPolicy(),
            errorFilter: Filter,
            sourceClient: new RecordingRequirementClient());

        // act
        await using var slotResult = await slotExecutor.ExecuteAsync(
            "{ secret }",
            TestContext.Current.CancellationToken);
        await using var nodeResult = await nodeExecutor.ExecuteAsync(
            "{ secret }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(
            NormalizeReasonId(nodeResult.ToJson()),
            NormalizeReasonId(slotResult.ToJson()));
        NormalizeReasonId(slotResult.ToJson()).MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "filtered denial",
                  "extensions": {
                    "code": "FILTERED_DENIAL",
                    "reasonId": "00000000-0000-0000-0000-000000000000"
                  }
                }
              ],
              "data": null
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_KeepGateOpen_When_RequestDenialIsBelowResidualSeverity()
    {
        // arrange
        var resourcePolicy = new NamedRequirementPolicy("CanResource", deny: false);
        var executor = await CreateExpressionExecutorAsync(
            """@policy(names: "CanRequest") @policy(names: "CanResource", onDenied: ABORT)""",
            new NamedStaticPolicy("CanRequest", deny: true),
            resourcePolicy);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ secret }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, resourcePolicy.EvaluationCount);
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "secret": "classified"
              }
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_DenyGate_When_RequestDenialDominatesResidualSeverity()
    {
        // arrange
        var resourcePolicy = new NamedRequirementPolicy("CanResource", deny: false);
        var executor = await CreateExpressionExecutorAsync(
            """@policy(names: "CanRequest", onDenied: ERROR) @policy(names: "CanResource")""",
            new NamedStaticPolicy("CanRequest", deny: true),
            resourcePolicy);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ secret }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(0, resourcePolicy.EvaluationCount);
        NormalizeReasonId(result.ToJson()).MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "path": [
                    "secret"
                  ],
                  "extensions": {
                    "code": "UNAUTHORIZED_FIELD_OR_TYPE",
                    "reasonId": "00000000-0000-0000-0000-000000000000"
                  }
                }
              ],
              "data": {
                "secret": null
              }
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ShortCircuit_When_RequestPolicyDeniesRootObject()
    {
        // arrange
        var policy = new CountingDenyPolicy();
        var executor = await CreateRootObjectPolicyExecutorAsync(policy);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ secret }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, policy.EvaluationCount);
        NormalizeReasonId(result.ToJson()).MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "extensions": {
                    "code": "UNAUTHORIZED_FIELD_OR_TYPE",
                    "reasonId": "00000000-0000-0000-0000-000000000000"
                  }
                }
              ],
              "data": null
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_EmitOneShortCircuitErrorAndAllCoordinateEvents()
    {
        // arrange
        var listener = new RequestPolicyDiagnosticListener();
        var executor = await CreateTwoAbortFieldExecutorAsync(listener);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ first second }",
            TestContext.Current.CancellationToken);

        // assert
        var json = result.ToJson();
        var reasonId = Guid.Parse(Assert.Single(ReasonIdPattern().Matches(json)).Value);
        Assert.Equal(2, listener.Events.Count);
        Assert.Equal(listener.Events[0].ReasonId, reasonId);
        listener.Denials.MatchInlineSnapshots(
        [
            """
            __fusion_policy_0|CanReadSecret|Query.first|Abort|denied by counting policy
            """,
            """
            __fusion_policy_0|CanReadSecret|Query.second|Abort|denied by counting policy
            """
        ]);
        NormalizeReasonId(json).MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "extensions": {
                    "code": "UNAUTHORIZED_FIELD_OR_TYPE",
                    "reasonId": "00000000-0000-0000-0000-000000000000"
                  }
                }
              ],
              "data": null
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_UseLastEqualSeverityApplicationInCoordinateOrder()
    {
        // arrange
        var firstListener = new RequestPolicyDiagnosticListener();
        var secondListener = new RequestPolicyDiagnosticListener();
        var policies = new IPolicy[]
        {
            new NamedStaticPolicy("First", deny: true, reason: "first reason"),
            new NamedStaticPolicy("Second", deny: true, reason: "second reason")
        };
        var firstExecutor = await CreateOrderedApplicationExecutorAsync(
            """@policy(names: "First", onDenied: ERROR) @policy(names: "Second", onDenied: ERROR)""",
            firstListener,
            policies);
        var secondExecutor = await CreateOrderedApplicationExecutorAsync(
            """@policy(names: "Second", onDenied: ERROR) @policy(names: "First", onDenied: ERROR)""",
            secondListener,
            policies);

        // act
        await using var firstResult = await firstExecutor.ExecuteAsync(
            "{ secret }",
            TestContext.Current.CancellationToken);
        await using var secondResult = await secondExecutor.ExecuteAsync(
            "{ secret }",
            TestContext.Current.CancellationToken);

        // assert
        new[] { Assert.Single(firstListener.Denials), Assert.Single(secondListener.Denials) }
            .MatchInlineSnapshots(
            [
                """
                __fusion_policy_0|Second|Query.secret|Error|second reason
                """,
                """
                __fusion_policy_0|First|Query.secret|Error|first reason
                """
            ]);
    }

    [Fact]
    public async Task ExecuteAsync_Should_RespectErrorHandlingModeForDeniedNonNullField()
    {
        // arrange
        var listener = new RequestPolicyDiagnosticListener();
        var executor = await CreateErrorHandlingExecutorAsync(listener);
        var nullRequest = OperationRequestBuilder.New()
            .SetDocument("{ parent { protected sibling } outside }")
            .SetErrorHandlingMode(ErrorHandlingMode.Null)
            .Build();
        var propagateRequest = OperationRequestBuilder.New()
            .SetDocument("{ parent { protected sibling } outside }")
            .SetErrorHandlingMode(ErrorHandlingMode.Propagate)
            .Build();

        // act
        await using var nullResult = await executor.ExecuteAsync(
            nullRequest,
            TestContext.Current.CancellationToken);
        await using var propagateResult = await executor.ExecuteAsync(
            propagateRequest,
            TestContext.Current.CancellationToken);

        // assert
        var nullJson = nullResult.ToJson();
        var propagateJson = propagateResult.ToJson();
        var reasonIds = new[]
        {
            Guid.Parse(Assert.Single(ReasonIdPattern().Matches(nullJson)).Value),
            Guid.Parse(Assert.Single(ReasonIdPattern().Matches(propagateJson)).Value)
        };
        Assert.Equal(listener.Events.Select(e => e.ReasonId), reasonIds);
        new[] { NormalizeReasonId(nullJson), NormalizeReasonId(propagateJson) }
            .MatchInlineSnapshots(
            [
                """
                {
                  "errors": [
                    {
                      "message": "The current user is not authorized to access this resource.",
                      "path": [
                        "parent",
                        "protected"
                      ],
                      "extensions": {
                        "code": "UNAUTHORIZED_FIELD_OR_TYPE",
                        "reasonId": "00000000-0000-0000-0000-000000000000"
                      }
                    }
                  ],
                  "data": {
                    "parent": {
                      "protected": null,
                      "sibling": "visible"
                    },
                    "outside": "outside"
                  }
                }
                """,
                """
                {
                  "errors": [
                    {
                      "message": "The current user is not authorized to access this resource.",
                      "path": [
                        "parent",
                        "protected"
                      ],
                      "extensions": {
                        "code": "UNAUTHORIZED_FIELD_OR_TYPE",
                        "reasonId": "00000000-0000-0000-0000-000000000000"
                      }
                    }
                  ],
                  "data": {
                    "parent": null,
                    "outside": "outside"
                  }
                }
                """
            ]);
    }

    [Fact]
    public async Task ExecuteAsync_Should_MaterializePossibleConcreteAbortOnlyForMatchingRuntimeType()
    {
        // arrange
        const string types =
            """
            type Query { results: [SearchResult] }
            union SearchResult = Product | Viewer
            type Product @policy(names: "CanReadSecret", onDenied: ABORT) { id: ID! }
            type Viewer { name: String! }
            """;
        var viewerListener = new RequestPolicyDiagnosticListener();
        var viewerClient = new RecordingRequirementClient(
            """{"data":{"results":[{"__typename":"Viewer","name":"Ada"}]}}""");
        var viewerExecutor = await CreateAbstractTypePolicyExecutorAsync(
            new DenyPolicy(),
            types,
            """{"data":{"results":[{"__typename":"Viewer","name":"Ada"}]}}""",
            viewerListener,
            viewerClient);
        var listener = new RequestPolicyDiagnosticListener();
        var productClient = new RecordingRequirementClient(
            """{"data":{"results":[{"__typename":"Product","id":"1"}]}}""");
        var productExecutor = await CreateAbstractTypePolicyExecutorAsync(
            new DenyPolicy(),
            types,
            """{"data":{"results":[{"__typename":"Product","id":"1"}]}}""",
            listener,
            productClient);
        const string operation =
            """
            { results { ... on Product { id } ... on Viewer { name } } }
            """;

        // act
        await using var viewerResult = await viewerExecutor.ExecuteAsync(
            operation,
            TestContext.Current.CancellationToken);
        await using var productResult = await productExecutor.ExecuteAsync(
            operation,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal((1, 1), (viewerClient.Requests.Count, productClient.Requests.Count));
        viewerResult.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "results": [
                  {
                    "name": "Ada"
                  }
                ]
              }
            }
            """);
        Assert.Single(viewerListener.Events);
        var productJson = productResult.ToJson();
        var productReasonId = Guid.Parse(Assert.Single(ReasonIdPattern().Matches(productJson)).Value);
        Assert.Equal(Assert.Single(listener.Events).ReasonId, productReasonId);
        NormalizeReasonId(productJson).MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "extensions": {
                    "code": "UNAUTHORIZED_FIELD_OR_TYPE",
                    "reasonId": "00000000-0000-0000-0000-000000000000"
                  }
                }
              ],
              "data": null
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_KeepFetchOpen_When_OnlyAbstractOccurrenceIsLive()
    {
        // arrange
        var listener = new RequestPolicyDiagnosticListener();
        var client = new RecordingRequirementClient(
            """{"data":{"result":{"__typename":"Viewer","name":"Ada"}}}""");
        var executor = await CreateMixedOccurrenceExecutorAsync(client, listener);

        // act
        await using var result = await executor.ExecuteAsync(
            MixedOccurrenceOperation,
            new Dictionary<string, object?> { ["concrete"] = false, ["abstract"] = true },
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(
            (Requests: 1, Events: 1),
            (Requests: client.Requests.Count, Events: listener.Events.Count));
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "result": {
                  "name": "Ada"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_MaterializeAbort_When_OnlyAbstractProductOccurrenceIsLive()
    {
        // arrange
        var listener = new RequestPolicyDiagnosticListener();
        var client = new RecordingRequirementClient(
            """{"data":{"result":{"__typename":"Product","id":"1"}}}""");
        var executor = await CreateMixedOccurrenceExecutorAsync(client, listener);

        // act
        await using var result = await executor.ExecuteAsync(
            MixedOccurrenceOperation,
            new Dictionary<string, object?> { ["concrete"] = false, ["abstract"] = true },
            TestContext.Current.CancellationToken);

        // assert
        var json = result.ToJson();
        var reasonId = Guid.Parse(Assert.Single(ReasonIdPattern().Matches(json)).Value);
        Assert.Equal(
            (Requests: 1, Events: 1),
            (Requests: client.Requests.Count, Events: listener.Events.Count));
        Assert.Equal(listener.Events[0].ReasonId, reasonId);
        NormalizeReasonId(json).MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "extensions": {
                    "code": "UNAUTHORIZED_FIELD_OR_TYPE",
                    "reasonId": "00000000-0000-0000-0000-000000000000"
                  }
                }
              ],
              "data": null
            }
            """);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ExecuteAsync_Should_ShortCircuit_When_ConcreteOccurrenceIsLive(
        bool concrete,
        bool @abstract)
    {
        // arrange
        var listener = new RequestPolicyDiagnosticListener();
        var client = new RecordingRequirementClient(
            """{"data":{"product":{"id":"1"},"result":{"__typename":"Viewer","name":"Ada"}}}""");
        var executor = await CreateMixedOccurrenceExecutorAsync(client, listener);

        // act
        await using var result = await executor.ExecuteAsync(
            MixedOccurrenceOperation,
            new Dictionary<string, object?> { ["concrete"] = concrete, ["abstract"] = @abstract },
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(
            (Requests: 0, Events: 1),
            (Requests: client.Requests.Count, Events: listener.Events.Count));
        NormalizeReasonId(result.ToJson()).MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "extensions": {
                    "code": "UNAUTHORIZED_FIELD_OR_TYPE",
                    "reasonId": "00000000-0000-0000-0000-000000000000"
                  }
                }
              ],
              "data": null
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_MaterializeDeferredErrorWithoutDispatchingDeniedFetch()
    {
        // arrange
        var listener = new RequestPolicyDiagnosticListener();
        var client = new RecordingRequirementClient(
            """{"data":{"user":{"immediate":"initial"}}}""");
        var executor = await CreateDeferredPolicyExecutorAsync(client, listener);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ user { immediate ... @defer { secret } } }",
            TestContext.Current.CancellationToken);
        await using var stream = result.ExpectResponseStream();
        var responses = new List<string>();
        Guid? deferredReasonId = null;
        await foreach (var response in stream
            .ReadResultsAsync()
            .WithCancellation(TestContext.Current.CancellationToken))
        {
            var json = response.ToJson();
            var match = ReasonIdPattern().Match(json);
            if (match.Success)
            {
                deferredReasonId = Guid.Parse(match.Value);
            }
            responses.Add(ReasonIdPattern().IsMatch(json) ? NormalizeReasonId(json) : json);
        }

        // assert
        Assert.Equal(
            (Requests: 1, Events: 1),
            (Requests: client.Requests.Count, Events: listener.Events.Count));
        responses.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "user": {
                  "immediate": "initial"
                }
              },
              "pending": [
                {
                  "id": "0",
                  "path": [
                    "user"
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "incremental": [
                {
                  "id": "0",
                  "errors": [
                    {
                      "message": "The current user is not authorized to access this resource.",
                      "path": [
                        "user",
                        "secret"
                      ],
                      "extensions": {
                        "code": "UNAUTHORIZED_FIELD_OR_TYPE",
                        "reasonId": "00000000-0000-0000-0000-000000000000"
                      }
                    }
                  ],
                  "data": {
                    "secret": null
                  }
                }
              ],
              "completed": [
                {
                  "id": "0"
                }
              ],
              "hasNext": false
            }
            """
        ]);
        client.RequestDetails.MatchInlineSnapshot(
            """
            [
              "query Op_ef69efc6_1 {\n  user {\n    immediate\n  }\n}\nvariables: {}"
            ]
            """);
        Assert.Equal(listener.Events[0].ReasonId, deferredReasonId);
    }

    [Fact]
    public async Task ExecuteAsync_Should_NotMaterializeDeferredDenialForClientExcludedDefinition()
    {
        // arrange
        var listener = new RequestPolicyDiagnosticListener();
        var client = new RecordingRequirementClient(
            """{"data":{"user":{"immediate":"initial"}}}""");
        var executor = await CreateDeferredPolicyExecutorAsync(client, listener);

        // act
        await using var result = await executor.ExecuteAsync(
            "query($include: Boolean!) { user { immediate ... @defer { secret @include(if: $include) } } }",
            new Dictionary<string, object?> { ["include"] = false },
            TestContext.Current.CancellationToken);
        await using var stream = result.ExpectResponseStream();
        var responses = new List<string>();
        await foreach (var response in stream
            .ReadResultsAsync()
            .WithCancellation(TestContext.Current.CancellationToken))
        {
            responses.Add(response.ToJson());
        }

        // assert
        Assert.Equal(
            (Requests: 1, Denials: 0),
            (Requests: client.Requests.Count, Denials: listener.Events.Count));
        responses.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "user": {
                  "immediate": "initial"
                }
              },
              "pending": [
                {
                  "id": "0",
                  "path": [
                    "user"
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "completed": [
                {
                  "id": "0"
                }
              ],
              "hasNext": false
            }
            """
        ]);
    }

    [Fact]
    public async Task ExecuteAsync_Should_PreserveDeferredDeniedListTargetOccurrences()
    {
        // arrange
        var listener = new RequestPolicyDiagnosticListener();
        var client = new RecordingRequirementClient(
            """{"data":{"users":[{"immediate":"one"},null,{"immediate":"three"}]}}""");
        var executor = await CreateDeferredPolicyExecutorAsync(
            client,
            listener,
            """
            # name: a
            enum PolicyDenialBehavior { NULL ERROR ABORT }

            directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
              repeatable on OBJECT | FIELD_DEFINITION

            type Query {
              users: [User]
            }

            type User {
              immediate: String
              secret: String @policy(names: "CanReadSecret", onDenied: ERROR)
            }
            """);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ users { immediate ... @defer { secret } } }",
            TestContext.Current.CancellationToken);
        await using var stream = result.ExpectResponseStream();
        var responses = new List<string>();
        var reasonIds = new List<Guid>();
        await foreach (var response in stream
            .ReadResultsAsync()
            .WithCancellation(TestContext.Current.CancellationToken))
        {
            var json = response.ToJson();
            foreach (Match match in ReasonIdPattern().Matches(json))
            {
                reasonIds.Add(Guid.Parse(match.Value));
            }

            responses.Add(ReasonIdPattern().IsMatch(json) ? NormalizeReasonId(json) : json);
        }

        // assert
        responses.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "users": [
                  {
                    "immediate": "one"
                  },
                  null,
                  {
                    "immediate": "three"
                  }
                ]
              },
              "pending": [
                {
                  "id": "0",
                  "path": [
                    "users"
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "incremental": [
                {
                  "id": "0",
                  "errors": [
                    {
                      "message": "The current user is not authorized to access this resource.",
                      "path": [
                        "users",
                        0,
                        "secret"
                      ],
                      "extensions": {
                        "code": "UNAUTHORIZED_FIELD_OR_TYPE",
                        "reasonId": "00000000-0000-0000-0000-000000000000"
                      }
                    }
                  ],
                  "subPath": [
                    0
                  ],
                  "data": {
                    "secret": null
                  }
                },
                {
                  "id": "0",
                  "errors": [
                    {
                      "message": "The current user is not authorized to access this resource.",
                      "path": [
                        "users",
                        2,
                        "secret"
                      ],
                      "extensions": {
                        "code": "UNAUTHORIZED_FIELD_OR_TYPE",
                        "reasonId": "00000000-0000-0000-0000-000000000000"
                      }
                    }
                  ],
                  "subPath": [
                    2
                  ],
                  "data": {
                    "secret": null
                  }
                }
              ],
              "completed": [
                {
                  "id": "0"
                }
              ],
              "hasNext": false
            }
            """
        ]);
        Assert.Equal(
            (Requests: 1, Events: 1, ErrorIds: 2),
            (Requests: client.Requests.Count, Events: listener.Events.Count, ErrorIds: reasonIds.Count));
        Assert.All(reasonIds, reasonId => Assert.Equal(listener.Events[0].ReasonId, reasonId));
    }

    [Fact]
    public async Task ExecuteAsync_Should_RebaseDeferredDenialThroughAliasedNestedSiblings()
    {
        // arrange
        var listener = new RequestPolicyDiagnosticListener();
        var client = new RecordingRequirementClient(
            """{"data":{"before":"before","userAlias":{"immediate":"initial","childAlias":{"public":"visible"}}}}""");
        var executor = await CreateDeferredPolicyExecutorAsync(
            client,
            listener,
            """
            # name: a
            enum PolicyDenialBehavior { NULL ERROR ABORT }

            directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
              repeatable on OBJECT | FIELD_DEFINITION

            type Query {
              before: String
              user: User
            }

            type User {
              immediate: String
              child: Child
            }

            type Child {
              public: String
              secret: String @policy(names: "CanReadSecret", onDenied: ERROR)
            }
            """);

        // act
        await using var result = await executor.ExecuteAsync(
            """
            {
              before
              userAlias: user {
                immediate
                childAlias: child {
                  public
                  ... @defer {
                    secretAlias: secret
                  }
                }
              }
            }
            """,
            TestContext.Current.CancellationToken);
        await using var stream = result.ExpectResponseStream();
        var responses = new List<string>();
        Guid? reasonId = null;
        await foreach (var response in stream
            .ReadResultsAsync()
            .WithCancellation(TestContext.Current.CancellationToken))
        {
            var json = response.ToJson();
            var match = ReasonIdPattern().Match(json);
            if (match.Success)
            {
                reasonId = Guid.Parse(match.Value);
            }

            responses.Add(ReasonIdPattern().IsMatch(json) ? NormalizeReasonId(json) : json);
        }

        // assert
        responses.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "before": "before",
                "userAlias": {
                  "immediate": "initial",
                  "childAlias": {
                    "public": "visible"
                  }
                }
              },
              "pending": [
                {
                  "id": "0",
                  "path": [
                    "userAlias",
                    "childAlias"
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "incremental": [
                {
                  "id": "0",
                  "errors": [
                    {
                      "message": "The current user is not authorized to access this resource.",
                      "path": [
                        "userAlias",
                        "childAlias",
                        "secretAlias"
                      ],
                      "extensions": {
                        "code": "UNAUTHORIZED_FIELD_OR_TYPE",
                        "reasonId": "00000000-0000-0000-0000-000000000000"
                      }
                    }
                  ],
                  "data": {
                    "secretAlias": null
                  }
                }
              ],
              "completed": [
                {
                  "id": "0"
                }
              ],
              "hasNext": false
            }
            """
        ]);
        Assert.Equal(
            (Requests: 1, Events: 1),
            (Requests: client.Requests.Count, Events: listener.Events.Count));
        Assert.Equal(listener.Events[0].ReasonId, reasonId);
    }

    [Fact]
    public async Task ExecuteAsync_Should_PreserveRebasedDeferredPathsAcrossPoolReuse()
    {
        // arrange
        var listener = new RequestPolicyDiagnosticListener();
        var client = new RecordingRequirementClient(
            """{"data":{"before":"before","userAlias":{"immediate":"initial","childAlias":{"public":"visible"}}}}""");
        var executor = await CreateDeferredPolicyExecutorAsync(
            client,
            listener,
            """
            # name: a
            enum PolicyDenialBehavior { NULL ERROR ABORT }

            directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
              repeatable on OBJECT | FIELD_DEFINITION

            type Query {
              before: String
              user: User
            }

            type User {
              immediate: String
              child: Child
            }

            type Child {
              public: String
              secret: String @policy(names: "CanReadSecret", onDenied: ERROR)
            }
            """);
        var incrementalResponses = new List<string>();

        // act
        for (var i = 0; i < 32; i++)
        {
            await using var result = await executor.ExecuteAsync(
                """
                {
                  before
                  userAlias: user {
                    immediate
                    childAlias: child {
                      public
                      ... @defer {
                        secretAlias: secret
                      }
                    }
                  }
                }
                """,
                TestContext.Current.CancellationToken);
            await using var stream = result.ExpectResponseStream();
            await foreach (var response in stream
                .ReadResultsAsync()
                .WithCancellation(TestContext.Current.CancellationToken))
            {
                var json = response.ToJson();
                if (json.Contains("incremental", StringComparison.Ordinal))
                {
                    incrementalResponses.Add(NormalizeReasonId(json));
                }
            }
        }

        // assert
        Assert.Equal(
            (Requests: 32, Events: 32, Incremental: 32),
            (Requests: client.Requests.Count,
                Events: listener.Events.Count,
                Incremental: incrementalResponses.Count));
        Assert.Single(incrementalResponses.Distinct()).MatchInlineSnapshot(
            """
            {
              "incremental": [
                {
                  "id": "0",
                  "errors": [
                    {
                      "message": "The current user is not authorized to access this resource.",
                      "path": [
                        "userAlias",
                        "childAlias",
                        "secretAlias"
                      ],
                      "extensions": {
                        "code": "UNAUTHORIZED_FIELD_OR_TYPE",
                        "reasonId": "00000000-0000-0000-0000-000000000000"
                      }
                    }
                  ],
                  "data": {
                    "secretAlias": null
                  }
                }
              ],
              "completed": [
                {
                  "id": "0"
                }
              ],
              "hasNext": false
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_RebaseAllowedDeferredLookupRequirementsAcrossPlans()
    {
        // arrange
        var sourceClient = new RecordingRequirementClient(
            """{"data":{"before":"before","topProducts":[{"id":"1"}]}}""");
        var downstreamClient = new RecordingLookupClient();
        var executor = await CreateDeferredLookupExecutorAsync(
            sourceClient,
            downstreamClient,
            new AllowPolicy());

        // act
        await using var result = await executor.ExecuteAsync(
            "{ before topProducts { id ... @defer { price } } }",
            TestContext.Current.CancellationToken);
        await using var stream = result.ExpectResponseStream();
        var responses = new List<string>();
        await foreach (var response in stream
            .ReadResultsAsync()
            .WithCancellation(TestContext.Current.CancellationToken))
        {
            responses.Add(response.ToJson());
        }

        // assert
        Assert.Equal((Source: 1, Downstream: 1),
            (Source: sourceClient.Requests.Count, Downstream: downstreamClient.ExecutionCount));
        responses.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "before": "before",
                "topProducts": [
                  {
                    "id": "1"
                  }
                ]
              },
              "pending": [
                {
                  "id": "0",
                  "path": [
                    "topProducts"
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "incremental": [
                {
                  "id": "0",
                  "subPath": [
                    0
                  ],
                  "data": {
                    "price": 9.99
                  }
                }
              ],
              "completed": [
                {
                  "id": "0"
                }
              ],
              "hasNext": false
            }
            """
        ]);
        downstreamClient.Requests.MatchInlineSnapshot(
            """
            query Op_defer_1($__fusion_1_id: ID!) {
              productById(id: $__fusion_1_id) {
                price
              }
            }
            variables: {"__fusion_1_id":"1"}
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_SeedDeniedDeferredLookupWithoutDispatch()
    {
        // arrange
        var listener = new RequestPolicyDiagnosticListener();
        var sourceClient = new RecordingRequirementClient(
            """{"data":{"before":"before","topProducts":[{"id":"1"}]}}""");
        var downstreamClient = new RecordingLookupClient();
        var executor = await CreateDeferredLookupExecutorAsync(
            sourceClient,
            downstreamClient,
            new DenyPolicy(),
            listener);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ before topProducts { id ... @defer { price } } }",
            TestContext.Current.CancellationToken);
        await using var stream = result.ExpectResponseStream();
        var responses = new List<string>();
        await foreach (var response in stream
            .ReadResultsAsync()
            .WithCancellation(TestContext.Current.CancellationToken))
        {
            var json = response.ToJson();
            responses.Add(ReasonIdPattern().IsMatch(json) ? NormalizeReasonId(json) : json);
        }

        // assert
        Assert.Equal(
            (Source: 1, Downstream: 0, Events: 1),
            (Source: sourceClient.Requests.Count,
                Downstream: downstreamClient.ExecutionCount,
                Events: listener.Events.Count));
        responses.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "before": "before",
                "topProducts": [
                  {
                    "id": "1"
                  }
                ]
              },
              "pending": [
                {
                  "id": "0",
                  "path": [
                    "topProducts"
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "incremental": [
                {
                  "id": "0",
                  "errors": [
                    {
                      "message": "The current user is not authorized to access this resource.",
                      "path": [
                        "topProducts",
                        0,
                        "price"
                      ],
                      "extensions": {
                        "code": "UNAUTHORIZED_FIELD_OR_TYPE",
                        "reasonId": "00000000-0000-0000-0000-000000000000"
                      }
                    }
                  ],
                  "subPath": [
                    0
                  ],
                  "data": {
                    "price": null
                  }
                }
              ],
              "completed": [
                {
                  "id": "0"
                }
              ],
              "hasNext": false
            }
            """
        ]);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReusePossibleConcreteReasonIdAcrossListIndices()
    {
        // arrange
        var listener = new RequestPolicyDiagnosticListener();
        var executor = await CreateAbstractTypePolicyExecutorAsync(
            new DenyPolicy(),
            """
            type Query { results: [SearchResult] }
            union SearchResult = Product | Viewer
            type Product @policy(names: "CanReadSecret", onDenied: ERROR) { id: ID! }
            type Viewer { name: String! }
            """,
            """{"data":{"results":[{"__typename":"Product","id":"1"},{"__typename":"Product","id":"2"}]}}""",
            listener);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ results { ... on Product { id } } }",
            TestContext.Current.CancellationToken);

        // assert
        var json = result.ToJson();
        var reasonIds = ReasonIdPattern().Matches(json).Select(match => Guid.Parse(match.Value)).ToArray();
        Assert.Equal(2, reasonIds.Length);
        Assert.All(reasonIds, reasonId => Assert.Equal(Assert.Single(listener.Events).ReasonId, reasonId));
        NormalizeReasonId(json).MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "path": [
                    "results",
                    0
                  ],
                  "extensions": {
                    "code": "UNAUTHORIZED_FIELD_OR_TYPE",
                    "reasonId": "00000000-0000-0000-0000-000000000000"
                  }
                },
                {
                  "message": "The current user is not authorized to access this resource.",
                  "path": [
                    "results",
                    1
                  ],
                  "extensions": {
                    "code": "UNAUTHORIZED_FIELD_OR_TYPE",
                    "reasonId": "00000000-0000-0000-0000-000000000000"
                  }
                }
              ],
              "data": {
                "results": [
                  null,
                  null
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReusePossibleConcreteReasonIdAcrossNestedObjects()
    {
        // arrange
        var listener = new RequestPolicyDiagnosticListener();
        var executor = await CreateAbstractTypePolicyExecutorAsync(
            new DenyPolicy(),
            """
            type Query { containers: [Container] }
            type Container { result: SearchResult }
            union SearchResult = Product | Viewer
            type Product @policy(names: "CanReadSecret", onDenied: ERROR) { id: ID! }
            type Viewer { name: String! }
            """,
            """{"data":{"containers":[{"result":{"__typename":"Product","id":"1"}},{"result":{"__typename":"Product","id":"2"}}]}}""",
            listener);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ containers { result { ... on Product { id } } } }",
            TestContext.Current.CancellationToken);

        // assert
        var reasonIds = ReasonIdPattern().Matches(result.ToJson())
            .Select(match => Guid.Parse(match.Value))
            .Distinct()
            .ToArray();
        Assert.Equal([Assert.Single(listener.Events).ReasonId], reasonIds);
        NormalizeReasonId(result.ToJson()).MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "path": [
                    "containers",
                    0,
                    "result"
                  ],
                  "extensions": {
                    "code": "UNAUTHORIZED_FIELD_OR_TYPE",
                    "reasonId": "00000000-0000-0000-0000-000000000000"
                  }
                },
                {
                  "message": "The current user is not authorized to access this resource.",
                  "path": [
                    "containers",
                    1,
                    "result"
                  ],
                  "extensions": {
                    "code": "UNAUTHORIZED_FIELD_OR_TYPE",
                    "reasonId": "00000000-0000-0000-0000-000000000000"
                  }
                }
              ],
              "data": {
                "containers": [
                  {
                    "result": null
                  },
                  {
                    "result": null
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_PinRequestPolicySlotAcrossVariableBatch()
    {
        // arrange
        var requestPolicy = new CountingAllowPolicy("CanReadProduct");
        var listener = new RequestPolicyDiagnosticListener();
        var executor = await CreateZeroSlotVariableBatchExecutorAsync(requestPolicy, listener);
        const string operation =
            """
            query($includeName: Boolean!) {
              product {
                id
                name @include(if: $includeName)
              }
            }
            """;
        var plan = PlanOperation(Assert.IsType<FusionSchemaDefinition>(executor.Schema), operation);
        using var variableValues = JsonDocument.Parse(
            """[{"includeName":false},{"includeName":true}]""");
        var request = VariableBatchRequest.FromSourceText(operation, variableValues);

        // act
        await using var result = await executor.ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Single(plan.PolicySlots);
        Assert.Equal((1, 1), (requestPolicy.EvaluationCount, listener.EvaluationCount));
        var batch = Assert.IsType<OperationResultBatch>(result);
        batch.Results.Select(current => current.ToJson()).ToArray().MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "product": {
                  "id": "1"
                }
              }
            }
            """,
            """
            {
              "data": {
                "product": {
                  "id": "1",
                  "name": "Ada"
                }
              }
            }
            """
        ]);
    }

    [Fact]
    public async Task ReevaluatePolicySlotsAsync_Should_ResetDecisionsAndDenialsAcrossReductions()
    {
        // arrange
        var initialPolicy = new MutableCountingPolicy { Deny = true };
        var listener = new RequestPolicyDiagnosticListener();
        IServiceProvider? requestServices = null;
        var executor = await CreateExecutorAsync(
            PolicyDenialBehavior.Null,
            initialPolicy,
            diagnosticListener: listener,
            captureRequestServices: services => requestServices = services);
        var schema = Assert.IsType<FusionSchemaDefinition>(executor.Schema);
        var provider = Assert.IsType<TestPolicyProvider>(
            schema.Services.GetRequiredService<IPolicyProvider>());
        var plan = PlanOperation(schema, "{ secret }");
        var requestContextPool = schema.Services.GetRequiredService<ObjectPool<PooledRequestContext>>();
        var requestContext = requestContextPool.Get();
        using var executionCts = new CancellationTokenSource();
        var context = new OperationPlanContext(
            schema.Services.GetRequiredService<INodeIdParser>(),
            schema.Services.GetRequiredService<IFusionExecutionDiagnosticEvents>(),
            schema.Services.GetRequiredService<IErrorHandler>());

        try
        {
            requestContext.Initialize(
                schema,
                executor.Version,
                OperationRequestBuilder.New().SetDocument("{ secret }").Build(),
                requestIndex: 0,
                requestServices: requestServices!,
                requestAborted: CancellationToken.None);
            requestContext.SetPolicySnapshot(schema.Policies.GetSnapshot());
            var requestState = PolicyRequestState.GetOrCreate(
                requestContext,
                plan,
                schema.Services.GetRequiredService<IFusionExecutionDiagnosticEvents>());
            PolicySlotEvaluationResult first;
            using (schema.Services
                .GetRequiredService<IFusionExecutionDiagnosticEvents>()
                .EvaluateRequestPolicies(requestContext))
            {
                first = await requestState.EvaluateSlotsAsync(
                    plan,
                    VariableValueCollection.Empty,
                    TestContext.Current.CancellationToken);
            }
            context.Initialize(
                requestContext,
                new PolicyVariableValueCollection(
                    VariableValueCollection.Empty,
                    plan.PolicySlots.Length,
                    first.LiveFlags,
                    first.DenyFlags,
                    first.FetchGateDenyFlags),
                plan,
                executionCts,
                new MemoryArena());
            var replacementPolicy = new CountingAllowPolicy("CanReadSecret");
            provider.Emit(replacementPolicy);
            initialPolicy.Deny = false;

            // act
            var second = await context.ReevaluatePolicySlotsAsync(
                TestContext.Current.CancellationToken);
            initialPolicy.Deny = true;
            var third = await context.ReevaluatePolicySlotsAsync(
                TestContext.Current.CancellationToken);

            // assert
            ($"initial={initialPolicy.EvaluationCount}; replacement={replacementPolicy.EvaluationCount}; "
                + $"first={first.LiveFlags}/{first.DenyFlags}; "
                + $"second={second.LiveFlags}/{second.DenyFlags}; "
                + $"third={third.LiveFlags}/{third.DenyFlags}; "
                + $"listener={listener.ScopeCount}/{listener.EvaluationCount}; "
                + $"denials={listener.Denials.Count}; "
                + $"reasonIds={listener.Events.Select(e => e.ReasonId).Distinct().Count()}")
                .MatchInlineSnapshot(
                    """
                    initial=3; replacement=0; first=1/1; second=1/0; third=1/1; listener=3/3; denials=2; reasonIds=2
                    """);
        }
        finally
        {
            await context.DisposeAsync();
            context.Destroy();
            requestContextPool.Return(requestContext);
        }
    }

    [Fact]
    public async Task ReevaluatePolicySlotsAsync_Should_ClearFallbackMemo_When_PlanHasNoSlots()
    {
        // arrange
        var initialPolicy = new MutableRequirementPolicy { Deny = true };
        IServiceProvider? requestServices = null;
        var executor = await CreateExecutorAsync(
            PolicyDenialBehavior.Null,
            initialPolicy,
            captureRequestServices: services => requestServices = services);
        var schema = Assert.IsType<FusionSchemaDefinition>(executor.Schema);
        var provider = Assert.IsType<TestPolicyProvider>(
            schema.Services.GetRequiredService<IPolicyProvider>());
        var plan = PlanOperation(schema, "{ secret }");
        var requestContextPool = schema.Services.GetRequiredService<ObjectPool<PooledRequestContext>>();
        var requestContext = requestContextPool.Get();
        using var executionCts = new CancellationTokenSource();
        var context = new OperationPlanContext(
            schema.Services.GetRequiredService<INodeIdParser>(),
            schema.Services.GetRequiredService<IFusionExecutionDiagnosticEvents>(),
            schema.Services.GetRequiredService<IErrorHandler>());

        try
        {
            requestContext.Initialize(
                schema,
                executor.Version,
                OperationRequestBuilder.New().SetDocument("{ secret }").Build(),
                requestIndex: 0,
                requestServices: requestServices!,
                requestAborted: CancellationToken.None);
            context.Initialize(
                requestContext,
                VariableValueCollection.Empty,
                plan,
                executionCts,
                new MemoryArena());
            var first = await context.EvaluateRequestPolicyAsync(
                "CanReadSecret",
                new ClaimsPrincipal(),
                TestContext.Current.CancellationToken);
            provider.Emit(new CountingAllowPolicy("CanReadSecret"));
            initialPolicy.Deny = false;

            // act
            var secondRound = await context.ReevaluatePolicySlotsAsync(
                TestContext.Current.CancellationToken);
            var second = await context.EvaluateRequestPolicyAsync(
                "CanReadSecret",
                new ClaimsPrincipal(),
                TestContext.Current.CancellationToken);
            initialPolicy.Deny = true;
            var thirdRound = await context.ReevaluatePolicySlotsAsync(
                TestContext.Current.CancellationToken);
            var third = await context.EvaluateRequestPolicyAsync(
                "CanReadSecret",
                new ClaimsPrincipal(),
                TestContext.Current.CancellationToken);

            // assert
            ($"slots={plan.PolicySlots.Length}; nodes={plan.AllNodes.OfType<PolicyExecutionNode>().Count()}; "
                + $"evaluations={initialPolicy.EvaluationCount}; "
                + $"decisions={first.IsDenied}/{second.IsDenied}/{third.IsDenied}; "
                + $"rounds={secondRound.LiveFlags}/{secondRound.DenyFlags},"
                + $"{thirdRound.LiveFlags}/{thirdRound.DenyFlags}; "
                + $"contextDenyFlags={context.PolicyDenyFlags}")
                .MatchInlineSnapshot(
                    """
                    slots=0; nodes=1; evaluations=3; decisions=True/False/True; rounds=0/0,0/0; contextDenyFlags=0
                    """);
        }
        finally
        {
            await context.DisposeAsync();
            context.Destroy();
            requestContextPool.Return(requestContext);
        }
    }

    [Fact]
    public async Task EvaluateSlotsAsync_Should_ReuseDenseReductionBuffersAcrossManySlotsAndItems()
    {
        // arrange
        const int slotCount = 32;
        const int itemCount = 64;
        // Warmed reductions allocate only the deliberately cleared per-policy decision memos.
        const long AllowReductionAllocationBudget = 3_000_000;
        const long DenyReductionAllocationBudget = 5_000_000;
        var fields = new StringBuilder();
        var operation = new StringBuilder("query {");
        var policies = new IPolicy[slotCount];
        for (var i = 0; i < slotCount; i++)
        {
            fields.Append("field");
            fields.Append(i);
            fields.Append(": String @policy(names: \"Policy");
            fields.Append(i);
            fields.AppendLine("\")");
            operation.Append(" field");
            operation.Append(i);
            policies[i] = new SwitchableCountingPolicy($"Policy{i}");
        }
        operation.Append(" }");

        var services = new ServiceCollection();
        services.AddHttpClient();
        var builder = services
            .AddGraphQLGateway()
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    $$"""
                    # name: a
                    enum PolicyDenialBehavior { NULL ERROR ABORT }

                    directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                      repeatable on OBJECT | FIELD_DEFINITION

                    type Query {
                    {{fields}}
                    }
                    """));
        ConfigurePolicies(builder, new TestPolicyProvider(policies));
        builder.Services.AddSingleton<ISourceSchemaClientFactory>(
            new TestClientFactory(("a", new StaticResultClient())));
        FusionSetupUtilities.Configure(
            builder,
            setup => setup.ClientConfigurationModifiers.Add(
                _ => new TestClientConfiguration("a")));
        var executor = await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
        var schema = Assert.IsType<FusionSchemaDefinition>(executor.Schema);
        var plan = PlanOperation(schema, operation.ToString());
        var requestContextPool = schema.Services.GetRequiredService<ObjectPool<PooledRequestContext>>();
        var requestContext = requestContextPool.Get();

        try
        {
            requestContext.Initialize(
                schema,
                executor.Version,
                OperationRequestBuilder.New().SetDocument(operation.ToString()).Build(),
                requestIndex: 0,
                requestServices: schema.Services,
                requestAborted: CancellationToken.None);
            requestContext.SetPolicySnapshot(schema.Policies.GetSnapshot());
            var requestState = PolicyRequestState.GetOrCreate(
                requestContext,
                plan,
                schema.Services.GetRequiredService<IFusionExecutionDiagnosticEvents>());

            // Warm JIT, request state, and the retained dense buffers before measuring.
            await requestState.EvaluateSlotsAsync(
                plan,
                VariableValueCollection.Empty,
                TestContext.Current.CancellationToken);

            // act
            var beforeAllow = GC.GetAllocatedBytesForCurrentThread();
            PolicySlotEvaluationResult allow = default;
            for (var i = 0; i < itemCount; i++)
            {
                requestState.ClearDecisions();
                allow = await requestState.EvaluateSlotsAsync(
                    plan,
                    VariableValueCollection.Empty,
                    TestContext.Current.CancellationToken);
            }
            var allowAllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - beforeAllow;

            foreach (var policy in policies.Cast<SwitchableCountingPolicy>())
            {
                policy.Deny = true;
            }

            requestState.ClearDecisions();
            await requestState.EvaluateSlotsAsync(
                plan,
                VariableValueCollection.Empty,
                TestContext.Current.CancellationToken);

            var beforeDeny = GC.GetAllocatedBytesForCurrentThread();
            PolicySlotEvaluationResult deny = default;
            for (var i = 0; i < itemCount; i++)
            {
                requestState.ClearDecisions();
                deny = await requestState.EvaluateSlotsAsync(
                    plan,
                    VariableValueCollection.Empty,
                    TestContext.Current.CancellationToken);
            }
            var denyAllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - beforeDeny;

            // assert
            Assert.True(
                allowAllocatedBytes <= AllowReductionAllocationBudget,
                $"allow allocated {allowAllocatedBytes} bytes");
            Assert.True(
                denyAllocatedBytes <= DenyReductionAllocationBudget,
                $"deny allocated {denyAllocatedBytes} bytes");
            ($"slots={plan.PolicySlots.Length}; expressions={plan.PolicyExpressions.Length}; "
                + $"capacity={requestState.ReductionBufferCapacity}; "
                + $"allowDeny={allow.DenyFlags}/{deny.DenyFlags}; "
                + $"budgets={allowAllocatedBytes <= AllowReductionAllocationBudget}/"
                + $"{denyAllocatedBytes <= DenyReductionAllocationBudget}; "
                + $"evaluations={policies.Cast<SwitchableCountingPolicy>().Sum(policy => policy.EvaluationCount)}")
                .MatchInlineSnapshot(
                    """
                    slots=32; expressions=32; capacity=32; allowDeny=0/4294967295; budgets=True/True; evaluations=4160
                    """);
        }
        finally
        {
            requestContextPool.Return(requestContext);
        }
    }

    private static async Task<IRequestExecutor> CreateTwoAbortFieldExecutorAsync(
        FusionExecutionDiagnosticEventListener diagnosticListener)
    {
        var services = new ServiceCollection();
        services.AddHttpClient();

        var builder = services
            .AddGraphQLGateway()
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    # name: a
                    enum PolicyDenialBehavior { NULL ERROR ABORT }

                    directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                      repeatable on OBJECT | FIELD_DEFINITION

                    type Query {
                      first: String @policy(names: "CanReadSecret", onDenied: ABORT)
                      second: String @policy(names: "CanReadSecret", onDenied: ABORT)
                    }
                    """));

        ConfigurePolicies(builder, new TestPolicyProvider(new CountingDenyPolicy()));
        builder.AddDiagnosticEventListener(_ => diagnosticListener);
        builder.Services.AddSingleton<ISourceSchemaClientFactory>(
            new TestClientFactory((
                "a",
                new RecordingRequirementClient(
                    """{"data":{"first":"one","second":"two"}}"""))));
        FusionSetupUtilities.Configure(
            builder,
            setup => setup.ClientConfigurationModifiers.Add(
                _ => new TestClientConfiguration("a")));

        return await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<IRequestExecutor> CreateErrorHandlingExecutorAsync(
        FusionExecutionDiagnosticEventListener listener)
    {
        var services = new ServiceCollection();
        services.AddHttpClient();

        var builder = services
            .AddGraphQLGateway()
            .ModifyRequestOptions(options => options.AllowErrorHandlingModeOverride = true)
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    # name: a
                    enum PolicyDenialBehavior { NULL ERROR ABORT }

                    directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                      repeatable on OBJECT | FIELD_DEFINITION

                    type Query {
                      parent: Parent
                      outside: String!
                    }

                    type Parent {
                      protected: String! @policy(names: "CanReadSecret", onDenied: ERROR)
                      sibling: String!
                    }
                    """));

        ConfigurePolicies(builder, new TestPolicyProvider(new CountingDenyPolicy()));
        builder.AddDiagnosticEventListener(_ => listener);
        builder.Services.AddSingleton<ISourceSchemaClientFactory>(
            new TestClientFactory((
                "a",
                new RecordingRequirementClient(
                    """{"data":{"parent":{"protected":"classified","sibling":"visible"},"outside":"outside"}}"""))));
        FusionSetupUtilities.Configure(
            builder,
            setup => setup.ClientConfigurationModifiers.Add(
                _ => new TestClientConfiguration("a")));

        return await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
    }

    private const string MixedOccurrenceOperation =
        "query($concrete: Boolean!, $abstract: Boolean!) { "
        + "product @include(if: $concrete) { id } "
        + "result @include(if: $abstract) { ... on Product { id } ... on Viewer { name } } }";

    private static Task<IRequestExecutor> CreateMixedOccurrenceExecutorAsync(
        RecordingRequirementClient client,
        FusionExecutionDiagnosticEventListener listener)
        => CreateAbstractTypePolicyExecutorAsync(
            new DenyPolicy(),
            """
            type Query {
              product: Product
              result: SearchResult
            }

            union SearchResult = Product | Viewer

            type Product @policy(names: "CanReadSecret", onDenied: ABORT) {
              id: ID!
            }

            type Viewer {
              name: String!
            }
            """,
            """{"data":{}}""",
            listener,
            client);

    private static async Task<IRequestExecutor> CreateDeferredPolicyExecutorAsync(
        RecordingRequirementClient client,
        FusionExecutionDiagnosticEventListener listener)
        => await CreateDeferredPolicyExecutorAsync(
            client,
            listener,
            """
            # name: a
            enum PolicyDenialBehavior { NULL ERROR ABORT }

            directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
              repeatable on OBJECT | FIELD_DEFINITION

            type Query {
              user: User
            }

            type User {
              immediate: String
              secret: String @policy(names: "CanReadSecret", onDenied: ERROR)
            }
            """);

    private static async Task<IRequestExecutor> CreateDeferredPolicyExecutorAsync(
        RecordingRequirementClient client,
        FusionExecutionDiagnosticEventListener listener,
        string schemaSource)
    {
        var services = new ServiceCollection();
        services.AddHttpClient();

        var builder = services
            .AddGraphQLGateway()
            .ModifyOptions(options => options.EnableDefer = true)
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(schemaSource));

        ConfigurePolicies(builder, new TestPolicyProvider(new CountingDenyPolicy()));
        builder.AddDiagnosticEventListener(_ => listener);
        builder.Services.AddSingleton<ISourceSchemaClientFactory>(
            new TestClientFactory(("a", client)));
        FusionSetupUtilities.Configure(
            builder,
            setup => setup.ClientConfigurationModifiers.Add(
                _ => new TestClientConfiguration("a")));

        return await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<IRequestExecutor> CreateDeferredLookupExecutorAsync(
        RecordingRequirementClient sourceClient,
        RecordingLookupClient downstreamClient,
        IPolicy policy,
        FusionExecutionDiagnosticEventListener? listener = null)
    {
        var services = new ServiceCollection();
        services.AddHttpClient();

        var builder = services
            .AddGraphQLGateway()
            .ModifyOptions(options => options.EnableDefer = true)
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    # name: a
                    type Query {
                      before: String
                      topProducts: [Product!]
                    }

                    type Product @key(fields: "id") {
                      id: ID!
                    }
                    """,
                    """
                    # name: b
                    enum PolicyDenialBehavior { NULL ERROR ABORT }

                    directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                      repeatable on OBJECT | FIELD_DEFINITION

                    type Query {
                      productById(id: ID!): Product @lookup @internal
                    }

                    type Product {
                      id: ID!
                      price: Float @policy(names: "CanReadSecret", onDenied: ERROR)
                    }
                    """));

        ConfigurePolicies(builder, new TestPolicyProvider(policy));
        if (listener is not null)
        {
            builder.AddDiagnosticEventListener(_ => listener);
        }

        builder.Services.AddSingleton<ISourceSchemaClientFactory>(
            new TestClientFactory(("a", sourceClient), ("b", downstreamClient)));
        FusionSetupUtilities.Configure(
            builder,
            setup =>
            {
                setup.ClientConfigurationModifiers.Add(_ => new TestClientConfiguration("a"));
                setup.ClientConfigurationModifiers.Add(_ => new TestClientConfiguration("b"));
            });

        return await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<IRequestExecutor> CreateOrderedApplicationExecutorAsync(
        string directives,
        FusionExecutionDiagnosticEventListener diagnosticListener,
        params IPolicy[] policies)
    {
        var services = new ServiceCollection();
        services.AddHttpClient();

        var builder = services
            .AddGraphQLGateway()
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    $$"""
                    # name: a
                    enum PolicyDenialBehavior { NULL ERROR ABORT }

                    directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                      repeatable on OBJECT | FIELD_DEFINITION

                    type Query {
                      secret: String {{directives}}
                    }
                    """));

        ConfigurePolicies(builder, new TestPolicyProvider(policies));
        builder.AddDiagnosticEventListener(_ => diagnosticListener);
        builder.Services.AddSingleton<ISourceSchemaClientFactory>(
            new TestClientFactory(("a", new StaticResultClient())));
        FusionSetupUtilities.Configure(
            builder,
            setup => setup.ClientConfigurationModifiers.Add(
                _ => new TestClientConfiguration("a")));

        return await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<IRequestExecutor> CreateZeroSlotVariableBatchExecutorAsync(
        IPolicy requestPolicy,
        FusionExecutionDiagnosticEventListener diagnosticListener)
    {
        var services = new ServiceCollection();
        services.AddHttpClient();

        var builder = services
            .AddGraphQLGateway()
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    # name: a
                    enum PolicyDenialBehavior { NULL ERROR ABORT }

                    directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                      repeatable on OBJECT | FIELD_DEFINITION

                    type Query {
                      product: Product
                    }

                    type Product @policy(names: "CanReadProduct") {
                      id: ID!
                      name: String @policy(names: "CanReadName")
                      role: String
                    }
                    """));

        ConfigurePolicies(
            builder,
            new TestPolicyProvider(
                requestPolicy,
                new NamedRequirementPolicy("CanReadName", deny: false)));
        builder.AddDiagnosticEventListener(_ => diagnosticListener);
        builder.Services.AddSingleton<ISourceSchemaClientFactory>(
            new TestClientFactory((
                "a",
                new RecordingRequirementClient(
                    """{"data":{"product":{"id":"1","name":"Ada","role":"admin"}}}"""))));
        FusionSetupUtilities.Configure(
            builder,
            setup => setup.ClientConfigurationModifiers.Add(
                _ => new TestClientConfiguration("a")));

        return await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
    }

    private sealed class RequestPolicyDiagnosticListener : FusionExecutionDiagnosticEventListener
    {
        public int ScopeCount { get; private set; }

        public int EvaluationCount { get; private set; }

        public List<string> Denials { get; } = [];

        public List<DeniedEvent> Events { get; } = [];

        public override IDisposable EvaluateRequestPolicies(RequestContext context)
        {
            ScopeCount++;
            return EmptyScope;
        }

        public override void PolicyEvaluated(
            RequestContext context,
            string policyName,
            bool denied,
            TimeSpan duration)
        {
            EvaluationCount++;
        }

        public override void PolicySlotDenied(
            RequestContext context,
            string slotVariableName,
            string policyExpression,
            string typeName,
            string? fieldName,
            PolicyDenialBehavior behavior,
            string? reason,
            Guid reasonId,
            string? subjectId)
        {
            Events.Add(new DeniedEvent(reasonId, subjectId));
            Denials.Add(
                $"{slotVariableName}|{policyExpression}|{typeName}.{fieldName}|{behavior}|{reason}");
        }
    }

    private readonly record struct DeniedEvent(Guid ReasonId, string? SubjectId);

    private sealed class MutableCountingPolicy : IPolicy
    {
        private int _evaluationCount;

        public string Name => "CanReadSecret";

        public PolicyRequirements Requirements => PolicyRequirements.Empty;

        public bool Deny { get; set; }

        public int EvaluationCount => Volatile.Read(ref _evaluationCount);

        public ValueTask EvaluateAsync(
            IPolicyContext context,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _evaluationCount);
            if (Deny)
            {
                context.Deny(0, "denied by mutable policy");
            }

            return ValueTask.CompletedTask;
        }
    }

    private sealed class MutableRequirementPolicy : IPolicy
    {
        private static readonly PolicyRequirements s_requirements =
            new() { Resource = Utf8GraphQLParser.Syntax.ParseSelectionSet("{ role }") };
        private int _evaluationCount;

        public string Name => "CanReadSecret";

        public PolicyRequirements Requirements => s_requirements;

        public bool Deny { get; set; }

        public int EvaluationCount => Volatile.Read(ref _evaluationCount);

        public ValueTask EvaluateAsync(
            IPolicyContext context,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _evaluationCount);
            if (Deny)
            {
                context.Deny(0, "denied by mutable requirement policy");
            }

            return ValueTask.CompletedTask;
        }
    }

    private sealed class PrincipalRequirementPolicy : IPolicy
    {
        private static readonly PolicyRequirements s_requirements =
            new() { Resource = Utf8GraphQLParser.Syntax.ParseSelectionSet("{ role }") };

        public string Name => "CanReadSecret";

        public PolicyRequirements Requirements => s_requirements;

        public string? SubjectId { get; private set; }

        public ValueTask EvaluateAsync(
            IPolicyContext context,
            CancellationToken cancellationToken)
        {
            SubjectId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? context.User.FindFirst("sub")?.Value;
            return ValueTask.CompletedTask;
        }
    }
}
