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
}
