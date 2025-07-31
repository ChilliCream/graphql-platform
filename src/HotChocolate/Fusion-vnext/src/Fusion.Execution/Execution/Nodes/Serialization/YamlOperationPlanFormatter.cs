using System.Text;

namespace HotChocolate.Fusion.Execution.Nodes.Serialization;

public sealed class YamlOperationPlanFormatter : OperationPlanFormatter
{
    public override string Format(OperationPlan plan, OperationPlanTrace? trace = null)
    {
        var sb = new StringBuilder();
        var writer = new CodeWriter(sb);

        WriteOperation(plan, trace, writer);

        writer.WriteLine("nodes:");
        writer.Indent();

        foreach (var node in plan.AllNodes)
        {
            ExecutionNodeTrace? nodeTrace = null;
            trace?.Nodes.TryGetValue(node.Id, out nodeTrace);

            switch (node)
            {
                case OperationExecutionNode operationNode:
                    WriteOperationNode(operationNode, nodeTrace, writer);
                    break;

                case IntrospectionExecutionNode introspectionNode:
                    WriteIntrospectionNode(introspectionNode, nodeTrace, writer);
                    break;
            }
        }

        return sb.ToString();
    }

    private static void WriteOperationNode(OperationExecutionNode node, ExecutionNodeTrace? trace, CodeWriter writer)
    {
        writer.WriteLine("- id: {0}", node.Id);
        writer.Indent();
        writer.WriteLine("schema: {0}", node.SchemaName);

        writer.WriteLine("operation: >-");
        writer.Indent();
        var reader = new StringReader(node.Operation.ToString(indented: true));
        var line = reader.ReadLine();
        while (line != null)
        {
            writer.WriteLine(line);
            line = reader.ReadLine();
        }
        writer.Unindent();

        if (!node.Source.IsRoot)
        {
            writer.WriteLine("source: {0}", node.Source.ToString());
        }

        if (!node.Target.IsRoot)
        {
            writer.WriteLine("target: {0}", node.Target.ToString());
        }

        if (node.Requirements.Length > 0)
        {
            writer.WriteLine("requirements:");
            writer.Indent();
            foreach (var requirement in node.Requirements.ToArray().OrderBy(t => t.Key))
            {
                writer.WriteLine("- name: {0}",  requirement.Key);
                writer.Indent();
                writer.WriteLine("selectionMap: {0}", requirement.Map);
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
                writer.WriteLine("- id: {0}", dependency.Id);
            }

            writer.Unindent();
        }

        if (trace is not null)
        {
            if (trace.SpanId is not null)
            {
                writer.WriteLine("spanId: {0}", trace.SpanId);
            }

            writer.WriteLine("duration: {0}", trace.Duration.TotalMilliseconds);
            writer.WriteLine("status: {0}", trace.Status.ToString());
        }

        writer.Unindent();
    }

    private static void WriteOperation(
        OperationPlan plan,
        OperationPlanTrace? trace,
        CodeWriter writer)
    {
        writer.WriteLine("operation:");
        writer.Indent();

        writer.WriteLine("- document: >-");
        writer.Indent();
        var reader = new StringReader(plan.Operation.Definition.ToString(indented: true));
        var line = reader.ReadLine();
        while (line != null)
        {
            writer.WriteLine(line);
            line = reader.ReadLine();
        }

        if (!string.IsNullOrEmpty(plan.Operation.Name))
        {
            writer.WriteLine("name: {0}", plan.Operation.Name);
        }

        writer.WriteLine("hash: {0}", plan.Operation.Id);

        if (trace is not null)
        {
            if (!string.IsNullOrEmpty(trace.AppId))
            {
                writer.WriteLine("appId: {0}", trace.AppId);
            }

            if (!string.IsNullOrEmpty(trace.EnvironmentName))
            {
                writer.WriteLine("environment: {0}", trace.EnvironmentName);
            }

            if (!string.IsNullOrEmpty(trace.TraceId))
            {
                writer.WriteLine("traceId: {0}", trace.TraceId);
            }

            writer.WriteLine("duration: {0}", trace.Duration.TotalMilliseconds);
        }

        writer.Unindent();
        writer.Unindent();
    }

    private static void WriteIntrospectionNode(IntrospectionExecutionNode node, ExecutionNodeTrace? trace, CodeWriter writer)
    {
        writer.WriteLine("- id: {0}", node.Id);
        writer.Indent();

        writer.WriteLine("selections:");
        writer.Indent();
        foreach (var selection in node.Selections)
        {
            writer.WriteLine("- id: {0}", selection.Id);
            writer.Indent();
            writer.WriteLine("responseName: {0}", selection.ResponseName);
            writer.WriteLine("fieldName: {0}", selection.Field.Name);
            writer.Unindent();
        }
        writer.Unindent();

        if (trace is not null)
        {
            if (trace.SpanId is not null)
            {
                writer.WriteLine("spanId: {0}", trace.SpanId);
            }

            writer.WriteLine("duration: {0}", trace.Duration.TotalMilliseconds);
            writer.WriteLine("status: {0}", trace.Status.ToString());
        }

        writer.Unindent();
    }
}
