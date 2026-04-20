using System.Text;

namespace HotChocolate.Fusion.Execution.Nodes.Serialization;

/// <summary>
/// Formats an <see cref="OperationPlan"/> as a YAML document.
/// This formatter is intended for testing purposes and is primarily used
/// to produce human-readable test snapshots.
/// </summary>
public sealed class YamlOperationPlanFormatter : OperationPlanFormatter
{
    /// <inheritdoc />
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

            WriteNode(node, nodeTrace, writer);
        }

        writer.Unindent();

        if (!plan.DeferredGroups.IsDefaultOrEmpty)
        {
            writer.WriteLine("deferredGroups:");
            writer.Indent();

            foreach (var group in plan.DeferredGroups)
            {
                WriteDeferredGroup(group, writer);
            }

            writer.Unindent();
        }

        return sb.ToString();
    }

    private static void WriteNode(ExecutionNode node, ExecutionNodeTrace? nodeTrace, CodeWriter writer)
    {
        switch (node)
        {
            case OperationExecutionNode operationNode:
                WriteOperationNode(operationNode, nodeTrace, writer);
                break;

            case OperationBatchExecutionNode batchNode:
                WriteBatchExecutionNode(batchNode, nodeTrace, writer);
                break;

            case IntrospectionExecutionNode introspectionNode:
                WriteIntrospectionNode(introspectionNode, nodeTrace, writer);
                break;

            case NodeFieldExecutionNode nodeExecutionNode:
                WriteNodeFieldNode(nodeExecutionNode, nodeTrace, writer);
                break;
        }
    }

    private static void WriteDeferredGroup(DeferredExecutionGroup group, CodeWriter writer)
    {
        writer.WriteLine("- id: {0}", group.DeferId);
        writer.Indent();

        if (group.Label is not null)
        {
            writer.WriteLine("label: {0}", group.Label);
        }

        writer.WriteLine("path: {0}", group.Path.ToString());

        if (group.IfVariable is not null)
        {
            writer.WriteLine("ifVariable: ${0}", group.IfVariable);
        }

        if (group.Parent is not null)
        {
            writer.WriteLine("parentId: {0}", group.Parent.DeferId);
        }

        writer.WriteLine("parentNodeId: {0}", group.ParentNodeId);

        if (!group.AllNodes.IsDefaultOrEmpty)
        {
            writer.WriteLine("nodes:");
            writer.Indent();

            foreach (var node in group.AllNodes)
            {
                WriteNode(node, nodeTrace: null, writer);
            }

            writer.Unindent();
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

        writer.WriteLine("- document: |");
        writer.Indent();
        writer.Indent();
        var reader = new StringReader(plan.Operation.Definition.ToString(indented: true));
        var line = reader.ReadLine();
        while (line != null)
        {
            writer.WriteLine(line);
            line = reader.ReadLine();
        }
        writer.Unindent();

        if (!string.IsNullOrEmpty(plan.Operation.Name))
        {
            writer.WriteLine("name: {0}", plan.Operation.Name);
        }

        writer.WriteLine("hash: {0}", plan.Operation.Id);
        writer.WriteLine("searchSpace: {0}", plan.SearchSpace);
        writer.WriteLine("expandedNodes: {0}", plan.ExpandedNodes);

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

    private static void WriteOperationNode(OperationExecutionNode node, ExecutionNodeTrace? trace, CodeWriter writer)
    {
        writer.WriteLine("- id: {0}", node.Id);
        writer.Indent();

        writer.WriteLine("type: {0}", "Operation");

        if (node.SchemaName is not null)
        {
            writer.WriteLine("schema: {0}", node.SchemaName);
        }

        writer.WriteLine("operation: |");
        writer.Indent();
        var reader = new StringReader(node.Operation.SourceText);
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
                writer.WriteLine("- name: {0}", requirement.Key);
                writer.Indent();

                writer.WriteLine("selectionMap: >-");
                writer.Indent();
                var selectionMapReader = new StringReader(requirement.Map.ToString(indented: true));
                var selectionMapLine = selectionMapReader.ReadLine();
                while (selectionMapLine != null)
                {
                    writer.WriteLine(selectionMapLine);
                    selectionMapLine = selectionMapReader.ReadLine();
                }
                writer.Unindent();
                writer.Unindent();
            }

            writer.Unindent();
        }

        TryWriteConditions(writer, node);

        if (node.ForwardedVariables.Length > 0)
        {
            writer.WriteLine("forwardedVariables:");
            writer.Indent();

            foreach (var variableName in node.ForwardedVariables)
            {
                writer.WriteLine("- {0}", variableName);
            }

            writer.Unindent();
        }

        if (node.RequiresFileUpload)
        {
            writer.WriteLine("requiresFileUpload: true");
        }

        if (node.Dependencies.Length > 0)
        {
            writer.WriteLine("dependencies:");
            writer.Indent();
            foreach (var dependency in node.Dependencies)
            {
                writer.WriteLine("- id: {0}", dependency.Id);
            }

            writer.Unindent();
        }

        TryWriteNodeTrace(writer, trace);

        writer.Unindent();
    }

    private static void WriteBatchExecutionNode(OperationBatchExecutionNode batchNode, ExecutionNodeTrace? trace, CodeWriter writer)
    {
        foreach (var opDef in batchNode.Operations)
        {
            switch (opDef)
            {
                case SingleOperationDefinition single:
                    WriteOperationDefinitionAsNode(batchNode, single, trace, writer);
                    break;

                case BatchOperationDefinition batch:
                    WriteBatchOperationDefinitionAsNode(batchNode, batch, trace, writer);
                    break;
            }
        }
    }

    private static void WriteOperationDefinitionAsNode(
        OperationBatchExecutionNode batchNode,
        SingleOperationDefinition opDef,
        ExecutionNodeTrace? trace,
        CodeWriter writer)
    {
        writer.WriteLine("- id: {0}", opDef.Id);
        writer.Indent();

        writer.WriteLine("type: {0}", "Operation");

        if (opDef.SchemaName is not null)
        {
            writer.WriteLine("schema: {0}", opDef.SchemaName);
        }

        writer.WriteLine("operation: |");
        writer.Indent();
        var reader = new StringReader(opDef.Operation.SourceText);
        var line = reader.ReadLine();
        while (line != null)
        {
            writer.WriteLine(line);
            line = reader.ReadLine();
        }
        writer.Unindent();

        if (!opDef.Source.IsRoot)
        {
            writer.WriteLine("source: {0}", opDef.Source.ToString());
        }

        if (!opDef.Target.IsRoot)
        {
            writer.WriteLine("target: {0}", opDef.Target.ToString());
        }

        writer.WriteLine("batchingGroupId: {0}", batchNode.Id);

        WriteRequirements(opDef.Requirements, writer);
        WriteConditions(opDef.Conditions, writer);
        WriteForwardedVariables(opDef.ForwardedVariables, writer);

        if (opDef.RequiresFileUpload)
        {
            writer.WriteLine("requiresFileUpload: true");
        }

        WriteDependencies(opDef.Dependencies, writer);
        TryWriteNodeTrace(writer, trace);

        writer.Unindent();
    }

    private static void WriteBatchOperationDefinitionAsNode(
        OperationBatchExecutionNode batchNode,
        BatchOperationDefinition opDef,
        ExecutionNodeTrace? trace,
        CodeWriter writer)
    {
        writer.WriteLine("- id: {0}", opDef.Id);
        writer.Indent();

        writer.WriteLine("type: {0}", "OperationBatch");

        if (opDef.SchemaName is not null)
        {
            writer.WriteLine("schema: {0}", opDef.SchemaName);
        }

        writer.WriteLine("operation: |");
        writer.Indent();
        var reader = new StringReader(opDef.Operation.SourceText);
        var line = reader.ReadLine();
        while (line != null)
        {
            writer.WriteLine(line);
            line = reader.ReadLine();
        }
        writer.Unindent();

        if (!opDef.Source.IsRoot)
        {
            writer.WriteLine("source: {0}", opDef.Source.ToString());
        }

        if (opDef.Targets.Length > 0)
        {
            writer.WriteLine("targets:");
            writer.Indent();
            foreach (var target in opDef.Targets)
            {
                writer.WriteLine("- {0}", target.ToString());
            }
            writer.Unindent();
        }

        writer.WriteLine("batchingGroupId: {0}", batchNode.Id);

        WriteRequirements(opDef.Requirements, writer);
        WriteConditions(opDef.Conditions, writer);
        WriteForwardedVariables(opDef.ForwardedVariables, writer);

        if (opDef.RequiresFileUpload)
        {
            writer.WriteLine("requiresFileUpload: true");
        }

        WriteDependencies(opDef.Dependencies, writer);
        TryWriteNodeTrace(writer, trace);

        writer.Unindent();
    }

    private static void WriteRequirements(ReadOnlySpan<OperationRequirement> requirements, CodeWriter writer)
    {
        if (requirements.Length > 0)
        {
            writer.WriteLine("requirements:");
            writer.Indent();
            foreach (var requirement in requirements)
            {
                writer.WriteLine("- name: {0}", requirement.Key);
                writer.Indent();

                writer.WriteLine("selectionMap: >-");
                writer.Indent();
                var selectionMapReader = new StringReader(requirement.Map.ToString(indented: true));
                var selectionMapLine = selectionMapReader.ReadLine();
                while (selectionMapLine != null)
                {
                    writer.WriteLine(selectionMapLine);
                    selectionMapLine = selectionMapReader.ReadLine();
                }
                writer.Unindent();
                writer.Unindent();
            }

            writer.Unindent();
        }
    }

    private static void WriteConditions(ReadOnlySpan<ExecutionNodeCondition> conditions, CodeWriter writer)
    {
        if (conditions.Length > 0)
        {
            writer.WriteLine("conditions:");
            writer.Indent();
            foreach (var condition in conditions)
            {
                writer.WriteLine("- variable: {0}", "$" + condition.VariableName);
                writer.Indent();

                writer.WriteLine("passingValue: {0}", condition.PassingValue ? "true" : "false");
                writer.Unindent();
            }

            writer.Unindent();
        }
    }

    private static void WriteForwardedVariables(ReadOnlySpan<string> forwardedVariables, CodeWriter writer)
    {
        if (forwardedVariables.Length > 0)
        {
            writer.WriteLine("forwardedVariables:");
            writer.Indent();

            foreach (var variableName in forwardedVariables)
            {
                writer.WriteLine("- {0}", variableName);
            }

            writer.Unindent();
        }
    }

    private static void WriteDependencies(ReadOnlySpan<IOperationPlanNode> dependencies, CodeWriter writer)
    {
        if (dependencies.Length > 0)
        {
            writer.WriteLine("dependencies:");
            writer.Indent();

            foreach (var dependency in dependencies)
            {
                writer.WriteLine("- id: {0}", dependency.Id);
            }

            writer.Unindent();
        }
    }

    private static void WriteIntrospectionNode(IntrospectionExecutionNode node, ExecutionNodeTrace? trace, CodeWriter writer)
    {
        writer.WriteLine("- id: {0}", node.Id);
        writer.Indent();

        writer.WriteLine("type: {0}", "Introspection");

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

        TryWriteConditions(writer, node);

        TryWriteNodeTrace(writer, trace);

        writer.Unindent();
    }

    private static void WriteNodeFieldNode(NodeFieldExecutionNode node, ExecutionNodeTrace? trace, CodeWriter writer)
    {
        writer.WriteLine("- id: {0}", node.Id);
        writer.Indent();

        writer.WriteLine("type: {0}", node.Type.ToString());

        writer.WriteLine("idValue: {0}", node.IdValue.ToString());
        writer.WriteLine("responseName: {0}", node.ResponseName);

        if (node.Branches.Count > 0)
        {
            writer.WriteLine("branches:");
            writer.Indent();
            foreach (var branch in node.Branches.OrderBy(kvp => kvp.Key))
            {
                writer.WriteLine("- {0}: {1}", branch.Key, branch.Value.Id);
            }
            writer.Unindent();
        }

        writer.WriteLine("fallback: {0}", node.FallbackQuery.Id);

        TryWriteConditions(writer, node);

        TryWriteNodeTrace(writer, trace);

        writer.Unindent();
    }

    private static void TryWriteNodeTrace(CodeWriter writer, ExecutionNodeTrace? trace)
    {
        if (trace is not null)
        {
            if (trace.SpanId is not null)
            {
                writer.WriteLine("spanId: {0}", trace.SpanId);
            }

            writer.WriteLine("duration: {0}", trace.Duration.TotalMilliseconds);
            writer.WriteLine("status: {0}", trace.Status.ToString());
        }
    }

    private static void TryWriteConditions(CodeWriter writer, ExecutionNode node)
    {
        if (node.Conditions.Length > 0)
        {
            writer.WriteLine("conditions:");
            writer.Indent();
            foreach (var condition in node.Conditions)
            {
                writer.WriteLine("- variable: {0}", "$" + condition.VariableName);
                writer.Indent();

                writer.WriteLine("passingValue: {0}", condition.PassingValue ? "true" : "false");
                writer.Unindent();
            }

            writer.Unindent();
        }
    }
}
