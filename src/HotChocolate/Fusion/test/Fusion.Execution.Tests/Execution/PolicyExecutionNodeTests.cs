using System.Buffers;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution;

public sealed class PolicyExecutionNodeTests : FusionTestBase
{
    [Fact]
    public async Task ExecuteAsync_Should_CoFetchAndHideValidPolicyRequirement()
    {
        // arrange
        var policy = new RoleRequirementPolicy();
        var client = new RecordingRequirementClient();
        var listener = new PlanningErrorListener();
        var executor = await CreateRequirementExecutorAsync(policy, client, listener);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ secret }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Null(listener.Error);
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "secret": "classified"
              }
            }
            """);
        Assert.Collection(
            client.Requests,
            request =>
            {
                Assert.Contains("secret", request, StringComparison.Ordinal);
                Assert.Contains("role", request, StringComparison.Ordinal);
            });
        Assert.Equal((Evaluated: true, Role: "admin"), (policy.Evaluated, policy.Role));
    }

    [Fact]
    public async Task ExecuteAsync_Should_FetchCrossSourceRequirementBeforePolicy()
    {
        // arrange
        var policy = new RoleRequirementPolicy();
        var secretClient = new RecordingRequirementClient(
            """{"data":{"secret":"classified"}}""");
        var roleClient = new RecordingRequirementClient(
            """{"data":{"role":"admin"}}""");
        var listener = new PlanningErrorListener();
        var executor = await CreateCrossSourceRequirementExecutorAsync(
            policy,
            secretClient,
            roleClient,
            listener);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ secret }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Null(listener.Error);
        Assert.Collection(
            secretClient.Requests,
            request => Assert.Contains("secret", request, StringComparison.Ordinal));
        Assert.Collection(
            roleClient.Requests,
            request => Assert.Contains("role", request, StringComparison.Ordinal));
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "secret": "classified"
              }
            }
            """);
        Assert.Equal((Evaluated: true, Role: "admin"), (policy.Evaluated, policy.Role));
    }

    [Fact]
    public async Task ExecuteAsync_Should_FailPlanning_When_PolicyRequiresUnknownField()
    {
        // arrange
        var policy = new UnknownRequirementPolicy();
        var client = new RecordingRequirementClient();
        var listener = new PlanningErrorListener();
        var executor = await CreateRequirementExecutorAsync(policy, client, listener);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ secret }",
            TestContext.Current.CancellationToken);

        // assert
        var error = Assert.IsType<InvalidOperationException>(listener.Error);
        Assert.Equal(
            "Authorization policy 'CanReadSecret' requires unknown field 'Query.unknown'.",
            error.Message);
        Assert.Empty(client.Requests);
    }

    [Fact]
    public async Task ExecuteAsync_Should_FailClosed_When_RuntimeRequirementWasNotPlanned()
    {
        // arrange
        var executor = await CreateExecutorAsync(
            PolicyDenialBehavior.Null,
            new DriftingRequirementsPolicy());

        // act
        await using var result = await executor.ExecuteAsync(
            "{ secret }",
            TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Authorization policy execution failed.",
                  "extensions": {
                    "code": "AUTH_NOT_AUTHORIZED"
                  }
                }
              ],
              "data": null
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_NullFieldSilently_When_PolicyDeniesWithNull()
    {
        // arrange
        var executor = await CreateExecutorAsync(
            PolicyDenialBehavior.Null,
            new DenyPolicy());

        // act
        await using var result = await executor.ExecuteAsync(
            "{ secret }",
            TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "secret": null
              }
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_AddErrorAndNullField_When_PolicyDeniesWithError()
    {
        // arrange
        var executor = await CreateExecutorAsync(
            PolicyDenialBehavior.Error,
            new DenyPolicy());

        // act
        await using var result = await executor.ExecuteAsync(
            "{ secret }",
            TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "denied by test policy",
                  "path": [
                    "secret"
                  ],
                  "extensions": {
                    "code": "AUTH_NOT_AUTHORIZED",
                    "policy": "CanReadSecret"
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
    public async Task ExecuteAsync_Should_UseDefaultMessage_When_PolicyDeniesWithoutReason()
    {
        // arrange
        var executor = await CreateExecutorAsync(
            PolicyDenialBehavior.Error,
            new DenyWithoutReasonPolicy());

        // act
        await using var result = await executor.ExecuteAsync(
            "{ secret }",
            TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "path": [
                    "secret"
                  ],
                  "extensions": {
                    "code": "AUTH_NOT_AUTHORIZED",
                    "policy": "CanReadSecret"
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
    public async Task ExecuteAsync_Should_EvaluateRequirementFreePolicyOncePerRequest()
    {
        // arrange
        var policy = new CountingDenyPolicy();
        var executor = await CreateMultipleTargetExecutorAsync(policy);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ secret otherSecret }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, policy.EvaluationCount);
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "denied by counting policy",
                  "path": [
                    "otherSecret"
                  ],
                  "extensions": {
                    "code": "AUTH_NOT_AUTHORIZED",
                    "policy": "CanReadSecret"
                  }
                }
              ],
              "data": {
                "secret": null,
                "otherSecret": null
              }
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_EvaluateRequirementFreePolicyForEachRequest()
    {
        // arrange
        var policy = new CountingDenyPolicy();
        var executor = await CreateExecutorAsync(PolicyDenialBehavior.Null, policy);

        // act
        await using var firstResult = await executor.ExecuteAsync(
            "{ secret }",
            TestContext.Current.CancellationToken);
        await using var secondResult = await executor.ExecuteAsync(
            "{ secret }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(2, policy.EvaluationCount);
        Assert.Equal(firstResult.ToJson(), secondResult.ToJson());
    }

    [Fact]
    public async Task ExecuteAsync_Should_NotEvaluateRequirementFreePolicy_When_TargetIsSkipped()
    {
        // arrange
        var policy = new CountingDenyPolicy();
        var executor = await CreateExecutorAsync(PolicyDenialBehavior.Null, policy);

        // act
        await using var result = await executor.ExecuteAsync(
            "query($include: Boolean!) { secret @include(if: $include) }",
            new Dictionary<string, object?> { ["include"] = false },
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
    public async Task EvaluatePolicyOnceAsync_Should_ShareInFlightEvaluation()
    {
        // arrange
        var policy = new BlockingPolicy();
        var executor = await CreateExecutorAsync(PolicyDenialBehavior.Null, policy);
        var schema = Assert.IsType<FusionSchemaDefinition>(executor.Schema);
        var services = schema.Services;
        var context = new OperationPlanContext(
            services.GetRequiredService<INodeIdParser>(),
            services.GetRequiredService<IFusionExecutionDiagnosticEvents>(),
            services.GetRequiredService<IErrorHandler>());
        var application = new PolicyApplication
        {
            Name = policy.Name,
            OnDenied = PolicyDenialBehavior.Null
        };
        var type = schema.Types.GetType<ITypeDefinition>("Query");
        var entities = new CompositeResultElement[1];

        try
        {
            // act
            var first = context.EvaluatePolicyOnceAsync(
                policy,
                selection: null,
                type,
                new ClaimsPrincipal(),
                application,
                entities[0],
                TestContext.Current.CancellationToken).AsTask();
            await policy.Started.Task.WaitAsync(TestContext.Current.CancellationToken);
            var second = context.EvaluatePolicyOnceAsync(
                policy,
                selection: null,
                type,
                new ClaimsPrincipal(),
                application,
                entities[0],
                TestContext.Current.CancellationToken).AsTask();

            policy.Release.TrySetResult();
            var decisions = await Task.WhenAll(first, second);

            // assert
            Assert.Equal(1, policy.EvaluationCount);
            Assert.Collection(
                decisions,
                decision => Assert.True(decision.IsDenied),
                decision => Assert.True(decision.IsDenied));
        }
        finally
        {
            context.Destroy();
        }
    }

    [Fact]
    public async Task EvaluatePolicyOnceAsync_Should_CacheFailure()
    {
        // arrange
        var policy = new ThrowingPolicy();
        var executor = await CreateExecutorAsync(PolicyDenialBehavior.Null, policy);
        var schema = Assert.IsType<FusionSchemaDefinition>(executor.Schema);
        var services = schema.Services;
        var context = new OperationPlanContext(
            services.GetRequiredService<INodeIdParser>(),
            services.GetRequiredService<IFusionExecutionDiagnosticEvents>(),
            services.GetRequiredService<IErrorHandler>());
        var application = new PolicyApplication
        {
            Name = policy.Name,
            OnDenied = PolicyDenialBehavior.Null
        };
        var type = schema.Types.GetType<ITypeDefinition>("Query");
        var entity = default(CompositeResultElement);

        try
        {
            // act
            var firstError = await Assert.ThrowsAsync<InvalidOperationException>(
                () => context.EvaluatePolicyOnceAsync(
                    policy,
                    selection: null,
                    type,
                    new ClaimsPrincipal(),
                    application,
                    entity,
                    TestContext.Current.CancellationToken).AsTask());
            var secondError = await Assert.ThrowsAsync<InvalidOperationException>(
                () => context.EvaluatePolicyOnceAsync(
                    policy,
                    selection: null,
                    type,
                    new ClaimsPrincipal(),
                    application,
                    entity,
                    TestContext.Current.CancellationToken).AsTask());

            // assert
            Assert.Equal(1, policy.EvaluationCount);
            Assert.Same(firstError, secondError);
        }
        finally
        {
            context.Destroy();
        }
    }

    [Fact]
    public async Task ExecuteAsync_Should_AbortOperation_When_PolicyDeniesWithAbort()
    {
        // arrange
        var executor = await CreateExecutorAsync(
            PolicyDenialBehavior.Abort,
            new DenyPolicy());

        // act
        await using var result = await executor.ExecuteAsync(
            "{ secret }",
            TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "denied by test policy",
                  "path": [
                    "secret"
                  ],
                  "extensions": {
                    "code": "AUTH_NOT_AUTHORIZED",
                    "policy": "CanReadSecret"
                  }
                }
              ],
              "data": null
            }
            """);
    }

    [Fact]
    public async Task CreateExecutorAsync_Should_Fail_When_PolicyIsMissing()
    {
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => CreateExecutorAsync(PolicyDenialBehavior.Null));

        Assert.Equal(
            "Authorization policy 'CanReadSecret' was not found.",
            exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_Should_FailClosed_When_PolicyThrows()
    {
        // arrange
        var executor = await CreateExecutorAsync(
            PolicyDenialBehavior.Null,
            new ThrowingPolicy());

        // act
        await using var result = await executor.ExecuteAsync(
            "{ secret }",
            TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Authorization policy execution failed.",
                  "extensions": {
                    "code": "AUTH_NOT_AUTHORIZED"
                  }
                }
              ],
              "data": null
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_CancelQuietly_When_RequestTokenIsCanceledDuringPolicyEvaluation()
    {
        // arrange
        using var cancellationSource = new CancellationTokenSource();
        var listener = new ExecutionNodeErrorListener();
        var policy = new CooperativeCancellationPolicy(cancellationSource);
        var executor = await CreateExecutorAsync(
            PolicyDenialBehavior.Null,
            policy,
            diagnosticListener: listener);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ secret }",
            cancellationSource.Token);

        // assert
        Assert.True(policy.Evaluated);
        Assert.Null(listener.Error);
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The GraphQL request execution was canceled.",
                  "extensions": {
                    "code": "HC0049"
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_FailClosed_When_PolicyThrowsNonCooperativeCancellation()
    {
        // arrange
        var listener = new ExecutionNodeErrorListener();
        var executor = await CreateExecutorAsync(
            PolicyDenialBehavior.Null,
            new NonCooperativeCancellationPolicy(),
            diagnosticListener: listener);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ secret }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<OperationCanceledException>(listener.Error);
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Authorization policy execution failed.",
                  "extensions": {
                    "code": "AUTH_NOT_AUTHORIZED"
                  }
                }
              ],
              "data": null
            }
            """);
    }

    [Fact]
    public async Task CreateExecutorAsync_Should_Fail_When_PolicyNameThrows()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateExecutorAsync(
                PolicyDenialBehavior.Null,
                new ThrowingNamePolicy()));

        Assert.Equal("test name failure", exception.Message);
    }

    [Fact]
    public async Task CreateExecutorAsync_Should_Fail_When_PolicyRequirementsThrow()
    {
        var policy = new ThrowingRequirementsPolicy();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateExecutorAsync(
                PolicyDenialBehavior.Null,
                policy));

        Assert.Equal("test requirements failure", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProvideRootObjectContext_When_QueryTypeIsProtected()
    {
        // arrange
        var policy = new RootContextPolicy();
        var executor = await CreateRootObjectPolicyExecutorAsync(policy);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ secret }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(
            (Evaluated: true, SelectionWasNull: true, TypeName: "Query"),
            (policy.Evaluated, policy.SelectionWasNull, policy.TypeName));
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
    public async Task ExecuteAsync_Should_DeactivateAuthorizationContext_When_EvaluationCompletes()
    {
        // arrange
        var policy = new CaptureContextPolicy();
        var executor = await CreateExecutorAsync(PolicyDenialBehavior.Null, policy);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ secret }",
            TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "secret": "classified"
              }
            }
            """);
        Assert.Equal(
            (SelectionField: "secret", TypeName: "Query"),
            (policy.SelectionField, policy.TypeName));
        Assert.Throws<ObjectDisposedException>(() => policy.Context!.Deny(0));
    }

    [Fact]
    public async Task CreateExecutorAsync_Should_Fail_When_PolicyNameIsDuplicated()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateExecutorAsync(
                PolicyDenialBehavior.Null,
                createPolicies: () => [new DenyPolicy(), new DenyPolicy()]));

        Assert.Equal(
            "Authorization policy 'CanReadSecret' is registered more than once.",
            exception.Message);
    }

    [Fact]
    public async Task CreateExecutorAsync_Should_Fail_When_PolicyCreationThrows()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateExecutorAsync(
                PolicyDenialBehavior.Null,
                createPolicies: static () =>
                    throw new InvalidOperationException("test construction failure")));

        Assert.Equal("test construction failure", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_Should_FailClosed_When_NodeConditionVariableIsMissing()
    {
        // arrange
        var listener = new ExecutionNodeErrorListener();
        IServiceProvider? requestServices = null;
        var executor = await CreateExecutorAsync(
            PolicyDenialBehavior.Null,
            new DenyPolicy(),
            diagnosticListener: listener,
            captureRequestServices: services => requestServices = services);
        var schema = Assert.IsType<FusionSchemaDefinition>(executor.Schema);
        var sourcePlan = PlanOperation(schema, "{ secret }");
        var sourceNode = sourcePlan.AllNodes.OfType<OperationExecutionNode>().Single();
        var plannedPolicyNode = sourcePlan.AllNodes.OfType<PolicyExecutionNode>().Single();
        var policyNode = new PolicyExecutionNode(
            0,
            plannedPolicyNode.Targets.ToArray(),
            [
                new ExecutionNodeCondition
                {
                    VariableName = "missing",
                    PassingValue = true
                }
            ]);
        policyNode.Seal();
        var conditionPlan = OperationPlan.Create(
            "condition-test",
            sourcePlan.Operation,
            [policyNode],
            [policyNode],
            [],
            [],
            searchSpace: 0,
            expandedNodes: 0);
        var schemaServices = executor.Schema.Services;
        var requestContextPool = schemaServices.GetRequiredService<ObjectPool<PooledRequestContext>>();
        var context = new OperationPlanContext(
            schemaServices.GetRequiredService<INodeIdParser>(),
            schemaServices.GetRequiredService<IFusionExecutionDiagnosticEvents>(),
            schemaServices.GetRequiredService<IErrorHandler>());
        var requestContext = requestContextPool.Get();
        using var cts = new CancellationTokenSource();

        try
        {
            var request = OperationRequestBuilder.New()
                .SetDocument("{ secret }")
                .Build();
            requestContext.Initialize(
                executor.Schema,
                executor.Version,
                request,
                requestIndex: 0,
                requestServices: requestServices!,
                requestAborted: CancellationToken.None);
            context.Initialize(
                requestContext,
                VariableValueCollection.Empty,
                conditionPlan,
                cts,
                new MemoryArena());

            var payload = """{"data":{"secret":"classified"}}"""u8.ToArray();
            var arena = context.MemorySource.GetNextArena();
            var document = SourceResultDocument.Parse(arena, payload, payload.Length);
            context.AddPartialResult(
                SelectionPath.Root,
                new SourceSchemaResult(CompactPath.Root, document),
                sourceNode.ResultSelectionSet,
                containsErrors: false);

            // act
            policyNode.BeginExecute(context, TestContext.Current.CancellationToken);
            await context.ExecutionState.Signal;

            // assert
            Assert.True(context.ExecutionState.TryDequeueCompletedResult(out var nodeResult));
            var exception = Assert.IsType<InvalidOperationException>(nodeResult.Exception);
            Assert.Same(exception, listener.Error);

            await using var result = context.Complete();
            result.ToJson().MatchInlineSnapshot(
                """
                {
                  "errors": [
                    {
                      "message": "Authorization policy execution failed.",
                      "extensions": {
                        "code": "AUTH_NOT_AUTHORIZED"
                      }
                    }
                  ],
                  "data": null
                }
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
    public async Task ExecuteAsync_Should_NotDispatchDownstreamLookup_When_AllEntitiesAreDenied()
    {
        // arrange
        var downstreamClient = new CountingClient();
        var listener = new ExecutionNodeStartListener();
        var executor = await CreateLookupExecutorAsync(downstreamClient, listener);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ topProducts { price } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(0, downstreamClient.ExecutionCount);
        Assert.Equal(0, listener.DownstreamOperationStarts);
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "topProducts": null
              }
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_DispatchDownstreamLookup_When_PolicyAllows()
    {
        // arrange
        var downstreamClient = new RecordingLookupClient();
        var listener = new ExecutionNodeStartListener();
        var executor = await CreateLookupExecutorAsync(
            downstreamClient,
            listener,
            new AllowPolicy());

        // act
        await using var result = await executor.ExecuteAsync(
            "{ topProducts { price } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(
            (ExecutionCount: 1, DownstreamStarts: 1),
            (downstreamClient.ExecutionCount,
                DownstreamStarts: listener.DownstreamOperationStarts));
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "topProducts": [
                  {
                    "price": 9.99
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_DispatchOnlyAllowedEntity_When_OneEntityIsDenied()
    {
        // arrange
        var downstreamClient = new RecordingLookupClient();
        var listener = new ExecutionNodeStartListener();
        var executor = await CreatePartialLookupExecutorAsync(downstreamClient, listener);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ topProducts { price } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, downstreamClient.ExecutionCount);
        Assert.Equal(1, listener.DownstreamOperationStarts);
        downstreamClient.Requests.MatchInlineSnapshot(
            """
            query Op_bd09d2cc_3($__fusion_1_id: ID!) {
              productById(id: $__fusion_1_id) {
                price
              }
            }
            variables: {"__fusion_1_id":"1"}
            """);
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "denied product 2",
                  "path": [
                    "topProducts",
                    1
                  ],
                  "extensions": {
                    "code": "AUTH_NOT_AUTHORIZED",
                    "policy": "CanReadSecret"
                  }
                }
              ],
              "data": {
                "topProducts": [
                  {
                    "price": 9.99
                  },
                  null
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_OmitDeniedMemberAndDispatchUnrelatedMember_When_RegularBatchIsShared()
    {
        // arrange
        var downstreamClient = new SelectiveBatchClient();
        var listener = new ExecutionNodeStartListener();
        var executor = await CreateSelectiveBatchExecutorAsync(downstreamClient, listener);

        // act
        await using var result = await executor.ExecuteAsync(
            "query($includeViewer: Boolean!) { "
                + "topProducts { price } "
                + "viewers @include(if: $includeViewer) { name } }",
            new Dictionary<string, object?> { ["includeViewer"] = true },
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(
            (ExecuteCalls: 0, BatchCalls: 1, BatchedRequests: 1),
            (downstreamClient.ExecuteCalls,
                downstreamClient.BatchCalls,
                BatchedRequests: downstreamClient.Requests.Count));
        Assert.Equal(1, listener.DownstreamBatchStarts);
        downstreamClient.Requests.MatchInlineSnapshot(
            """
            [
              "query Op_8924044f_3($__fusion_1_id: ID!) {\n  viewerById(id: $__fusion_1_id) {\n    name\n  }\n}\nvariables: {\"__fusion_1_id\":\"v1\"}"
            ]
            """);
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "topProducts": [
                  null
                ],
                "viewers": [
                  {
                    "name": "Michael"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_NotStartRegularBatch_When_AllMembersAreDenied()
    {
        // arrange
        var downstreamClient = new SelectiveBatchClient();
        var listener = new ExecutionNodeStartListener();
        var executor = await CreateSelectiveBatchExecutorAsync(
            downstreamClient,
            listener,
            protectViewer: true);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ topProducts { price } viewers { name } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(
            (ExecuteCalls: 0, BatchCalls: 0, BatchedRequests: 0, BatchStarts: 0),
            (downstreamClient.ExecuteCalls,
                downstreamClient.BatchCalls,
                BatchedRequests: downstreamClient.Requests.Count,
                BatchStarts: listener.DownstreamBatchStarts));
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "topProducts": [
                  null
                ],
                "viewers": [
                  null
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_DispatchUnrelatedBatchMember_When_PolicyConditionIsNotMet()
    {
        // arrange
        var downstreamClient = new SelectiveBatchClient();
        var listener = new ExecutionNodeStartListener();
        var executor = await CreateSelectiveBatchExecutorAsync(downstreamClient, listener);

        // act
        await using var result = await executor.ExecuteAsync(
            "query($includeProducts: Boolean!) { "
                + "topProducts @include(if: $includeProducts) { price } "
                + "viewers { name } }",
            new Dictionary<string, object?> { ["includeProducts"] = false },
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(
            (ExecuteCalls: 0, BatchCalls: 1, BatchedRequests: 1, BatchStarts: 1),
            (downstreamClient.ExecuteCalls,
                downstreamClient.BatchCalls,
                BatchedRequests: downstreamClient.Requests.Count,
                BatchStarts: listener.DownstreamBatchStarts));
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "viewers": [
                  {
                    "name": "Michael"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_OmitDeniedMemberAndDispatchUnrelatedMember_When_ApolloBatchIsShared()
    {
        // arrange
        var downstreamClient = new SelectiveApolloBatchClient();
        var listener = new ExecutionNodeStartListener();
        var executor = await CreateSelectiveApolloBatchExecutorAsync(
            downstreamClient,
            listener);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ topProducts { price } viewers { name } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(
            (BatchCalls: 1, BatchedRequests: 1, BatchStarts: 1),
            (downstreamClient.BatchCalls,
                BatchedRequests: downstreamClient.Requests.Count,
                BatchStarts: listener.DownstreamBatchStarts));
        Assert.Equal(
            """
            query($representations: [_Any!]!) {
              _entities(representations: $representations) {
                ... on Viewer {
                  name
                }
              }
            }
            """,
            Assert.Single(downstreamClient.Requests));
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "topProducts": [
                  null
                ],
                "viewers": [
                  {
                    "name": "Michael"
                  }
                ]
              }
            }
            """);
    }

    private static async Task<IRequestExecutor> CreateExecutorAsync(
        PolicyDenialBehavior behavior,
        IAuthorizationPolicy? policy = null,
        Func<IReadOnlyList<IAuthorizationPolicy>>? createPolicies = null,
        FusionExecutionDiagnosticEventListener? diagnosticListener = null,
        Action<IServiceProvider>? captureRequestServices = null)
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

                    directive @policy(name: String!, onDenied: PolicyDenialBehavior! = NULL)
                      repeatable on OBJECT | INTERFACE | FIELD_DEFINITION

                    type Query {
                      secret: String @policy(name: "CanReadSecret", onDenied: {{behavior.ToString().ToUpperInvariant()}})
                    }
                    """));

        if (policy is not null && createPolicies is not null)
        {
            throw new ArgumentException("Specify either a policy or a policy factory.");
        }

        if (policy is not null)
        {
            ConfigurePolicies(builder, new TestAuthorizationPolicyProvider(policy));
        }
        else if (createPolicies is not null)
        {
            ConfigurePolicies(builder, new TestAuthorizationPolicyProvider(createPolicies));
        }

        if (diagnosticListener is not null)
        {
            builder.AddDiagnosticEventListener(_ => diagnosticListener);
        }

        builder.Services.AddSingleton<ISourceSchemaClientFactory>(
            new TestClientFactory(("a", new StaticResultClient())));

        FusionSetupUtilities.Configure(
            builder,
            setup => setup.ClientConfigurationModifiers.Add(
                _ => new TestClientConfiguration("a")));

        var serviceProvider = services.BuildServiceProvider();
        captureRequestServices?.Invoke(serviceProvider);
        return await serviceProvider
            .GetRequiredService<IRequestExecutorProvider>()
            .GetExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
    }

    private static async Task<IRequestExecutor> CreateRequirementExecutorAsync(
        IAuthorizationPolicy policy,
        RecordingRequirementClient client,
        PlanningErrorListener listener)
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

                    directive @policy(name: String!, onDenied: PolicyDenialBehavior! = NULL)
                      repeatable on OBJECT | INTERFACE | FIELD_DEFINITION

                    type Query {
                      secret: String @policy(name: "CanReadSecret")
                      role: String
                    }
                    """));

        ConfigurePolicies(builder, new TestAuthorizationPolicyProvider(policy));
        builder.AddDiagnosticEventListener(_ => listener);
        builder.Services.AddSingleton<ISourceSchemaClientFactory>(
            new TestClientFactory(("a", client)));

        FusionSetupUtilities.Configure(
            builder,
            setup => setup.ClientConfigurationModifiers.Add(
                _ => new TestClientConfiguration("a")));

        return await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<IRequestExecutor> CreateMultipleTargetExecutorAsync(
        IAuthorizationPolicy policy)
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

                    directive @policy(name: String!, onDenied: PolicyDenialBehavior! = NULL)
                      repeatable on OBJECT | INTERFACE | FIELD_DEFINITION

                    type Query {
                      secret: String @policy(name: "CanReadSecret")
                      otherSecret: String @policy(name: "CanReadSecret", onDenied: ERROR)
                    }
                    """));

        ConfigurePolicies(builder, new TestAuthorizationPolicyProvider(policy));
        builder.Services.AddSingleton<ISourceSchemaClientFactory>(
            new TestClientFactory(
                ("a", new RecordingRequirementClient(
                    """{"data":{"secret":"one","otherSecret":"two"}}"""))));

        FusionSetupUtilities.Configure(
            builder,
            setup => setup.ClientConfigurationModifiers.Add(
                _ => new TestClientConfiguration("a")));

        return await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<IRequestExecutor> CreateCrossSourceRequirementExecutorAsync(
        IAuthorizationPolicy policy,
        RecordingRequirementClient secretClient,
        RecordingRequirementClient roleClient,
        PlanningErrorListener listener)
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

                    directive @policy(name: String!, onDenied: PolicyDenialBehavior! = NULL)
                      repeatable on OBJECT | INTERFACE | FIELD_DEFINITION

                    type Query {
                      secret: String @policy(name: "CanReadSecret")
                    }
                    """,
                    """
                    # name: b
                    type Query {
                      role: String
                    }
                    """));

        ConfigurePolicies(builder, new TestAuthorizationPolicyProvider(policy));
        builder.AddDiagnosticEventListener(_ => listener);
        builder.Services.AddSingleton<ISourceSchemaClientFactory>(
            new TestClientFactory(
                ("a", secretClient),
                ("b", roleClient)));

        FusionSetupUtilities.Configure(
            builder,
            setup =>
            {
                setup.ClientConfigurationModifiers.Add(_ => new TestClientConfiguration("a"));
                setup.ClientConfigurationModifiers.Add(_ => new TestClientConfiguration("b"));
            });

        return await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<IRequestExecutor> CreateLookupExecutorAsync(
        ISourceSchemaClient downstreamClient,
        FusionExecutionDiagnosticEventListener? diagnosticListener = null,
        IAuthorizationPolicy? policy = null)
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

                    directive @policy(name: String!, onDenied: PolicyDenialBehavior! = NULL)
                      repeatable on OBJECT | INTERFACE | FIELD_DEFINITION

                    type Query {
                      topProducts: [Product!]
                    }

                    type Product @key(fields: "id") @policy(name: "CanReadSecret") {
                      id: ID!
                    }
                    """,
                    """
                    # name: b
                    type Query {
                      productById(id: ID!): Product @lookup @internal
                    }

                    type Product {
                      id: ID!
                      price: Float!
                    }
                    """));

        ConfigurePolicies(
            builder,
            new TestAuthorizationPolicyProvider(policy ?? new DenyPolicy()));

        if (diagnosticListener is not null)
        {
            builder.AddDiagnosticEventListener(_ => diagnosticListener);
        }

        builder.Services.AddSingleton<ISourceSchemaClientFactory>(
            new TestClientFactory(
                ("a", new ProductResultClient()),
                ("b", downstreamClient)));

        FusionSetupUtilities.Configure(
            builder,
            setup =>
            {
                setup.ClientConfigurationModifiers.Add(_ => new TestClientConfiguration("a"));
                setup.ClientConfigurationModifiers.Add(_ => new TestClientConfiguration("b"));
            });

        return await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
    }

    private static DocumentNode ComposeApolloPolicySchemaDocument(
        string sourceSchemaA,
        string sourceSchemaB)
    {
        var compositeSchema = ComposeSchemaDocument(sourceSchemaA, sourceSchemaB);
        var sourceText = compositeSchema.ToString();
        const string schemaMetadata = "B @fusion__schema_metadata(name: \"b\")";

        if (!sourceText.Contains(schemaMetadata, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The composed test schema does not contain source schema metadata for 'b'.");
        }

        return Utf8GraphQLParser.Parse(
            sourceText.Replace(
                schemaMetadata,
                "B @fusion__schema_metadata(name: \"b\", kind: \"ApolloFederation\")",
                StringComparison.Ordinal));
    }

    private static async Task<IRequestExecutor> CreateRootObjectPolicyExecutorAsync(
        IAuthorizationPolicy policy)
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

                    directive @policy(name: String!, onDenied: PolicyDenialBehavior! = NULL)
                      repeatable on OBJECT | INTERFACE | FIELD_DEFINITION

                    type Query @policy(name: "CanReadSecret") {
                      secret: String
                    }
                    """));

        ConfigurePolicies(builder, new TestAuthorizationPolicyProvider(policy));
        builder.Services.AddSingleton<ISourceSchemaClientFactory>(
            new TestClientFactory(("a", new StaticResultClient())));

        FusionSetupUtilities.Configure(
            builder,
            setup => setup.ClientConfigurationModifiers.Add(
                _ => new TestClientConfiguration("a")));

        return await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<IRequestExecutor> CreatePartialLookupExecutorAsync(
        RecordingLookupClient downstreamClient,
        FusionExecutionDiagnosticEventListener? diagnosticListener = null)
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

                    directive @policy(name: String!, onDenied: PolicyDenialBehavior! = NULL)
                      repeatable on OBJECT | INTERFACE | FIELD_DEFINITION

                    type Query {
                      topProducts: [Product]
                    }

                    type Product @key(fields: "id")
                      @policy(name: "CanReadSecret", onDenied: ERROR) {
                      id: ID!
                    }
                    """,
                    """
                    # name: b
                    type Query {
                      productById(id: ID!): Product @lookup @internal
                    }

                    type Product {
                      id: ID!
                      price: Float!
                    }
                    """));

        ConfigurePolicies(
            builder,
            new TestAuthorizationPolicyProvider(new DenySecondProductPolicy()));

        if (diagnosticListener is not null)
        {
            builder.AddDiagnosticEventListener(_ => diagnosticListener);
        }

        builder.Services.AddSingleton<ISourceSchemaClientFactory>(
            new TestClientFactory(
                ("a", new TwoProductResultClient()),
                ("b", downstreamClient)));

        FusionSetupUtilities.Configure(
            builder,
            setup =>
            {
                setup.ClientConfigurationModifiers.Add(_ => new TestClientConfiguration("a"));
                setup.ClientConfigurationModifiers.Add(_ => new TestClientConfiguration("b"));
            });

        return await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<IRequestExecutor> CreateSelectiveBatchExecutorAsync(
        SelectiveBatchClient downstreamClient,
        FusionExecutionDiagnosticEventListener? diagnosticListener = null,
        bool protectViewer = false)
    {
        var services = new ServiceCollection();
        services.AddHttpClient();

        var viewerPolicy = protectViewer
            ? "@policy(name: \"CanReadSecret\")"
            : string.Empty;
        var builder = services
            .AddGraphQLGateway()
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    $$"""
                    # name: a
                    enum PolicyDenialBehavior { NULL ERROR ABORT }

                    directive @policy(name: String!, onDenied: PolicyDenialBehavior! = NULL)
                      repeatable on OBJECT | INTERFACE | FIELD_DEFINITION

                    type Query {
                      topProducts: [Product]
                      viewers: [Viewer]
                    }

                    type Product @key(fields: "id") @policy(name: "CanReadSecret") {
                      id: ID!
                    }

                    type Viewer @key(fields: "id") {{viewerPolicy}} {
                      id: ID!
                    }
                    """,
                    """
                    # name: b
                    type Query {
                      productById(id: ID!): Product @lookup @internal
                      viewerById(id: ID!): Viewer @lookup @internal
                    }

                    type Product {
                      id: ID!
                      price: Float!
                    }

                    type Viewer {
                      id: ID!
                      name: String!
                    }
                    """));

        ConfigurePolicies(builder, new TestAuthorizationPolicyProvider(new DenyPolicy()));

        if (diagnosticListener is not null)
        {
            builder.AddDiagnosticEventListener(_ => diagnosticListener);
        }

        builder.Services.AddSingleton<ISourceSchemaClientFactory>(
            new TestClientFactory(
                ("a", new ProductAndViewerResultClient()),
                ("b", downstreamClient)));

        FusionSetupUtilities.Configure(
            builder,
            setup =>
            {
                setup.ClientConfigurationModifiers.Add(_ => new TestClientConfiguration("a"));
                setup.ClientConfigurationModifiers.Add(_ => new TestClientConfiguration("b"));
            });

        return await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<IRequestExecutor> CreateSelectiveApolloBatchExecutorAsync(
        SelectiveApolloBatchClient downstreamClient,
        FusionExecutionDiagnosticEventListener diagnosticListener)
    {
        var services = new ServiceCollection();
        services.AddHttpClient();

        var builder = services
            .AddGraphQLGateway()
            .AddInMemoryConfiguration(
                ComposeApolloPolicySchemaDocument(
                    """
                    # name: a
                    enum PolicyDenialBehavior { NULL ERROR ABORT }

                    directive @policy(name: String!, onDenied: PolicyDenialBehavior! = NULL)
                      repeatable on OBJECT | INTERFACE | FIELD_DEFINITION

                    type Query {
                      topProducts: [Product]
                      viewers: [Viewer]
                    }

                    type Product @key(fields: "id") @policy(name: "CanReadSecret") {
                      id: ID!
                    }

                    type Viewer @key(fields: "id") {
                      id: ID!
                    }
                    """,
                    """
                    # name: b
                    type Query {
                      productById(id: ID!): Product @lookup @internal
                      viewerById(id: ID!): Viewer @lookup @internal
                    }

                    type Product {
                      id: ID!
                      price: Float!
                    }

                    type Viewer {
                      id: ID!
                      name: String!
                    }
                    """));

        ConfigurePolicies(builder, new TestAuthorizationPolicyProvider(new DenyPolicy()));
        builder.AddDiagnosticEventListener(_ => diagnosticListener);
        builder.Services.AddSingleton<ISourceSchemaClientFactory>(
            new TestClientFactory(
                ("a", new ProductAndViewerResultClient()),
                ("b", downstreamClient)));

        FusionSetupUtilities.Configure(
            builder,
            setup =>
            {
                setup.ClientConfigurationModifiers.Add(_ => new TestClientConfiguration("a"));
                setup.ClientConfigurationModifiers.Add(_ => new TestClientConfiguration("b"));
            });

        return await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
    }

    private static void ConfigurePolicies(
        IFusionGatewayBuilder builder,
        IAuthorizationPolicyProvider provider)
        => builder.ConfigureSchemaServices(
            (_, services) => services.AddSingleton(_ => provider));

    private sealed class DenyPolicy : IAuthorizationPolicy
    {
        public string Name => "CanReadSecret";

        public SelectionSetNode? Requirements => null;

        public ValueTask EvaluateAsync(
            IAuthorizationContext context,
            EntityData entities,
            CancellationToken cancellationToken = default)
        {
            for (var i = 0; i < entities.Count; i++)
            {
                context.Deny(i, "denied by test policy");
            }

            return ValueTask.CompletedTask;
        }
    }

    private sealed class DenyWithoutReasonPolicy : IAuthorizationPolicy
    {
        public string Name => "CanReadSecret";

        public SelectionSetNode? Requirements => null;

        public ValueTask EvaluateAsync(
            IAuthorizationContext context,
            EntityData entities,
            CancellationToken cancellationToken = default)
        {
            for (var i = 0; i < entities.Count; i++)
            {
                context.Deny(i);
            }

            return ValueTask.CompletedTask;
        }
    }

    private sealed class CountingDenyPolicy : IAuthorizationPolicy
    {
        private int _evaluationCount;

        public string Name => "CanReadSecret";

        public SelectionSetNode? Requirements => null;

        public int EvaluationCount => Volatile.Read(ref _evaluationCount);

        public ValueTask EvaluateAsync(
            IAuthorizationContext context,
            EntityData entities,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _evaluationCount);
            context.Deny(0, "denied by counting policy");
            return ValueTask.CompletedTask;
        }
    }

    private sealed class BlockingPolicy : IAuthorizationPolicy
    {
        private int _evaluationCount;

        public string Name => "CanReadSecret";

        public SelectionSetNode? Requirements => null;

        public int EvaluationCount => Volatile.Read(ref _evaluationCount);

        public TaskCompletionSource Started { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async ValueTask EvaluateAsync(
            IAuthorizationContext context,
            EntityData entities,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _evaluationCount);
            Started.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
            context.Deny(0);
        }
    }

    private sealed class RoleRequirementPolicy : IAuthorizationPolicy
    {
        private static readonly SelectionSetNode s_requirements =
            Utf8GraphQLParser.Syntax.ParseSelectionSet("{ role }");

        public string Name => "CanReadSecret";

        public SelectionSetNode? Requirements => s_requirements;

        public bool Evaluated { get; private set; }

        public string? Role { get; private set; }

        public ValueTask EvaluateAsync(
            IAuthorizationContext context,
            EntityData entities,
            CancellationToken cancellationToken = default)
        {
            Assert.Equal(1, entities.Count);
            Evaluated = true;
            Role = entities[0].GetProperty("role").GetString();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class UnknownRequirementPolicy : IAuthorizationPolicy
    {
        private static readonly SelectionSetNode s_requirements =
            Utf8GraphQLParser.Syntax.ParseSelectionSet("{ unknown }");

        public string Name => "CanReadSecret";

        public SelectionSetNode? Requirements => s_requirements;

        public ValueTask EvaluateAsync(
            IAuthorizationContext context,
            EntityData entities,
            CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;
    }

    private sealed class DriftingRequirementsPolicy : IAuthorizationPolicy
    {
        private static readonly SelectionSetNode s_requirements =
            Utf8GraphQLParser.Syntax.ParseSelectionSet("{ unknown }");
        private int _readCount;

        public string Name => "CanReadSecret";

        public SelectionSetNode? Requirements
            => Interlocked.Increment(ref _readCount) == 1 ? null : s_requirements;

        public ValueTask EvaluateAsync(
            IAuthorizationContext context,
            EntityData entities,
            CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;
    }

    private sealed class AllowPolicy : IAuthorizationPolicy
    {
        public string Name => "CanReadSecret";

        public SelectionSetNode? Requirements => null;

        public ValueTask EvaluateAsync(
            IAuthorizationContext context,
            EntityData entities,
            CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;
    }

    private sealed class ThrowingPolicy : IAuthorizationPolicy
    {
        private int _evaluationCount;

        public string Name => "CanReadSecret";

        public SelectionSetNode? Requirements => null;

        public int EvaluationCount => Volatile.Read(ref _evaluationCount);

        public ValueTask EvaluateAsync(
            IAuthorizationContext context,
            EntityData entities,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _evaluationCount);
            throw new InvalidOperationException("test failure");
        }
    }

    private sealed class CooperativeCancellationPolicy(CancellationTokenSource cancellationSource)
        : IAuthorizationPolicy
    {
        public string Name => "CanReadSecret";

        public SelectionSetNode? Requirements => null;

        public bool Evaluated { get; private set; }

        public ValueTask EvaluateAsync(
            IAuthorizationContext context,
            EntityData entities,
            CancellationToken cancellationToken = default)
        {
            Evaluated = true;
            cancellationSource.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException("The linked cancellation token was not canceled.");
        }
    }

    private sealed class NonCooperativeCancellationPolicy : IAuthorizationPolicy
    {
        public string Name => "CanReadSecret";

        public SelectionSetNode? Requirements => null;

        public ValueTask EvaluateAsync(
            IAuthorizationContext context,
            EntityData entities,
            CancellationToken cancellationToken = default)
            => throw new OperationCanceledException("non-cooperative cancellation");
    }

    private sealed class ThrowingNamePolicy : IAuthorizationPolicy
    {
        public string Name => throw new InvalidOperationException("test name failure");

        public SelectionSetNode? Requirements => null;

        public ValueTask EvaluateAsync(
            IAuthorizationContext context,
            EntityData entities,
            CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;
    }

    private sealed class ThrowingRequirementsPolicy : IAuthorizationPolicy
    {
        public string Name => "CanReadSecret";

        public SelectionSetNode? Requirements
            => throw new InvalidOperationException("test requirements failure");

        public ValueTask EvaluateAsync(
            IAuthorizationContext context,
            EntityData entities,
            CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;
    }

    private sealed class CaptureContextPolicy : IAuthorizationPolicy
    {
        public string Name => "CanReadSecret";

        public SelectionSetNode? Requirements => null;

        public IAuthorizationContext? Context { get; private set; }

        public string? SelectionField { get; private set; }

        public string? TypeName { get; private set; }

        public ValueTask EvaluateAsync(
            IAuthorizationContext context,
            EntityData entities,
            CancellationToken cancellationToken = default)
        {
            Context = context;
            SelectionField = context.Selection?.Field.Name;
            TypeName = context.Type.Name;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RootContextPolicy : IAuthorizationPolicy
    {
        public string Name => "CanReadSecret";

        public SelectionSetNode? Requirements => null;

        public bool Evaluated { get; private set; }

        public bool SelectionWasNull { get; private set; }

        public string? TypeName { get; private set; }

        public ValueTask EvaluateAsync(
            IAuthorizationContext context,
            EntityData entities,
            CancellationToken cancellationToken = default)
        {
            Evaluated = true;
            SelectionWasNull = context.Selection is null;
            TypeName = context.Type.Name;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ExecutionNodeErrorListener : FusionExecutionDiagnosticEventListener
    {
        public Exception? Error { get; private set; }

        public override void ExecutionNodeError(
            OperationPlanContext context,
            ExecutionNode node,
            Exception error)
        {
            Error = error;
        }
    }

    private sealed class PlanningErrorListener : FusionExecutionDiagnosticEventListener
    {
        public Exception? Error { get; private set; }

        public override void PlanOperationError(
            RequestContext context,
            string operationId,
            Exception error)
        {
            Error = error;
        }
    }

    private sealed class ExecutionNodeStartListener : FusionExecutionDiagnosticEventListener
    {
        public int DownstreamOperationStarts { get; private set; }

        public int DownstreamBatchStarts { get; private set; }

        public override IDisposable ExecuteOperationNode(
            OperationPlanContext context,
            OperationExecutionNode node,
            string schemaName)
        {
            if (schemaName.Equals("b", StringComparison.Ordinal))
            {
                DownstreamOperationStarts++;
            }

            return EmptyScope;
        }

        public override IDisposable ExecuteOperationBatchNode(
            OperationPlanContext context,
            OperationBatchExecutionNode node,
            string schemaName)
        {
            if (schemaName.Equals("b", StringComparison.Ordinal))
            {
                DownstreamBatchStarts++;
            }

            return EmptyScope;
        }

        public override IDisposable ExecuteApolloOperationExecutionNode(
            OperationPlanContext context,
            ApolloOperationExecutionNode node,
            string schemaName)
        {
            if (schemaName.Equals("b", StringComparison.Ordinal))
            {
                DownstreamOperationStarts++;
            }

            return EmptyScope;
        }

        public override IDisposable ExecuteApolloOperationBatchExecutionNode(
            OperationPlanContext context,
            ApolloOperationBatchExecutionNode node,
            string schemaName)
        {
            if (schemaName.Equals("b", StringComparison.Ordinal))
            {
                DownstreamBatchStarts++;
            }

            return EmptyScope;
        }
    }

    private sealed class DenySecondProductPolicy : IAuthorizationPolicy
    {
        private static readonly SelectionSetNode s_requirements =
            Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }");

        public string Name => "CanReadSecret";

        public SelectionSetNode? Requirements => s_requirements;

        public ValueTask EvaluateAsync(
            IAuthorizationContext context,
            EntityData entities,
            CancellationToken cancellationToken = default)
        {
            for (var i = 0; i < entities.Count; i++)
            {
                if (entities[i].GetProperty("id").GetString() == "2")
                {
                    context.Deny(i, "denied product 2");
                }
            }

            return ValueTask.CompletedTask;
        }
    }

    private sealed class StaticResultClient : ISourceSchemaClient
    {
        private static readonly byte[] s_payload = """{"data":{"secret":"classified"}}"""u8.ToArray();

        public SourceSchemaClientCapabilities Capabilities => SourceSchemaClientCapabilities.None;

        public async IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var arena = context.MemorySource.GetNextArena();
            var document = SourceResultDocument.Parse(arena, s_payload, s_payload.Length);
            await Task.Yield();
            yield return new SourceSchemaResult(CompactPath.Root, document);
        }

        public IAsyncEnumerable<SourceSchemaBatchResult> ExecuteBatchAsync(
            OperationPlanContext context,
            ImmutableArray<SourceSchemaClientRequest> requests,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public IAsyncEnumerable<SourceSchemaResult> SubscribeAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class RecordingRequirementClient : ISourceSchemaClient
    {
        private readonly byte[] _payload;

        public RecordingRequirementClient()
            : this("""{"data":{"secret":"classified","role":"admin"}}""")
        {
        }

        public RecordingRequirementClient(string payload)
        {
            _payload = Encoding.UTF8.GetBytes(payload);
        }

        public List<string> Requests { get; } = [];

        public SourceSchemaClientCapabilities Capabilities => SourceSchemaClientCapabilities.None;

        public async IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Requests.Add(request.OperationSourceText);
            var arena = context.MemorySource.GetNextArena();
            var document = SourceResultDocument.Parse(arena, _payload, _payload.Length);
            await Task.Yield();
            yield return new SourceSchemaResult(CompactPath.Root, document);
        }

        public IAsyncEnumerable<SourceSchemaBatchResult> ExecuteBatchAsync(
            OperationPlanContext context,
            ImmutableArray<SourceSchemaClientRequest> requests,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public IAsyncEnumerable<SourceSchemaResult> SubscribeAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class ProductResultClient : ISourceSchemaClient
    {
        private static readonly byte[] s_payload = """{"data":{"topProducts":[{"id":"1"}]}}"""u8.ToArray();

        public SourceSchemaClientCapabilities Capabilities => SourceSchemaClientCapabilities.None;

        public async IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var arena = context.MemorySource.GetNextArena();
            var document = SourceResultDocument.Parse(arena, s_payload, s_payload.Length);
            await Task.Yield();
            yield return new SourceSchemaResult(CompactPath.Root, document);
        }

        public IAsyncEnumerable<SourceSchemaBatchResult> ExecuteBatchAsync(
            OperationPlanContext context,
            ImmutableArray<SourceSchemaClientRequest> requests,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public IAsyncEnumerable<SourceSchemaResult> SubscribeAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class TwoProductResultClient : ISourceSchemaClient
    {
        private static readonly byte[] s_payload =
            """{"data":{"topProducts":[{"id":"1"},{"id":"2"}]}}"""u8.ToArray();

        public SourceSchemaClientCapabilities Capabilities => SourceSchemaClientCapabilities.None;

        public async IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var arena = context.MemorySource.GetNextArena();
            var document = SourceResultDocument.Parse(arena, s_payload, s_payload.Length);
            await Task.Yield();
            yield return new SourceSchemaResult(CompactPath.Root, document);
        }

        public IAsyncEnumerable<SourceSchemaBatchResult> ExecuteBatchAsync(
            OperationPlanContext context,
            ImmutableArray<SourceSchemaClientRequest> requests,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public IAsyncEnumerable<SourceSchemaResult> SubscribeAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class RecordingLookupClient : ISourceSchemaClient
    {
        private static readonly byte[] s_payload =
            """{"data":{"productById":{"price":9.99}}}"""u8.ToArray();

        public int ExecutionCount { get; private set; }

        public string Requests { get; private set; } = string.Empty;

        public SourceSchemaClientCapabilities Capabilities => SourceSchemaClientCapabilities.None;

        public async IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            ExecutionCount++;
            Requests = request.OperationSourceText
                + "\nvariables: "
                + Encoding.UTF8.GetString(request.Variables[0].Values.AsSequence().ToArray());

            var arena = context.MemorySource.GetNextArena();
            var document = SourceResultDocument.Parse(arena, s_payload, s_payload.Length);
            await Task.Yield();
            var variable = request.Variables[0];
            yield return new SourceSchemaResult(
                variable.Path,
                document,
                additionalPaths: variable.AdditionalPaths);
        }

        public IAsyncEnumerable<SourceSchemaBatchResult> ExecuteBatchAsync(
            OperationPlanContext context,
            ImmutableArray<SourceSchemaClientRequest> requests,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public IAsyncEnumerable<SourceSchemaResult> SubscribeAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class ProductAndViewerResultClient : ISourceSchemaClient
    {
        private static readonly byte[] s_payload =
            """{"data":{"topProducts":[{"id":"1"}],"viewers":[{"id":"v1"}]}}"""u8.ToArray();

        public SourceSchemaClientCapabilities Capabilities => SourceSchemaClientCapabilities.None;

        public async IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var arena = context.MemorySource.GetNextArena();
            var document = SourceResultDocument.Parse(arena, s_payload, s_payload.Length);
            await Task.Yield();
            yield return new SourceSchemaResult(CompactPath.Root, document);
        }

        public IAsyncEnumerable<SourceSchemaBatchResult> ExecuteBatchAsync(
            OperationPlanContext context,
            ImmutableArray<SourceSchemaClientRequest> requests,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public IAsyncEnumerable<SourceSchemaResult> SubscribeAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class SelectiveBatchClient : ISourceSchemaClient
    {
        private static readonly byte[] s_viewerPayload =
            """{"data":{"viewerById":{"name":"Michael"}}}"""u8.ToArray();

        public int ExecuteCalls { get; private set; }

        public int BatchCalls { get; private set; }

        public List<string> Requests { get; } = [];

        public SourceSchemaClientCapabilities Capabilities => SourceSchemaClientCapabilities.None;

        public async IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            ExecuteCalls++;
            Requests.Add(FormatRequest(request));
            await Task.Yield();
            yield break;
        }

        public async IAsyncEnumerable<SourceSchemaBatchResult> ExecuteBatchAsync(
            OperationPlanContext context,
            ImmutableArray<SourceSchemaClientRequest> requests,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            BatchCalls++;

            for (var i = 0; i < requests.Length; i++)
            {
                var request = requests[i];
                Requests.Add(FormatRequest(request));

                if (!request.OperationSourceText.Contains("viewerById", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("The denied product lookup was dispatched.");
                }

                var arena = context.MemorySource.GetNextArena();
                var document = SourceResultDocument.Parse(
                    arena,
                    s_viewerPayload,
                    s_viewerPayload.Length);
                await Task.Yield();
                yield return new SourceSchemaBatchResult(
                    i,
                    new SourceSchemaResult(
                        request.Variables[0].Path,
                        document,
                        additionalPaths: request.Variables[0].AdditionalPaths));
            }
        }

        public IAsyncEnumerable<SourceSchemaResult> SubscribeAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private static string FormatRequest(SourceSchemaClientRequest request)
        {
            var variables = request.Variables.Length == 0
                ? "{}"
                : Encoding.UTF8.GetString(request.Variables[0].Values.AsSequence().ToArray());
            return request.OperationSourceText + "\nvariables: " + variables;
        }
    }

    private sealed class SelectiveApolloBatchClient : ISourceSchemaClient
    {
        private static readonly byte[] s_viewerPayload =
            """{"data":{"_entities":[{"name":"Michael"}]}}"""u8.ToArray();

        public int BatchCalls { get; private set; }

        public List<string> Requests { get; } = [];

        public SourceSchemaClientCapabilities Capabilities => SourceSchemaClientCapabilities.None;

        public IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public async IAsyncEnumerable<SourceSchemaBatchResult> ExecuteBatchAsync(
            OperationPlanContext context,
            ImmutableArray<SourceSchemaClientRequest> requests,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            BatchCalls++;

            for (var i = 0; i < requests.Length; i++)
            {
                var request = requests[i];
                Requests.Add(request.OperationSourceText);

                if (!request.OperationSourceText.Contains("... on Viewer", StringComparison.Ordinal)
                    || request.OperationSourceText.Contains("... on Product", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("The denied product lookup was dispatched.");
                }

                var arena = context.MemorySource.GetNextArena();
                var document = SourceResultDocument.Parse(
                    arena,
                    s_viewerPayload,
                    s_viewerPayload.Length);
                await Task.Yield();
                yield return new SourceSchemaBatchResult(
                    i,
                    new SourceSchemaResult(CompactPath.Root, document));
            }
        }

        public IAsyncEnumerable<SourceSchemaResult> SubscribeAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class CountingClient : ISourceSchemaClient
    {
        public int ExecutionCount { get; private set; }

        public SourceSchemaClientCapabilities Capabilities => SourceSchemaClientCapabilities.None;

        public async IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            ExecutionCount++;
            await Task.Yield();
            yield break;
        }

        public IAsyncEnumerable<SourceSchemaBatchResult> ExecuteBatchAsync(
            OperationPlanContext context,
            ImmutableArray<SourceSchemaClientRequest> requests,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public IAsyncEnumerable<SourceSchemaResult> SubscribeAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class TestClientFactory(params (string Name, ISourceSchemaClient Client)[] clients)
        : ISourceSchemaClientFactory
    {
        public bool CanHandle(ISourceSchemaClientConfiguration configuration)
            => configuration is TestClientConfiguration;

        public ISourceSchemaClient CreateClient(
            FusionSchemaDefinition schema,
            ISourceSchemaClientConfiguration configuration)
            => clients.Single(t => t.Name == configuration.Name).Client;
    }

    private sealed class TestClientConfiguration(string name)
        : ISourceSchemaClientConfiguration
    {
        public string Name { get; } = name;

        public SupportedOperationType SupportedOperations => SupportedOperationType.Query;
    }
}
