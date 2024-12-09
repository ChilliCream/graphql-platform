using System.Buffers;
using System.Text;
using System.Text.Json;

namespace HotChocolate.Fusion.Planning.Nodes;

public static class PlanNodeJsonFormatter
{
    public static string ToJson(this RootPlanNode root)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true });
        writer.Write(root);
        writer.Flush();
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    public static void Write(this Utf8JsonWriter writer, RootPlanNode root)
    {
        var nodeIdLookup = CollectNodeIds(root);

        writer.WriteStartObject();

        writer.WritePropertyName("nodes");
        writer.WriteStartArray();

        foreach (var (node, nodeId) in nodeIdLookup.OrderBy(t => t.Value))
        {
            WriteOperationNode(writer, node, nodeId, nodeIdLookup);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WriteOperationNode(
        Utf8JsonWriter writer,
        OperationPlanNode operation,
        int operationId,
        Dictionary<OperationPlanNode, int> nodeIdLookup)
    {
        writer.WriteStartObject();
        writer.WriteNumber("id", operationId);
        writer.WriteString("schema", operation.SchemaName);
        writer.WriteString("operation", operation.ToSyntaxNode().ToString(false));

        if (operation.Requirements.Count > 0)
        {
            writer.WritePropertyName("requirements");
            writer.WriteStartArray();

            foreach (var requirement in operation.Requirements.Values)
            {
                writer.WriteStartObject();
                writer.WriteString("name", requirement.Name);
                writer.WriteNumber("dependsOn", nodeIdLookup[requirement.From]);

                writer.WritePropertyName("field");
                writer.WriteStartArray();

                foreach (var segment in requirement.SelectionSet.Reverse())
                {
                    writer.WriteStringValue(segment.Name);
                }

                writer.WriteEndArray();

                writer.WriteString("type", requirement.Type.ToString(false));
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    private static Dictionary<OperationPlanNode, int> CollectNodeIds(PlanNode root)
    {
        var nextId = 1;
        var nodeIdLookup = new Dictionary<OperationPlanNode, int>();
        var backlog = new Queue<PlanNode>();
        backlog.Enqueue(root);

        while (backlog.TryDequeue(out var node))
        {
            if (node is RootPlanNode rootPlanNode)
            {
                foreach (var child in rootPlanNode.Nodes)
                {
                    backlog.Enqueue(child);
                }
            }
            else if (node is OperationPlanNode operationPlanNode)
            {
                if (!nodeIdLookup.ContainsKey(operationPlanNode))
                {
                    nodeIdLookup.Add(operationPlanNode, nextId++);
                }

                foreach (var child in operationPlanNode.Nodes)
                {
                    backlog.Enqueue(child);
                }
            }
            else if (node is ConditionPlanNode conditionPlanNode)
            {
                foreach (var child in conditionPlanNode.Nodes)
                {
                    backlog.Enqueue(child);
                }
            }
        }

        return nodeIdLookup;
    }
}
