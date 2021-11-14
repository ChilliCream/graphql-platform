using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using static HotChocolate.Execution.Processing.Plan.QueryPlanSerializationProperties;

namespace HotChocolate.Execution.Processing.Plan;

internal sealed class ParallelNode : QueryPlanNode
{
    private const string _name = "Parallel";

    public ParallelNode() : base(ExecutionStrategy.Parallel)
    {
    }

    public override ExecutionStep CreateStep() =>
        new ParallelStep(Nodes.Select(t => t.CreateStep()).ToArray());

    public override void Serialize(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString(TypeProp, _name);

        writer.WritePropertyName(NodesProp);
        writer.WriteStartArray();
        foreach (QueryPlanNode? node in Nodes)
        {
            node.Serialize(writer);
        }
        writer.WriteEndArray();

        writer.WriteEndObject();
    }

    public override object Serialize()
    {
        return new Dictionary<string, object?>
            {
                { TypeProp, _name },
                { NodesProp, Nodes.Select(t => t.Serialize()).ToArray() }
            };
    }

    public static ParallelNode Create(params QueryPlanNode[] nodes)
    {
        var parallel = new ParallelNode();
        foreach (QueryPlanNode node in nodes)
        {
            parallel.AddNode(node);
        }
        return parallel;
    }
}
