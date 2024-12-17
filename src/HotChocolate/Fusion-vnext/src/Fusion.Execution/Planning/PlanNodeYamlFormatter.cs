using System.Buffers;
using System.Buffers.Text;
using System.Text;
using System.Text.Json;
using HotChocolate.Fusion.Planning.Nodes;

namespace HotChocolate.Fusion.Planning;

public static class PlanNodeYamlFormatter
{
    public static string ToYaml(this RequestPlanNode request)
    {
        var sb = new StringBuilder();
        var writer = new StringWriter(sb);
        Write(writer, request);
        writer.Flush();
        return sb.ToString();
    }

    public static void Write(this StringWriter writer, RequestPlanNode request)
    {
        var nodeIdLookup = CollectNodeIds(request);

        writer.WriteLine("request:");
        writer.WriteLine("  - document: >-");

        var reader = new StringReader(request.Document.ToString());
        while (true)
        {
            var line = reader.ReadLine();
            if (line is null)
            {
                break;
            }

            writer.WriteLine("      {0}", line);
        }

        if (!string.IsNullOrEmpty(request.OperationName))
        {
            writer.WriteLine("  - operationName: \"{0}\"", request.OperationName);
        }

        writer.WriteLine("nodes:");

        foreach (var (node, nodeId) in nodeIdLookup.OrderBy(t => t.Value))
        {
            WriteOperationNode(writer, node, nodeId, nodeIdLookup);
        }
    }

    private static void WriteOperationNode(
        StringWriter writer,
        OperationPlanNode operation,
        int operationId,
        Dictionary<OperationPlanNode, int> nodeIdLookup)
    {
        writer.WriteLine("  - id: {0}", operationId);
        writer.WriteLine("    schema: \"{0}\"", operation.SchemaName);
        writer.WriteLine("    operation: >-");

        var reader = new StringReader(operation.ToSyntaxNode().ToString());
        while (true)
        {
            var line = reader.ReadLine();
            if (line is null)
            {
                break;
            }

            writer.WriteLine("      {0}", line);
        }

        if (operation.SkipVariable is not null)
        {
            writer.WriteLine("    skipIf: \"{0}\"", operation.SkipVariable);
        }

        if (operation.IncludeVariable is not null)
        {
            writer.WriteLine("    includeIf: \"{0}\"", operation.IncludeVariable);
        }

        if (operation.Requirements.Count > 0)
        {
            writer.WriteLine("    requirements:");

            foreach (var requirement in operation.Requirements.Values)
            {
                writer.WriteLine("      - name: \"{0}\"", requirement.Name);
                writer.WriteLine("        dependsOn: \"{0}\"", nodeIdLookup[requirement.From]);
                writer.WriteLine("        selectionSet: \"{0}\"", requirement.SelectionSet);
                writer.WriteLine("        field: \"{0}\"", requirement.RequiredField);
                writer.WriteLine("        type: \"{0}\"", requirement.Type.ToString(false));
            }
        }
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
