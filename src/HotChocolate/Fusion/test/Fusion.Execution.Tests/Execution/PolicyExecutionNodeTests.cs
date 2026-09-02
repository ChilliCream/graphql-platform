using System.Buffers;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
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
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution;

public sealed partial class PolicyExecutionNodeTests : FusionTestBase
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
        var report = $"""
            error: {listener.Error?.Message ?? "<none>"}
            secretRequests: {secretClient.Requests.Count}
            secretContainsSecret: {secretClient.Requests.Any(r => r.Contains("secret", StringComparison.Ordinal))}
            roleRequests: {roleClient.Requests.Count}
            roleContainsRole: {roleClient.Requests.Any(r => r.Contains("role", StringComparison.Ordinal))}
            policyEvaluated: {policy.Evaluated}
            policyRole: {policy.Role}
            """;
        report.MatchInlineSnapshot(
            """
            error: <none>
            secretRequests: 1
            secretContainsSecret: True
            roleRequests: 1
            roleContainsRole: True
            policyEvaluated: True
            policyRole: admin
            """);
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
                    "code": "UNAUTHORIZED_FIELD_OR_TYPE"
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
        NormalizeReasonId(result.ToJson()).MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "path": [
                    "otherSecret"
                  ],
                  "extensions": {
                    "code": "UNAUTHORIZED_FIELD_OR_TYPE",
                    "reasonId": "00000000-0000-0000-0000-000000000000"
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
    public async Task ExecuteAsync_Should_UsePinnedPolicy_When_ProviderReplacesItDuringRequest()
    {
        // arrange
        var initialPolicy = new BlockingPolicy();
        var originalSecondPolicy = new CountingAllowPolicy("CanReadSecond");
        var replacementFirstPolicy = new CountingAllowPolicy("CanReadSecret");
        var replacementSecondPolicy = new CountingAllowPolicy("CanReadSecond");
        var executor = await CreateExpressionExecutorAsync(
            "@policy(names: [[\"CanReadSecret\", \"CanReadSecond\"]], onDenied: NULL)",
            initialPolicy,
            originalSecondPolicy);
        var provider = Assert.IsType<TestPolicyProvider>(
            executor.Schema.Services.GetRequiredService<IPolicyProvider>());

        // act
        var execution = executor.ExecuteAsync("{ secret }", TestContext.Current.CancellationToken);
        await initialPolicy.Started.Task.WaitAsync(TestContext.Current.CancellationToken);
        provider.Emit(replacementFirstPolicy, replacementSecondPolicy);
        initialPolicy.Release.TrySetResult();
        await using var result = await execution;

        // assert
        Assert.Equal(1, initialPolicy.EvaluationCount);
        Assert.Equal(1, originalSecondPolicy.EvaluationCount);
        Assert.Equal(0, replacementFirstPolicy.EvaluationCount);
        Assert.Equal(0, replacementSecondPolicy.EvaluationCount);
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
    public async Task ExecuteAsync_Should_GrantAccess_When_AnyOrGroupPasses()
    {
        // arrange
        var executor = await CreateExpressionExecutorAsync(
            """@policy(names: [["CanDeny"], ["CanAllow"]], onDenied: ERROR)""",
            new NamedStaticPolicy("CanDeny", deny: true, reason: "denied by CanDeny"),
            new NamedStaticPolicy("CanAllow", deny: false));

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
    }

    [Fact]
    public async Task ExecuteAsync_Should_DenyAccess_When_AndGroupMemberFails()
    {
        // arrange
        var executor = await CreateExpressionExecutorAsync(
            """@policy(names: [["CanAllow", "CanDeny"]], onDenied: ERROR)""",
            new NamedStaticPolicy("CanAllow", deny: false),
            new NamedStaticPolicy("CanDeny", deny: true, reason: "denied by CanDeny"));

        // act
        await using var result = await executor.ExecuteAsync(
            "{ secret }",
            TestContext.Current.CancellationToken);

        // assert
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
    public async Task ExecuteAsync_Should_EvaluateSharedPolicyOncePerTarget_When_NameAppearsInMultipleGroups()
    {
        // arrange
        var countingPolicy = new CountingRequirementPolicy("CanAudit");
        var executor = await CreateExpressionExecutorAsync(
            """@policy(names: [["CanAudit", "CanDeny"], ["CanAudit"]], onDenied: ERROR)""",
            countingPolicy,
            new NamedStaticPolicy("CanDeny", deny: true, reason: "denied by CanDeny"));

        // act
        await using var result = await executor.ExecuteAsync(
            "{ secret }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, countingPolicy.EvaluationCount);
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
    public async Task ExecuteAsync_Should_UseMostSevereBehavior_When_MultipleApplicationsDeny()
    {
        // arrange
        var executor = await CreateExpressionExecutorAsync(
            """@policy(names: "CanDeny") @policy(names: "CanAlsoDeny", onDenied: ERROR)""",
            new NamedStaticPolicy("CanDeny", deny: true, reason: "denied by CanDeny"),
            new NamedStaticPolicy("CanAlsoDeny", deny: true, reason: "denied by CanAlsoDeny"));

        // act
        await using var result = await executor.ExecuteAsync(
            "{ secret }",
            TestContext.Current.CancellationToken);

        // assert
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
    public async Task ExecuteAsync_Should_SkipRemainingOrGroups_When_EarlierGroupAllowsAllEntities()
    {
        // arrange
        // The second OR group's policy throws; it must not be evaluated because
        // the first group already satisfies the expression for every entity.
        var executor = await CreateExpressionExecutorAsync(
            """@policy(names: [["CanAllow"], ["CanAudit"]], onDenied: ERROR)""",
            new NamedStaticPolicy("CanAllow", deny: false),
            new NamedThrowingPolicy("CanAudit"));

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
    }

    [Fact]
    public async Task ExecuteAsync_Should_KeepMostSevereBehavior_When_LessSevereApplicationDeniesLater()
    {
        // arrange
        // The second application denies with the default NULL behavior; it must
        // not downgrade the ERROR attribution of the first application.
        var executor = await CreateExpressionExecutorAsync(
            """@policy(names: "CanDeny", onDenied: ERROR) @policy(names: "CanAlsoDeny")""",
            new NamedStaticPolicy("CanDeny", deny: true, reason: "denied by CanDeny"),
            new NamedStaticPolicy("CanAlsoDeny", deny: true, reason: "denied by CanAlsoDeny"));

        // act
        await using var result = await executor.ExecuteAsync(
            "{ secret }",
            TestContext.Current.CancellationToken);

        // assert
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
    public async Task ExecuteAsync_Should_EvaluateSharedPolicyOnce_When_NameIsSharedAcrossApplications()
    {
        // arrange
        // CanShared appears in both applications, but must be evaluated only once.
        var sharedPolicy = new CountingAllowPolicy("CanShared");
        var executor = await CreateExpressionExecutorAsync(
            """@policy(names: "CanShared") @policy(names: [["CanShared", "CanOther"]], onDenied: ERROR)""",
            sharedPolicy,
            new NamedStaticPolicy("CanOther", deny: false));

        // act
        await using var result = await executor.ExecuteAsync(
            "{ secret }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, sharedPolicy.EvaluationCount);
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
    public async Task ExecuteAsync_Should_AllowAllEntities_When_DifferentOrGroupsSatisfyDifferentEntities()
    {
        // arrange
        // Each entity is satisfied by a different OR group, so both products
        // must remain accessible.
        var executor = await CreateTwoProductExpressionExecutorAsync(
            """@policy(names: [["CanReadFirst"], ["CanReadSecond"]], onDenied: ERROR)""",
            new DenyMatchingIdPolicy("CanReadFirst", deniedId: "2"),
            new DenyMatchingIdPolicy("CanReadSecond", deniedId: "1"));

        // act
        await using var result = await executor.ExecuteAsync(
            "{ topProducts { id } }",
            TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "topProducts": [
                  {
                    "id": "1"
                  },
                  {
                    "id": "2"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_UseFirstFailingPolicyReason_When_EarlierPolicyDeniesWithoutReason()
    {
        // arrange
        var executor = await CreateExpressionExecutorAsync(
            """@policy(names: [["CanDenyQuietly", "CanDenyWithReason"]], onDenied: ERROR)""",
            new NamedStaticPolicy("CanDenyQuietly", deny: true),
            new NamedStaticPolicy("CanDenyWithReason", deny: true, reason: "denied loudly"));

        // act
        await using var result = await executor.ExecuteAsync(
            "{ secret }",
            TestContext.Current.CancellationToken);

        // assert
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
                    "code": "UNAUTHORIZED_FIELD_OR_TYPE"
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
                    "code": "UNAUTHORIZED_FIELD_OR_TYPE"
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
    public async Task ExecuteAsync_Should_ProvideNoSelection_When_QueryTypeIsProtected()
    {
        // arrange
        // The policy declares no resource requirement, so it produces a request-constant decision
        // and observes no guarded selection at all, regardless of the object target it applies to.
        var policy = new RootContextPolicy();
        var executor = await CreateRootObjectPolicyExecutorAsync(policy);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ secret }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(
            (Evaluated: true, SelectionWasNull: true),
            (policy.Evaluated, policy.SelectionWasNull));
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
    public async Task ExecuteAsync_Should_ProvideNoSelection_When_NestedTypeIsProtected()
    {
        // arrange
        // The policy declares no resource requirement, so it produces a request-constant decision
        // and observes no guarded selection even when the guarded value is produced by an
        // enclosing field.
        var policy = new RootContextPolicy();
        var executor = await CreateNestedTypePolicyExecutorAsync(policy);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ product { id } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(
            (Evaluated: true, SelectionWasNull: true),
            (policy.Evaluated, policy.SelectionWasNull));
    }

    [Fact]
    public async Task ExecuteAsync_Should_DenyProduct_When_NodeReturnsProtectedConcreteType()
    {
        // arrange
        var executor = await CreateAbstractTypePolicyExecutorAsync(
            new DenyPolicy(),
            """
            type Query {
              item: Node
            }

            interface Node {
              id: ID!
            }

            type Product implements Node @policy(names: "CanReadSecret") {
              id: ID!
            }

            type Viewer implements Node {
              id: ID!
            }
            """,
            """{"data":{"item":{"__typename":"Product","id":"1"}}}""");

        // act
        await using var result = await executor.ExecuteAsync(
            "{ item { __typename id } }",
            TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "item": null
              }
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_DenyProduct_When_UnionReturnsProtectedConcreteType()
    {
        // arrange
        var executor = await CreateAbstractTypePolicyExecutorAsync(
            new DenyPolicy(),
            """
            type Query {
              result: SearchResult
            }

            union SearchResult = Product | Viewer

            type Product @policy(names: "CanReadSecret") {
              id: ID!
            }

            type Viewer {
              id: ID!
            }
            """,
            """{"data":{"result":{"__typename":"Product","id":"1"}}}""");

        // act
        await using var result = await executor.ExecuteAsync(
            "{ result { __typename ... on Product { id } ... on Viewer { id } } }",
            TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "result": null
              }
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_DenyOnlyConcreteInstances_When_AbstractResultContainsPolicyType()
    {
        // arrange
        var executor = await CreateAbstractTypePolicyExecutorAsync(
            new DenyPolicy(),
            """
            type Query {
              nodes: [Node]
            }

            interface Node {
              id: ID!
            }

            type Product implements Node @policy(names: "CanReadSecret") {
              id: ID!
            }

            type Viewer implements Node {
              id: ID!
            }
            """,
            """{"data":{"nodes":[{"__typename":"Product","id":"1"},{"__typename":"Viewer","id":"2"}]}}""");

        // act
        await using var result = await executor.ExecuteAsync(
            "{ nodes { __typename id } }",
            TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "nodes": [
                  null,
                  {
                    "__typename": "Viewer",
                    "id": "2"
                  }
                ]
              }
            }
            """);
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
    public async Task CreatePlan_Should_RebindUntrustedPolicyConditions()
    {
        // arrange
        var executor = await CreateExecutorAsync(
            PolicyDenialBehavior.Null,
            new RoleRequirementPolicy());
        var schema = Assert.IsType<FusionSchemaDefinition>(executor.Schema);
        var sourcePlan = PlanOperation(schema, "{ secret }");
        var plannedPolicyNode = sourcePlan.AllNodes.OfType<PolicyExecutionNode>().Single();
        var missingCondition = new ExecutionNodeCondition
        {
            VariableName = "missing",
            PassingValue = true
        };
        plannedPolicyNode.SetTargets(
            plannedPolicyNode.Targets
                .ToArray()
                .Select(target => target with { Conditions = [missingCondition] })
                .ToArray());
        plannedPolicyNode.SetConditions([missingCondition]);

        // act
        _ = OperationPlan.Create(
            "condition-test",
            sourcePlan.Operation,
            sourcePlan.RootNodes,
            sourcePlan.AllNodes,
            sourcePlan.DeliveryGroups,
            sourcePlan.IncrementalPlans,
            sourcePlan.IncludeConditions,
            sourcePlan.PolicyExpressions,
            sourcePlan.PolicySlots,
            sourcePlan.Policies,
            searchSpace: 0,
            expandedNodes: 0);

        // assert
        Assert.Equal(
            (Node: string.Empty, Targets: string.Empty),
            (
                Node: string.Join(",", plannedPolicyNode.Conditions.ToArray()
                    .Select(condition => condition.VariableName)),
                Targets: string.Join(",", plannedPolicyNode.Targets.ToArray()
                    .SelectMany(target => target.Conditions)
                    .Select(condition => condition.VariableName))));
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
        var executor = await CreatePartialLookupExecutorAsync(
            downstreamClient,
            PolicyDenialBehavior.Error,
            new DenySecondProductPolicy(),
            listener);

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
        NormalizeReasonId(result.ToJson()).MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "path": [
                    "topProducts",
                    1
                  ],
                  "extensions": {
                    "code": "UNAUTHORIZED_FIELD_OR_TYPE",
                    "reasonId": "00000000-0000-0000-0000-000000000000"
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
    public async Task ExecuteAsync_Should_OmitPathAndNotDispatch_When_AbortDenialOccursUnderListPosition()
    {
        // arrange
        // Decision repo-1y0: an ABORT denial never carries a path, even one resolvable to a
        // real list index, since a pre-execution short-circuit (the plan-time slot path, not
        // yet implemented) cannot resolve list positions at all; this keeps the execution-time
        // PolicyExecutionNode path byte-identical to that short-circuit for the same denial.
        var downstreamClient = new RecordingLookupClient();
        var executor = await CreatePartialLookupExecutorAsync(
            downstreamClient,
            PolicyDenialBehavior.Abort,
            new DenySecondProductPolicy());

        // act
        await using var result = await executor.ExecuteAsync(
            "{ topProducts { price } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(0, downstreamClient.ExecutionCount);
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
    public async Task ExecuteAsync_Should_EmitSingleError_When_AbortDenialCoversEveryListPosition()
    {
        // arrange
        var downstreamClient = new RecordingLookupClient();
        var executor = await CreatePartialLookupExecutorAsync(
            downstreamClient,
            PolicyDenialBehavior.Abort,
            new DenyAllProductsPolicy());

        // act
        await using var result = await executor.ExecuteAsync(
            "{ topProducts { price } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(0, downstreamClient.ExecutionCount);
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
              "query Op_8924044f_2($__fusion_1_id: ID!) {\n  viewerById(id: $__fusion_1_id) {\n    name\n  }\n}\nvariables: {\"__fusion_1_id\":\"v1\"}"
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
    public async Task ExecuteAsync_Should_DispatchRetainedRegularBatchMember_When_DeferredPolicyDeniesSibling()
    {
        // arrange
        var downstreamClient = new SelectiveBatchClient();
        var listener = new ExecutionNodeStartListener();
        var executor = await CreateSelectiveBatchExecutorAsync(
            downstreamClient,
            listener,
            enableDefer: true);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ before ... @defer { topProducts { price } viewers { name } } }",
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
        Assert.Equal((ExecuteCalls: 0, BatchCalls: 1, BatchedRequests: 1), (
            ExecuteCalls: downstreamClient.ExecuteCalls,
            BatchCalls: downstreamClient.BatchCalls,
            BatchedRequests: downstreamClient.Requests.Count));
        Assert.Equal(1, listener.DownstreamBatchStarts);
        responses.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "before": "initial"
              },
              "pending": [
                {
                  "id": "0",
                  "path": []
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

    [Fact]
    public async Task ExecuteAsync_Should_DispatchRetainedApolloBatchMember_When_DeferredPolicyDeniesSibling()
    {
        // arrange
        var downstreamClient = new SelectiveApolloBatchClient();
        var listener = new ExecutionNodeStartListener();
        var executor = await CreateSelectiveApolloBatchExecutorAsync(
            downstreamClient,
            listener,
            enableDefer: true);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ before ... @defer { topProducts { price } viewers { name } } }",
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
        Assert.Equal((BatchCalls: 1, BatchedRequests: 1), (
            BatchCalls: downstreamClient.BatchCalls,
            BatchedRequests: downstreamClient.Requests.Count));
        Assert.Equal(1, listener.DownstreamBatchStarts);
        responses.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "before": "initial"
              },
              "pending": [
                {
                  "id": "0",
                  "path": []
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
    public async Task CreateDeferredPolicyOperations_Should_IncludeRegularBatchDefinitions()
    {
        // arrange
        var downstreamClient = new SelectiveBatchClient();
        var executor = await CreateSelectiveBatchExecutorAsync(downstreamClient);
        var plan = PlanOperation(
            Assert.IsType<FusionSchemaDefinition>(executor.Schema),
            "{ topProducts { price } viewers { name } }");

        // act
        var operations = DeferredPolicyOperation.Create(plan.AllNodes);

        // assert
        Assert.Equal(
            [
                DeferredPolicyOperationKind.Regular,
                DeferredPolicyOperationKind.RegularDefinition,
                DeferredPolicyOperationKind.RegularDefinition
            ],
            operations.Select(operation => operation.Kind));
    }

    [Fact]
    public async Task CreateDeferredPolicyOperations_Should_IncludeApolloBatchDefinitions()
    {
        // arrange
        var downstreamClient = new SelectiveApolloBatchClient();
        var listener = new ExecutionNodeStartListener();
        var executor = await CreateSelectiveApolloBatchExecutorAsync(downstreamClient, listener);
        var plan = PlanOperation(
            Assert.IsType<FusionSchemaDefinition>(executor.Schema),
            "{ topProducts { price } viewers { name } }");

        // act
        var operations = DeferredPolicyOperation.Create(plan.AllNodes);

        // assert
        Assert.Equal(
            [
                DeferredPolicyOperationKind.Regular,
                DeferredPolicyOperationKind.ApolloDefinition,
                DeferredPolicyOperationKind.ApolloDefinition
            ],
            operations.Select(operation => operation.Kind));
    }

    [Theory]
    [InlineData("deniedPolicyGate", true)]
    [InlineData("notSkipped", false)]
    [InlineData("clientInclude", false)]
    [InlineData("oneSkippedOneLiveDependency", true)]
    [InlineData("allDependenciesSkipped", false)]
    [InlineData("transportFailure", false)]
    [InlineData("policyAllowed", false)]
    public void ShouldMaterializeSkippedDeferredPolicyDenial_Should_RequireExclusiveDeniedGate(
        string scenario,
        bool expected)
    {
        // arrange
        var booleanType = new NonNullType(new BooleanType());
        var innerVariables = new VariableValueCollection(
            new Dictionary<string, VariableValue>
            {
                ["include"] = new("include", booleanType, BooleanValueNode.False)
            });
        var fetchGateDenyFlags = scenario.Equals("policyAllowed", StringComparison.Ordinal)
            ? 0UL
            : 1UL;
        var variables = new PolicyVariableValueCollection(
            innerVariables,
            slotCount: 1,
            liveFlags: 1,
            denyFlags: fetchGateDenyFlags,
            fetchGateDenyFlags);
        var conditions = scenario.Equals("clientInclude", StringComparison.Ordinal)
            ? new[]
            {
                new ExecutionNodeCondition { VariableName = "include", PassingValue = true }
            }
            : new[]
            {
                new ExecutionNodeCondition
                {
                    VariableName = "__fusion_policy_0",
                    PassingValue = true
                }
            };

        // act
        var dependencyCount = scenario is "oneSkippedOneLiveDependency"
            or "allDependenciesSkipped"
            or "transportFailure"
                ? 2
                : 0;
        var skippedDependencyCount = scenario is "allDependenciesSkipped" or "transportFailure"
            ? 2
            : scenario.Equals("oneSkippedOneLiveDependency", StringComparison.Ordinal)
                ? 1
                : 0;
        var actual = OperationPlanContext.ShouldMaterializeSkippedDeferredPolicyDenial(
            isSkipped: !scenario.Equals("notSkipped", StringComparison.Ordinal),
            dependencyCount,
            skippedDependencyCount,
            suppressOnlyWhenAllDependenciesAreSkipped: true,
            conditions,
            variables);

        // assert
        Assert.Equal(expected, actual);
    }

    // Replaces each correlation identifier so the remaining error shape is deterministic.
    [GeneratedRegex("""(?<="reasonId": ")[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}(?=")""")]
    private static partial Regex ReasonIdPattern();

    private static string NormalizeReasonId(string json)
    {
        var match = ReasonIdPattern().Match(json);
        Assert.True(match.Success, "Expected a well-formed extensions.reasonId GUID.");
        Assert.True(Guid.TryParse(match.Value, out _));
        return ReasonIdPattern().Replace(json, "00000000-0000-0000-0000-000000000000");
    }

    private static async Task<IRequestExecutor> CreateExecutorAsync(
        PolicyDenialBehavior behavior,
        IPolicy? policy = null,
        Func<IReadOnlyList<IPolicy>>? createPolicies = null,
        FusionExecutionDiagnosticEventListener? diagnosticListener = null,
        Action<IServiceProvider>? captureRequestServices = null,
        Func<IError, IError>? errorFilter = null,
        ISourceSchemaClient? sourceClient = null)
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
                      secret: String @policy(names: "CanReadSecret", onDenied: {{behavior.ToString().ToUpperInvariant()}})
                      role: String
                    }
                    """));

        if (policy is not null && createPolicies is not null)
        {
            throw new ArgumentException("Specify either a policy or a policy factory.");
        }

        if (policy is not null)
        {
            ConfigurePolicies(builder, new TestPolicyProvider(policy));
        }
        else if (createPolicies is not null)
        {
            ConfigurePolicies(builder, new TestPolicyProvider(createPolicies));
        }

        if (diagnosticListener is not null)
        {
            builder.AddDiagnosticEventListener(_ => diagnosticListener);
        }

        if (errorFilter is not null)
        {
            builder.AddErrorFilter(errorFilter);
        }

        builder.Services.AddSingleton<ISourceSchemaClientFactory>(
            new TestClientFactory(("a", sourceClient ?? new StaticResultClient())));

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

    private static async Task<IRequestExecutor> CreateExpressionExecutorAsync(
        string secretFieldDirectives,
        params IPolicy[] policies)
    {
        var services = new ServiceCollection();
        services.AddHttpClient();

        var builder = services.AddGraphQLGateway();
        builder.AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    $$"""
                    # name: a
                    enum PolicyDenialBehavior { NULL ERROR ABORT }

                    directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                      repeatable on OBJECT | FIELD_DEFINITION

                    type Query {
                      secret: String {{secretFieldDirectives}}
                      role: String
                    }
                    """));

        ConfigurePolicies(builder, new TestPolicyProvider(policies));
        builder.Services.AddSingleton<ISourceSchemaClientFactory>(
            new TestClientFactory(("a", new RecordingRequirementClient())));

        FusionSetupUtilities.Configure(
            builder,
            setup => setup.ClientConfigurationModifiers.Add(
                _ => new TestClientConfiguration("a")));

        return await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<IRequestExecutor> CreateTwoProductExpressionExecutorAsync(
        string productDirectives,
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
                      topProducts: [Product]
                    }

                    type Product {{productDirectives}} {
                      id: ID!
                    }
                    """));

        ConfigurePolicies(builder, new TestPolicyProvider(policies));
        builder.Services.AddSingleton<ISourceSchemaClientFactory>(
            new TestClientFactory(("a", new TwoProductResultClient())));

        FusionSetupUtilities.Configure(
            builder,
            setup => setup.ClientConfigurationModifiers.Add(
                _ => new TestClientConfiguration("a")));

        return await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<IRequestExecutor> CreateRequirementExecutorAsync(
        IPolicy policy,
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

                    directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                      repeatable on OBJECT | FIELD_DEFINITION

                    type Query {
                      secret: String @policy(names: "CanReadSecret")
                      role: String
                    }
                    """));

        ConfigurePolicies(builder, new TestPolicyProvider(policy));
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
        IPolicy policy)
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
                      secret: String @policy(names: "CanReadSecret")
                      otherSecret: String @policy(names: "CanReadSecret", onDenied: ERROR)
                    }
                    """));

        ConfigurePolicies(builder, new TestPolicyProvider(policy));
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
        IPolicy policy,
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

                    directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                      repeatable on OBJECT | FIELD_DEFINITION

                    type Query {
                      secret: String @policy(names: "CanReadSecret")
                    }
                    """,
                    """
                    # name: b
                    type Query {
                      role: String
                    }
                    """));

        ConfigurePolicies(builder, new TestPolicyProvider(policy));
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
        IPolicy? policy = null)
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
                      topProducts: [Product!]
                    }

                    type Product @key(fields: "id") @policy(names: "CanReadSecret") {
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
            new TestPolicyProvider(policy ?? new DenyPolicy()));

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
        IPolicy policy)
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

                    type Query @policy(names: "CanReadSecret") {
                      secret: String
                    }
                    """));

        ConfigurePolicies(builder, new TestPolicyProvider(policy));
        builder.Services.AddSingleton<ISourceSchemaClientFactory>(
            new TestClientFactory(("a", new StaticResultClient())));

        FusionSetupUtilities.Configure(
            builder,
            setup => setup.ClientConfigurationModifiers.Add(
                _ => new TestClientConfiguration("a")));

        return await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<IRequestExecutor> CreateNestedTypePolicyExecutorAsync(
        IPolicy policy)
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

                    type Product @policy(names: "CanReadSecret") {
                      id: ID!
                    }
                    """));

        ConfigurePolicies(builder, new TestPolicyProvider(policy));
        builder.Services.AddSingleton<ISourceSchemaClientFactory>(
            new TestClientFactory(
                ("a", new RecordingRequirementClient("""{"data":{"product":{"id":"1"}}}"""))));

        FusionSetupUtilities.Configure(
            builder,
            setup => setup.ClientConfigurationModifiers.Add(
                _ => new TestClientConfiguration("a")));

        return await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<IRequestExecutor> CreateAbstractTypePolicyExecutorAsync(
        IPolicy policy,
        string typeDefinitions,
        string response,
        FusionExecutionDiagnosticEventListener? diagnosticListener = null,
        ISourceSchemaClient? sourceClient = null)
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

                    {{typeDefinitions}}
                    """));

        ConfigurePolicies(builder, new TestPolicyProvider(policy));
        if (diagnosticListener is not null)
        {
            builder.AddDiagnosticEventListener(_ => diagnosticListener);
        }

        builder.Services.AddSingleton<ISourceSchemaClientFactory>(
            new TestClientFactory(
                ("a", sourceClient ?? new RecordingRequirementClient(response))));

        FusionSetupUtilities.Configure(
            builder,
            setup => setup.ClientConfigurationModifiers.Add(
                _ => new TestClientConfiguration("a")));

        return await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<IRequestExecutor> CreatePartialLookupExecutorAsync(
        RecordingLookupClient downstreamClient,
        PolicyDenialBehavior onDenied,
        IPolicy policy,
        FusionExecutionDiagnosticEventListener? diagnosticListener = null)
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
                      topProducts: [Product]
                    }

                    type Product @key(fields: "id")
                      @policy(names: "CanReadSecret", onDenied: {{onDenied.ToString().ToUpperInvariant()}}) {
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
            new TestPolicyProvider(policy));

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
        bool protectViewer = false,
        bool enableDefer = false)
    {
        var services = new ServiceCollection();
        services.AddHttpClient();

        var viewerPolicy = protectViewer
            ? "@policy(names: \"CanReadSecret\")"
            : string.Empty;
        var builder = services.AddGraphQLGateway();
        builder.ModifyOptions(options => options.EnableDefer = enableDefer);
        builder.AddInMemoryConfiguration(
            ComposeSchemaDocument(
                    $$"""
                    # name: a
                    enum PolicyDenialBehavior { NULL ERROR ABORT }

                    directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                      repeatable on OBJECT | FIELD_DEFINITION

                    type Query {
                      before: String
                      topProducts: [Product]
                      viewers: [Viewer]
                    }

                    type Product @key(fields: "id") @policy(names: "CanReadSecret") {
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

        ConfigurePolicies(builder, new TestPolicyProvider(new DenyPolicy()));

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
        FusionExecutionDiagnosticEventListener diagnosticListener,
        bool enableDefer = false)
    {
        var services = new ServiceCollection();
        services.AddHttpClient();

        var builder = services.AddGraphQLGateway();
        builder.ModifyOptions(options => options.EnableDefer = enableDefer);
        builder.AddInMemoryConfiguration(
            ComposeApolloPolicySchemaDocument(
                    """
                    # name: a
                    enum PolicyDenialBehavior { NULL ERROR ABORT }

                    directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                      repeatable on OBJECT | FIELD_DEFINITION

                    type Query {
                      before: String
                      topProducts: [Product]
                      viewers: [Viewer]
                    }

                    type Product @key(fields: "id") @policy(names: "CanReadSecret") {
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

        ConfigurePolicies(builder, new TestPolicyProvider(new DenyPolicy()));
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
        IPolicyProvider provider)
        => builder.ConfigureSchemaServices(
            (_, services) => services.AddSingleton(_ => provider));

    private sealed class DenyPolicy : IPolicy
    {
        public string Name => "CanReadSecret";

        public PolicyRequirements Requirements => PolicyRequirements.Empty;

        public ValueTask EvaluateAsync(
            IPolicyContext context,
            CancellationToken cancellationToken)
        {
            context.Deny(0, "denied by test policy");
            return ValueTask.CompletedTask;
        }
    }

    private sealed class NamedStaticPolicy(string name, bool deny, string? reason = null)
        : IPolicy
    {
        public string Name => name;

        public PolicyRequirements Requirements => PolicyRequirements.Empty;

        public ValueTask EvaluateAsync(
            IPolicyContext context,
            CancellationToken cancellationToken)
        {
            if (deny)
            {
                context.Deny(0, reason);
            }

            return ValueTask.CompletedTask;
        }
    }

    private sealed class NamedThrowingPolicy(string name) : IPolicy
    {
        public string Name => name;

        public PolicyRequirements Requirements => PolicyRequirements.Empty;

        public ValueTask EvaluateAsync(
            IPolicyContext context,
            CancellationToken cancellationToken)
            => throw new InvalidOperationException("policy backend unavailable");
    }

    private sealed class CountingAllowPolicy(string name) : IPolicy
    {
        private int _evaluationCount;

        public string Name => name;

        public PolicyRequirements Requirements => PolicyRequirements.Empty;

        public int EvaluationCount => Volatile.Read(ref _evaluationCount);

        public ValueTask EvaluateAsync(
            IPolicyContext context,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _evaluationCount);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class SwitchableCountingPolicy(string name) : IPolicy
    {
        private int _evaluationCount;

        public string Name => name;

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
                context.Deny(0);
            }

            return ValueTask.CompletedTask;
        }
    }

    private sealed class DenyMatchingIdPolicy(string name, string deniedId) : IPolicy
    {
        private static readonly PolicyRequirements s_requirements =
            new() { Resource = Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }") };

        public string Name => name;

        public PolicyRequirements Requirements => s_requirements;

        public ValueTask EvaluateAsync(
            IPolicyContext context,
            CancellationToken cancellationToken)
        {
            var span = context.Selection!.Entities.Span;

            for (var i = 0; i < span.Length; i++)
            {
                if (span[i].GetProperty("id").GetString() == deniedId)
                {
                    context.Deny(i, $"denied product {deniedId}");
                }
            }

            return ValueTask.CompletedTask;
        }
    }

    private sealed class CountingRequirementPolicy(string name) : IPolicy
    {
        private static readonly PolicyRequirements s_requirements =
            new() { Resource = Utf8GraphQLParser.Syntax.ParseSelectionSet("{ role }") };
        private int _evaluationCount;

        public string Name => name;

        public PolicyRequirements Requirements => s_requirements;

        public int EvaluationCount => Volatile.Read(ref _evaluationCount);

        public ValueTask EvaluateAsync(
            IPolicyContext context,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _evaluationCount);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class NamedRequirementPolicy(string name, bool deny) : IPolicy
    {
        private static readonly PolicyRequirements s_requirements =
            new() { Resource = Utf8GraphQLParser.Syntax.ParseSelectionSet("{ role }") };
        private int _evaluationCount;

        public string Name => name;

        public PolicyRequirements Requirements => s_requirements;

        public int EvaluationCount => Volatile.Read(ref _evaluationCount);

        public ValueTask EvaluateAsync(
            IPolicyContext context,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _evaluationCount);
            if (deny)
            {
                context.Deny(0, $"denied by {name}");
            }

            return ValueTask.CompletedTask;
        }
    }

    private sealed class DenyWithoutReasonPolicy : IPolicy
    {
        public string Name => "CanReadSecret";

        public PolicyRequirements Requirements => PolicyRequirements.Empty;

        public ValueTask EvaluateAsync(
            IPolicyContext context,
            CancellationToken cancellationToken)
        {
            context.Deny(0);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class CountingDenyPolicy : IPolicy
    {
        private int _evaluationCount;

        public string Name => "CanReadSecret";

        public PolicyRequirements Requirements => PolicyRequirements.Empty;

        public int EvaluationCount => Volatile.Read(ref _evaluationCount);

        public ValueTask EvaluateAsync(
            IPolicyContext context,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _evaluationCount);
            context.Deny(0, "denied by counting policy");
            return ValueTask.CompletedTask;
        }
    }

    private sealed class BlockingPolicy(string name = "CanReadSecret") : IPolicy
    {
        private int _evaluationCount;

        public string Name => name;

        public PolicyRequirements Requirements => PolicyRequirements.Empty;

        public int EvaluationCount => Volatile.Read(ref _evaluationCount);

        public TaskCompletionSource Started { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async ValueTask EvaluateAsync(
            IPolicyContext context,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _evaluationCount);
            Started.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
            context.Deny(0);
        }
    }

    private sealed class RoleRequirementPolicy : IPolicy
    {
        private static readonly PolicyRequirements s_requirements =
            new() { Resource = Utf8GraphQLParser.Syntax.ParseSelectionSet("{ role }") };

        public string Name => "CanReadSecret";

        public PolicyRequirements Requirements => s_requirements;

        public bool Evaluated { get; private set; }

        public string? Role { get; private set; }

        public ValueTask EvaluateAsync(
            IPolicyContext context,
            CancellationToken cancellationToken)
        {
            var entities = context.Selection!.Entities;
            Assert.Equal(1, entities.Length);
            Evaluated = true;
            Role = entities.Span[0].GetProperty("role").GetString();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class DenyRequirementPolicy : IPolicy
    {
        private static readonly PolicyRequirements s_requirements =
            new() { Resource = Utf8GraphQLParser.Syntax.ParseSelectionSet("{ role }") };

        public string Name => "CanReadSecret";

        public PolicyRequirements Requirements => s_requirements;

        public ValueTask EvaluateAsync(
            IPolicyContext context,
            CancellationToken cancellationToken)
        {
            for (var i = 0; i < context.Selection!.Entities.Length; i++)
            {
                context.Deny(i, "denied by requirement policy");
            }

            return ValueTask.CompletedTask;
        }
    }

    private sealed class UnknownRequirementPolicy : IPolicy
    {
        private static readonly PolicyRequirements s_requirements =
            new() { Resource = Utf8GraphQLParser.Syntax.ParseSelectionSet("{ unknown }") };

        public string Name => "CanReadSecret";

        public PolicyRequirements Requirements => s_requirements;

        public ValueTask EvaluateAsync(
            IPolicyContext context,
            CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }

    private sealed class DriftingRequirementsPolicy : IPolicy
    {
        private static readonly PolicyRequirements s_requirements =
            new() { Resource = Utf8GraphQLParser.Syntax.ParseSelectionSet("{ unknown }") };
        private int _readCount;

        public string Name => "CanReadSecret";

        public PolicyRequirements Requirements
            => Interlocked.Increment(ref _readCount) == 1
                ? PolicyRequirements.Empty
                : s_requirements;

        public ValueTask EvaluateAsync(
            IPolicyContext context,
            CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }

    private sealed class AllowPolicy : IPolicy
    {
        public string Name => "CanReadSecret";

        public PolicyRequirements Requirements => PolicyRequirements.Empty;

        public ValueTask EvaluateAsync(
            IPolicyContext context,
            CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }

    private sealed class ThrowingPolicy : IPolicy
    {
        private int _evaluationCount;

        public string Name => "CanReadSecret";

        public PolicyRequirements Requirements => PolicyRequirements.Empty;

        public int EvaluationCount => Volatile.Read(ref _evaluationCount);

        public ValueTask EvaluateAsync(
            IPolicyContext context,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _evaluationCount);
            throw new InvalidOperationException("test failure");
        }
    }

    private sealed class CooperativeCancellationPolicy(CancellationTokenSource cancellationSource)
        : IPolicy
    {
        public string Name => "CanReadSecret";

        public PolicyRequirements Requirements => PolicyRequirements.Empty;

        public bool Evaluated { get; private set; }

        public ValueTask EvaluateAsync(
            IPolicyContext context,
            CancellationToken cancellationToken)
        {
            Evaluated = true;
            cancellationSource.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException("The linked cancellation token was not canceled.");
        }
    }

    private sealed class NonCooperativeCancellationPolicy : IPolicy
    {
        public string Name => "CanReadSecret";

        public PolicyRequirements Requirements => PolicyRequirements.Empty;

        public ValueTask EvaluateAsync(
            IPolicyContext context,
            CancellationToken cancellationToken)
            => throw new OperationCanceledException("non-cooperative cancellation");
    }

    private sealed class ThrowingNamePolicy : IPolicy
    {
        public string Name => throw new InvalidOperationException("test name failure");

        public PolicyRequirements Requirements => PolicyRequirements.Empty;

        public ValueTask EvaluateAsync(
            IPolicyContext context,
            CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }

    private sealed class ThrowingRequirementsPolicy : IPolicy
    {
        public string Name => "CanReadSecret";

        public PolicyRequirements Requirements
            => throw new InvalidOperationException("test requirements failure");

        public ValueTask EvaluateAsync(
            IPolicyContext context,
            CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }

    private sealed class RootContextPolicy : IPolicy
    {
        public string Name => "CanReadSecret";

        public PolicyRequirements Requirements => PolicyRequirements.Empty;

        public bool Evaluated { get; private set; }

        public bool SelectionWasNull { get; private set; }

        public ValueTask EvaluateAsync(
            IPolicyContext context,
            CancellationToken cancellationToken)
        {
            Evaluated = true;
            SelectionWasNull = context.Selection is null;
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

        public override void RequestError(RequestContext context, Exception error)
        {
            if (!context.RequestAborted.IsCancellationRequested)
            {
                Error = error;
            }
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

    private sealed class DenySecondProductPolicy : IPolicy
    {
        private static readonly PolicyRequirements s_requirements =
            new() { Resource = Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }") };

        public string Name => "CanReadSecret";

        public PolicyRequirements Requirements => s_requirements;

        public ValueTask EvaluateAsync(
            IPolicyContext context,
            CancellationToken cancellationToken)
        {
            var span = context.Selection!.Entities.Span;

            for (var i = 0; i < span.Length; i++)
            {
                if (span[i].GetProperty("id").GetString() == "2")
                {
                    context.Deny(i, "denied product 2");
                }
            }

            return ValueTask.CompletedTask;
        }
    }

    private sealed class DenyAllProductsPolicy : IPolicy
    {
        private static readonly PolicyRequirements s_requirements =
            new() { Resource = Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }") };

        public string Name => "CanReadSecret";

        public PolicyRequirements Requirements => s_requirements;

        public ValueTask EvaluateAsync(
            IPolicyContext context,
            CancellationToken cancellationToken)
        {
            var span = context.Selection!.Entities.Span;

            for (var i = 0; i < span.Length; i++)
            {
                context.Deny(i, "denied all products");
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

        public List<string> RequestDetails { get; } = [];

        public SourceSchemaClientCapabilities Capabilities => SourceSchemaClientCapabilities.None;

        public async IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var operationSource = Encoding.UTF8.GetString(request.OperationSourceText.Value.Span);
            Requests.Add(operationSource);
            var variables = request.Variables.Length == 0
                ? "{}"
                : Encoding.UTF8.GetString(request.Variables[0].Values.AsSequence().ToArray());
            RequestDetails.Add(operationSource + "\nvariables: " + variables);
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
            Requests = Encoding.UTF8.GetString(request.OperationSourceText.Value.Span)
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
            """{"data":{"before":"initial","topProducts":[{"id":"1"}],"viewers":[{"id":"v1"}]}}"""u8.ToArray();

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

                if (request.OperationSourceText.Value.Span.IndexOf("viewerById"u8) < 0)
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
            return Encoding.UTF8.GetString(request.OperationSourceText.Value.Span)
                + "\nvariables: "
                + variables;
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
                var operationSourceText =
                    Encoding.UTF8.GetString(request.OperationSourceText.Value.Span);
                Requests.Add(operationSourceText);
                if (!operationSourceText.Contains("... on Viewer", StringComparison.Ordinal)
                    || operationSourceText.Contains("... on Product", StringComparison.Ordinal))
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
