using System.Text;

namespace HotChocolate.Fusion.Planning;

public abstract class ExecutionPlanFormatter
{
    public abstract string Format(ExecutionPlan plan);
}

public sealed class YamlExecutionPlanFormatter : ExecutionPlanFormatter
{
    public override string Format(ExecutionPlan plan)
    {
        var sb = new StringBuilder();
        var writer = new CodeWriter(sb);

        writer.WriteLine("nodes:");
        writer.Indent();

        foreach (var node in plan.AllNodes)
        {
            if(node is OperationExecutionNode operationNode)
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
        writer.WriteLine("operation: >-");
        writer.Indent();
        writer.WriteLine(node.Definition.ToString());
        writer.Unindent();

        writer.WriteLine("requirements:");
        writer.Indent();
        foreach (var requirement in node.Requirements)
        {
            writer.WriteLine("- name: " + requirement.Key);
            writer.Indent();
            writer.WriteLine("selectionSet: " + requirement.Path);
            writer.WriteLine("selectionMap: " + requirement.Map);
            writer.Unindent();
        }
        writer.Unindent();

        writer.WriteLine("dependencies:");
        writer.Indent();
        foreach (var dependency in node.Dependencies)
        {
            writer.WriteLine("- id: " + dependency.Id);
        }
        writer.Unindent();
        writer.Unindent();
    }
}

internal sealed class CodeWriter(StringBuilder sb)
{
    private int indent = 0;

    public void Indent() => indent++;

    public void Unindent() => indent--;

    public void WriteLine(string line)
    {
        for (var i = 0; i < indent; i++)
        {
            sb.Append("  ");
        }

        sb.AppendLine(line);
    }

    public void Write(string s)
    {
        sb.Append(s);
    }

}
