using System.Text;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public sealed class YamlExecutionPlanFormatter : ExecutionPlanFormatter
{
    public override string Format(OperationExecutionPlan plan)
    {
        var sb = new StringBuilder();
        var writer = new CodeWriter(sb);

        WriteOperation(plan.Operation.Definition, writer);

        writer.WriteLine("nodes:");
        writer.Indent();

        foreach (var node in plan.AllNodes)
        {
            if (node is OperationExecutionNode operationNode)
            {
                WriteNode(operationNode, writer);
            }
        }

        return sb.ToString();
    }

    private static void WriteNode(OperationExecutionNode node, CodeWriter writer)
    {
        writer.WriteLine("- id: " + node.Id);
        writer.Indent();
        writer.WriteLine("schema: " + node.SchemaName);

        WriteOperation(node.Operation, writer);

        if (node.Requirements.Length > 0)
        {
            writer.WriteLine("requirements:");
            writer.Indent();
            foreach (var requirement in node.Requirements.ToArray().OrderBy(t => t.Key))
            {
                writer.WriteLine("- name: " + requirement.Key);
                writer.Indent();
                writer.WriteLine("selectionSet: " + requirement.Path);
                writer.WriteLine("selectionMap: " + requirement.Map);
                writer.Unindent();
            }

            writer.Unindent();
        }

        if (node.Dependencies.Length > 0)
        {
            writer.WriteLine("dependencies:");
            writer.Indent();
            foreach (var dependency in node.Dependencies.ToArray().OrderBy(t => t.Id))
            {
                writer.WriteLine("- id: " + dependency.Id);
            }

            writer.Unindent();
        }

        writer.Unindent();
    }

    private static void WriteOperation(OperationDefinitionNode operation, CodeWriter writer)
    {
        writer.WriteLine("operation: >-");
        writer.Indent();
        var reader = new StringReader(operation.ToString());
        var line = reader.ReadLine();
        while (line != null)
        {
            writer.WriteLine(line);
            line = reader.ReadLine();
        }
        writer.Unindent();
    }
}
