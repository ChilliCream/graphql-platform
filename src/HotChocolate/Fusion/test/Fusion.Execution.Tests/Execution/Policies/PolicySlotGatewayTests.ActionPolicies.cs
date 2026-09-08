using System.Collections.Concurrent;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Fusion.Execution;

public sealed partial class PolicySlotGatewayTests
{
    [Fact]
    public async Task ExecuteAsync_Should_DenyOnlyMatchingAlias_When_ActionPolicyReadsArguments()
    {
        // arrange
        var client = new RecordingClient("""{"data":{"b":"changed"}}""");
        var policy = new DenyOnArgumentPolicy("CanWrite", "value", "1");
        var executor = await CreateExecutorAsync(
            CreateSchema(
                """
                type Query { placeholder: String }
                type Mutation {
                  write(value: Int!): String @policy(names: "CanWrite", onDenied: NULL)
                }
                """),
            policy,
            client);

        // act
        await using var result = await executor.ExecuteAsync(
            "mutation { a: write(value: 1) b: write(value: 2) }",
            TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "a": null,
                "b": "changed"
              }
            }
            """);
        Assert.Equal(1, client.ExecuteCount);
    }

    [Fact]
    public async Task ExecuteAsync_Should_NotInvokeSource_When_ActionPolicyDeniesMutation()
    {
        // arrange
        var client = new RecordingClient("""{"data":{"write":"changed"}}""");
        var policy = new DenyOnArgumentPolicy("CanWrite", "value", "1");
        var executor = await CreateExecutorAsync(
            CreateSchema(
                """
                type Query { placeholder: String }
                type Mutation {
                  write(value: Int!): String @policy(names: "CanWrite", onDenied: NULL)
                }
                """),
            policy,
            client);

        // act
        await using var denied = await executor.ExecuteAsync(
            "mutation { write(value: 1) }",
            TestContext.Current.CancellationToken);
        await using var allowed = await executor.ExecuteAsync(
            "mutation { write(value: 2) }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, client.ExecuteCount);
        denied.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "write": null
              }
            }
            """);
        allowed.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "write": "changed"
              }
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_IsolateVariableBatchItems_When_ActionPolicyDependsOnArguments()
    {
        // arrange
        var client = new RecordingClient("""{"data":{"write":"changed"}}""");
        var policy = new DenyOnArgumentPolicy("CanWrite", "value", "1");
        var executor = await CreateExecutorAsync(
            CreateSchema(
                """
                type Query { placeholder: String }
                type Mutation {
                  write(value: Int!): String @policy(names: "CanWrite", onDenied: NULL)
                }
                """),
            policy,
            client);
        using var variableValues = JsonDocument.Parse("""[{"value": 1}, {"value": 2}]""");
        var request = VariableBatchRequest.FromSourceText(
            "mutation($value: Int!) { write(value: $value) }",
            variableValues);

        // act
        await using var result = await executor.ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);
        var batch = Assert.IsType<OperationResultBatch>(result);
        var results = batch.Results.Select(current => current.ToJson()).ToArray();

        // assert
        Assert.Equal(1, client.ExecuteCount);
        results.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "write": null
              }
            }
            """,
            """
            {
              "data": {
                "write": "changed"
              }
            }
            """
        ]);
    }

    [Fact]
    public async Task ExecuteAsync_Should_CoerceArgumentsInDefinitionOrder_When_ActionPolicyReadsAction()
    {
        // arrange
        var client = new RecordingClient("""{"data":{"write":"changed"}}""");
        var capture = new CapturingActionPolicy("CanWrite");
        var executor = await CreateExecutorAsync(
            CreateSchema(
                """
                type Query { placeholder: String }
                input FilterInput {
                  flag: Boolean = true
                  label: String
                }
                type Mutation {
                  write(
                    label: String
                    id: Int!
                    filter: FilterInput
                  ): String @policy(names: "CanWrite", onDenied: NULL)
                }
                """),
            capture,
            client);

        // act
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "mutation($theId: Int!) { write(id: $theId, filter: { label: \"nested\" }) }")
                .SetVariableValues(new Dictionary<string, object?> { ["theId"] = 7 })
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, client.ExecuteCount);
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "write": "changed"
              }
            }
            """);
        Assert.Equal("Mutation.write", capture.LastAction!.Name);
        capture.LastAction.Arguments.Print(indented: false).MatchInlineSnapshot(
            """
            { filter: { flag: true, label: "nested" }, id: 7 }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_KeepArgumentOrderStable_When_QueryOrdersArgumentsDifferently()
    {
        // arrange: ruling 700's canonical order is the field's own argument-definition order,
        // never the order the client wrote the arguments in the query. This query writes "filter"
        // before "id", the opposite of the textual order used by the definition-order test above;
        // the envelope must still come out identical.
        var client = new RecordingClient("""{"data":{"write":"changed"}}""");
        var capture = new CapturingActionPolicy("CanWrite");
        var executor = await CreateExecutorAsync(
            CreateSchema(
                """
                type Query { placeholder: String }
                input FilterInput {
                  flag: Boolean = true
                  label: String
                }
                type Mutation {
                  write(
                    label: String
                    id: Int!
                    filter: FilterInput
                  ): String @policy(names: "CanWrite", onDenied: NULL)
                }
                """),
            capture,
            client);

        // act
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "mutation($theId: Int!) { write(filter: { label: \"nested\" }, id: $theId) }")
                .SetVariableValues(new Dictionary<string, object?> { ["theId"] = 7 })
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, client.ExecuteCount);
        Assert.Equal("Mutation.write", capture.LastAction!.Name);
        capture.LastAction.Arguments.Print(indented: false).MatchInlineSnapshot(
            """
            { filter: { flag: true, label: "nested" }, id: 7 }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_RouteEachNameByKind_When_ExpressionMixesActionAndRequestConstant()
    {
        // arrange: ruling 700 / F2 - one @policy expression mixing an ActionOccurrence-kind name
        // with a RequestConstant-kind name must evaluate ONLY the action-kind name through the
        // action path; the request-constant name keeps its own normal, cached evaluation (its
        // context never sees an Action) regardless of appearing in the same expression. Query
        // fields (not mutations) keep both aliases inside a single gate-evaluation pass, so a
        // request-constant decision cached there is not disturbed by serial-mutation-root
        // re-evaluation, which is unrelated to this concern.
        var client = new RecordingClient("""{"data":{"a":"changed","b":"changed"}}""");
        var actionPolicy = new CountingActionPolicy("CanReadAction");
        var constPolicy = new CapturingConstPolicy("AlwaysAllow");
        var executor = await CreateExecutorAsync(
            CreateSchema(
                """
                type Query {
                  read(value: Int!): String
                    @policy(names: [["CanReadAction", "AlwaysAllow"]], onDenied: NULL)
                }
                """),
            [actionPolicy, constPolicy],
            client);

        // act: two aliased occurrences so the action name is re-evaluated per occurrence while the
        // request-constant name's decision is reused (cached) across both.
        await using var result = await executor.ExecuteAsync(
            "{ a: read(value: 1) b: read(value: 2) }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, client.ExecuteCount);
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "a": "changed",
                "b": "changed"
              }
            }
            """);
        Assert.Equal(2, actionPolicy.EvaluationCount);
        Assert.Equal(1, constPolicy.EvaluationCount);
        Assert.False(constPolicy.LastSawSelection);
        Assert.False(constPolicy.LastSawAction);
    }

    [Fact]
    public async Task ExecuteAsync_Should_RejectPlan_When_ActionPolicyIsReachedThroughInlineFragment()
    {
        // arrange: F1 (ruling 700 / eva m-46l4gm) - a coordinate reached only through a
        // concrete-type inline fragment under an abstractly typed selection cannot carry a fetch
        // gate today, so an action policy on it must never be fetched then denied. The plan is
        // rejected outright instead (fail closed), and the source is never invoked.
        var client = new RecordingClient("""{"data":{"node":{"__typename":"Product","id":"1"}}}""");
        var policy = new CountingActionPolicy("CanCancel");
        var executor = await CreateExecutorAsync(
            CreateSchema(
                """
                type Query { node: Node }
                interface Node { id: ID! }
                type Product implements Node {
                  id: ID!
                  cancel(reason: String): Boolean @policy(names: "CanCancel", onDenied: NULL)
                }
                type Book implements Node { id: ID! }
                """),
            policy,
            client);

        // act: the plan-time rejection is an InvalidOperationException raised while building the
        // operation plan; the gateway's own exception handling turns it into a generic execution
        // error rather than letting it escape ExecuteAsync, exactly like any other internal plan
        // validation failure (ThrowHelper.InvalidOperationPlan) already does elsewhere.
        await using var result = await executor.ExecuteAsync(
            """{ node { id ... on Product { cancel(reason: "x") } } }""",
            TestContext.Current.CancellationToken);

        // assert: the plan is rejected and the source is never invoked, so a denied mutation/field
        // is never fetched even when it cannot be gated.
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Unexpected Execution Error"
                }
              ]
            }
            """);
        Assert.Equal(0, client.ExecuteCount);
        Assert.Equal(0, policy.EvaluationCount);
    }

    [Fact]
    public async Task ExecuteAsync_Should_DenyOnlyMatchingParent_When_SameFieldSharesResponseNameAcrossParents()
    {
        // arrange: u1.info and u2.info are distinct compiled occurrences that both materialize
        // under the response name "info"; an action policy's decision depends on its own
        // occurrence's arguments, so denying u1 must never bleed into u2's decision.
        var client = new RecordingClient(
            """{"data":{"u1":{"info":"secretA"},"u2":{"info":"secretB"}}}""");
        var policy = new DenyOnArgumentPolicy("CanRead", "scope", "\"a\"");
        var executor = await CreateExecutorAsync(
            CreateSchema(
                """
                type Query {
                  u1: User
                  u2: User
                }
                type User {
                  info(scope: String): String @policy(names: "CanRead", onDenied: NULL)
                }
                """),
            policy,
            client);

        // act
        await using var result = await executor.ExecuteAsync(
            """{ u1 { info(scope: "a") } u2 { info(scope: "b") } }""",
            TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "u1": {
                  "info": null
                },
                "u2": {
                  "info": "secretB"
                }
              }
            }
            """);
        Assert.Equal(1, client.ExecuteCount);
    }

    [Fact]
    public async Task ExecuteAsync_Should_EvaluateActionPolicyBeforeInitialPayload_When_DeferredFieldIsDenied()
    {
        // arrange
        var client = new RecordingClient(
            request => request.OperationSourceText.Value.Span.IndexOf("immediate"u8) >= 0
                ? """{"data":{"immediate":"initial"}}"""
                : """{"data":{"secret":"classified"}}"""
        );
        var policy = new DenyOnArgumentPolicy("CanReadSecret", "scope", "\"a\"");
        var executor = await CreateExecutorAsync(
            CreateSchema(
                """
                type Query {
                  immediate: String
                  secret(scope: String): String @policy(names: "CanReadSecret", onDenied: NULL)
                }
                """),
            policy,
            client,
            enableDefer: true);

        // act
        await using var result = await executor.ExecuteAsync(
            """
            query {
              immediate
              ... @defer {
                secret(scope: "a")
              }
            }
            """,
            TestContext.Current.CancellationToken);
        await using var stream = result.ExpectResponseStream();
        var responses = new List<string>();

        await foreach (var response in stream
            .ReadResultsAsync()
            .WithCancellation(TestContext.Current.CancellationToken))
        {
            responses.Add(response.ToJson());
        }

        // assert: the action policy is evaluated before the initial payload is produced (a denied
        // deferred field's fetch never runs), so the source is invoked only once for "immediate".
        Assert.Equal(1, client.ExecuteCount);
        responses.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "immediate": "initial"
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
    }

    [Fact]
    public async Task SubscribeAsync_Should_NotSubscribeSource_When_ActionPolicyDeniesArgument()
    {
        // arrange
        var client = new RecordingClient(
            """{"data":{"placeholder":null}}""",
            """{"data":{"onMessage":"classified"}}"""
        );
        var policy = new DenyOnArgumentPolicy("CanReadMessage", "scope", "\"a\"");
        var executor = await CreateExecutorAsync(
            CreateSchema(
                """
                type Query { placeholder: String }
                type Subscription {
                  onMessage(scope: String): String @policy(names: "CanReadMessage", onDenied: NULL)
                }
                """),
            policy,
            client);

        // act
        await using var result = await executor.ExecuteAsync(
            """subscription { onMessage(scope: "a") }""",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(0, client.SubscribeCount);
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
    public async Task SubscribeAsync_Should_DeliverEvents_When_ActionPolicyAllowsArgument()
    {
        // arrange
        var client = new RecordingClient(
            """{"data":{"placeholder":null}}""",
            """{"data":{"onMessage":"visible"}}"""
        );
        var policy = new DenyOnArgumentPolicy("CanReadMessage", "scope", "\"a\"");
        var executor = await CreateExecutorAsync(
            CreateSchema(
                """
                type Query { placeholder: String }
                type Subscription {
                  onMessage(scope: String): String @policy(names: "CanReadMessage", onDenied: NULL)
                }
                """),
            policy,
            client);

        // act
        await using var result = await executor.ExecuteAsync(
            """subscription { onMessage(scope: "b") }""",
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
        Assert.Equal(1, client.SubscribeCount);
        responses.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "onMessage": "visible"
              }
            }
            """
        ]);
    }

    [Fact]
    public async Task SubscribeAsync_Should_EvaluateActionPolicyOnce_When_MultipleEventsAreDelivered()
    {
        // arrange: ruling 700 requires a subscription-root action policy to be evaluated once
        // before stream setup, independent of ctf.11's per-event resource re-evaluation.
        var policy = new CountingActionPolicy("CanReadMessage");
        var client = new RecordingClient(
            """{"data":{"placeholder":null}}""",
            [
                () => """{"data":{"onMessage":"first"}}""",
                () => """{"data":{"onMessage":"second"}}"""
            ]);
        var executor = await CreateExecutorAsync(
            CreateSchema(
                """
                type Query { placeholder: String }
                type Subscription {
                  onMessage(scope: String): String @policy(names: "CanReadMessage", onDenied: NULL)
                }
                """),
            policy,
            client);

        // act
        await using var result = await executor.ExecuteAsync(
            """subscription { onMessage(scope: "b") }""",
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
        Assert.Equal(2, responses.Count);
        Assert.Equal(1, policy.EvaluationCount);
    }

    private sealed class DenyOnArgumentPolicy(string name, string argumentName, string deniedRawValue) : IPolicy
    {
        public string Name => name;

        public PolicyRequirements Requirements { get; } =
            new() { Kind = PolicyEvaluationKind.ActionOccurrence };

        public ValueTask EvaluateAsync(IPolicyContext context, CancellationToken cancellationToken)
        {
            var argument = context.Action!.Arguments.Fields
                .FirstOrDefault(field => field.Name.Value.Equals(argumentName, StringComparison.Ordinal));

            if (argument is { Value: IValueNode value } && value.Print(indented: false) == deniedRawValue)
            {
                context.Deny(0, "denied by test action policy");
            }

            return ValueTask.CompletedTask;
        }
    }

    private sealed class CapturingActionPolicy(string name) : IPolicy
    {
        private readonly ConcurrentBag<PolicyAction> _actions = [];

        public string Name => name;

        public PolicyRequirements Requirements { get; } =
            new() { Kind = PolicyEvaluationKind.ActionOccurrence };

        public PolicyAction? LastAction => _actions.LastOrDefault();

        public ValueTask EvaluateAsync(IPolicyContext context, CancellationToken cancellationToken)
        {
            _actions.Add(context.Action!);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class CapturingConstPolicy(string name) : IPolicy
    {
        private int _evaluationCount;

        public string Name => name;

        public PolicyRequirements Requirements { get; } = PolicyRequirements.Empty;

        public int EvaluationCount => Volatile.Read(ref _evaluationCount);

        public bool LastSawSelection { get; private set; }

        public bool LastSawAction { get; private set; }

        public ValueTask EvaluateAsync(IPolicyContext context, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _evaluationCount);
            LastSawSelection = context.Selection is not null;
            LastSawAction = context.Action is not null;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class CountingActionPolicy(string name) : IPolicy
    {
        private int _evaluationCount;

        public string Name => name;

        public PolicyRequirements Requirements { get; } =
            new() { Kind = PolicyEvaluationKind.ActionOccurrence };

        public int EvaluationCount => Volatile.Read(ref _evaluationCount);

        public ValueTask EvaluateAsync(IPolicyContext context, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _evaluationCount);
            return ValueTask.CompletedTask;
        }
    }
}
