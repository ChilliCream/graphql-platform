using System.Buffers;
using System.Text;
using System.Text.Json;
using HotChocolate.Fusion.Planning.Nodes;

namespace HotChocolate.Fusion.Planning;

public static class PlanNodeJsonFormatter
{
    public static string ToJson(this RequestPlanNode request)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true });
        writer.Write(request);
        writer.Flush();
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    public static void Write(this Utf8JsonWriter writer, RequestPlanNode request)
    {
        var nodeIdLookup = CollectNodeIds(request);

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

        if (operation.SkipVariable is not null)
        {
            writer.WriteString("skipIf", operation.SkipVariable);
        }

        if (operation.IncludeVariable is not null)
        {
            writer.WriteString("includeIf", operation.SkipVariable);
        }

        if (operation.DataRequirements.Count > 0)
        {
            writer.WritePropertyName("requirements");
            writer.WriteStartArray();

            foreach (var requirement in operation.DataRequirements.Values)
            {
                writer.WriteStartObject();
                writer.WriteString("name", requirement.Name);

                if (requirement.DependsOn is not null)
                {
                    writer.WritePropertyName("dependsOn");
                    writer.WriteStartArray();
                    foreach (var item in requirement.DependsOn)
                    {
                        writer.WriteNumberValue(nodeIdLookup[item]);
                    }
                    writer.WriteEndArray();
                }

                writer.WritePropertyName("selectionSet");
                writer.WriteStartArray();

                if (requirement.SelectionSet is not null)
                {
                    foreach (var segment in requirement.SelectionSet.Segments)
                    {
                        writer.WriteStringValue(segment.Name);
                    }
                }

                writer.WriteEndArray();

                writer.WritePropertyName("field");
                writer.WriteStartArray();

                foreach (var segment in requirement.RequiredField.Segments)
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
            if (node is RequestPlanNode rootPlanNode)
            {
                foreach (var child in rootPlanNode.Operations)
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

                foreach (var child in operationPlanNode.Dependants)
                {
                    backlog.Enqueue(child);
                }
            }
        }

        return nodeIdLookup;
    }
}
