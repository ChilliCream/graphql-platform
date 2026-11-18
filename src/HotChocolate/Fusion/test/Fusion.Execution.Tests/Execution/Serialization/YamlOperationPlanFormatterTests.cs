using System.Buffers;
using System.Collections.Immutable;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Nodes.Serialization;
using HotChocolate.Fusion.Text.Json;

namespace HotChocolate.Fusion.Execution.Serialization;

public class YamlOperationPlanFormatterTests : FusionTestBase
{
    [Fact]
    public void Format_Should_IncludeVariableSetsAndTransport_When_NodeHasTrace()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: products
            type Query { product(id: ID!): String }
            """);
        var plan = PlanOperation(schema, "query Product($id: ID!) { product(id: $id) }");
        var node = Assert.Single(plan.AllNodes);
        var variables = JsonSegment.Create(new ReadOnlySequence<byte>("""{"id":"123"}"""u8.ToArray()));
        var nodeTrace = new ExecutionNodeTrace
        {
            Id = node.Id,
            Status = ExecutionStatus.Success,
            Duration = TimeSpan.FromMilliseconds(1.5),
            VariableSets = [new VariableValues(CompactPath.Root, variables)],
            Transport = new ExecutionNodeTransportTrace
            {
                Uri = new Uri("https://products.example/graphql"),
                ContentType = "application/graphql-response+json"
            }
        };
        var trace = new OperationPlanTrace
        {
            Duration = nodeTrace.Duration,
            Nodes = ImmutableDictionary<int, ExecutionNodeTrace>.Empty.Add(node.Id, nodeTrace)
        };

        // act
        var yaml = new YamlOperationPlanFormatter().Format(plan, trace);

        // assert
        yaml.MatchSnapshot(extension: ".yaml");
    }
}
