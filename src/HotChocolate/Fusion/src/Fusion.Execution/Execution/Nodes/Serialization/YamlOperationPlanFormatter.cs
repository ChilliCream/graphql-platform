using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes.Serialization;

/// <summary>
/// Formats an <see cref="OperationPlan"/> as a YAML document.
/// This formatter is intended for testing purposes and is primarily used
/// to produce human-readable test snapshots.
/// </summary>
public sealed class YamlOperationPlanFormatter : OperationPlanFormatter
{
    private static readonly HashSet<string> s_yamlKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "null", "true", "false", "yes", "no", "on", "off", "y", "n"
    };

    /// <inheritdoc />
    public override string Format(OperationPlan plan, OperationPlanTrace? trace = null)
    {
        var sb = new StringBuilder();
        var writer = new CodeWriter(sb);

        writer.WriteLine("id: {0}", FormatScalar(plan.Id));
        WriteOperation(plan, trace, writer);

        writer.WriteLine(plan.AllNodes.IsEmpty ? "nodes: []" : "nodes:");
        writer.Indent();

        foreach (var node in plan.AllNodes)
        {
            ExecutionNodeTrace? nodeTrace = null;
            trace?.Nodes.TryGetValue(node.Id, out nodeTrace);

            WriteNode(node, plan.Operation, nodeTrace, writer);
        }

        writer.Unindent();

        if (!plan.DeliveryGroups.IsDefaultOrEmpty)
        {
            writer.WriteLine("deliveryGroups:");
            writer.Indent();

            foreach (var deliveryGroup in plan.DeliveryGroups)
            {
                WriteDeliveryGroup(deliveryGroup, writer);
            }

            writer.Unindent();
        }

        if (!plan.IncrementalPlans.IsDefaultOrEmpty)
        {
            writer.WriteLine("incrementalPlans:");
            writer.Indent();

            foreach (var incrementalPlan in plan.IncrementalPlans)
            {
                WriteIncrementalPlan(incrementalPlan, writer);
            }

            writer.Unindent();
        }

        WritePolicies(plan, writer);

        return sb.ToString();
    }

    private static void WriteNode(ExecutionNode node, Operation operation, ExecutionNodeTrace? nodeTrace, CodeWriter writer)
    {
        switch (node)
        {
            case OperationExecutionNode operationNode:
                WriteOperationNode(operationNode, operation, nodeTrace, writer);
                break;

            case EventStreamExecutionNode eventStreamNode:
                WriteEventStreamNode(eventStreamNode, operation, nodeTrace, writer);
                break;

            case OperationBatchExecutionNode batchNode:
                WriteBatchExecutionNode(batchNode, operation, nodeTrace, writer);
                break;

            case ApolloOperationExecutionNode apolloOperationNode:
                WriteApolloOperationNode(apolloOperationNode, operation, nodeTrace, writer);
                break;

            case ApolloOperationBatchExecutionNode apolloBatchNode:
                WriteApolloBatchExecutionNode(apolloBatchNode, operation, nodeTrace, writer);
                break;

            case IntrospectionExecutionNode introspectionNode:
                WriteIntrospectionNode(introspectionNode, operation, nodeTrace, writer);
                break;

            case NodeFieldExecutionNode nodeExecutionNode:
                WriteNodeFieldNode(nodeExecutionNode, operation, nodeTrace, writer);
                break;
        }
    }

    private static void WriteDeliveryGroup(DeliveryGroup deliveryGroup, CodeWriter writer)
    {
        writer.WriteLine("- id: {0}", deliveryGroup.Id);
        writer.Indent();

        writer.WriteLine("path: {0}", (deliveryGroup.Path ?? SelectionPath.Root).ToString());

        if (deliveryGroup.Label is not null)
        {
            writer.WriteLine("label: {0}", FormatScalar(deliveryGroup.Label));
        }

        if (deliveryGroup.IfVariable is not null)
        {
            writer.WriteLine("ifVariable: ${0}", deliveryGroup.IfVariable);
        }

        if (deliveryGroup.Parent is not null)
        {
            writer.WriteLine("parentId: {0}", deliveryGroup.Parent.Id);
        }

        writer.Unindent();
    }

    private static void WriteIncrementalPlan(IncrementalPlan incrementalPlan, CodeWriter writer)
    {
        writer.WriteLine("- deliveryGroupIds:");
        writer.Indent();
        writer.Indent();

        foreach (var deliveryGroup in incrementalPlan.DeliveryGroups)
        {
            writer.WriteLine("- {0}", deliveryGroup.Id);
        }

        writer.Unindent();

        writer.WriteLine("parentNodeId: {0}", incrementalPlan.ParentNodeId);

        if (!incrementalPlan.Requirements.IsDefaultOrEmpty)
        {
            WriteRequirements(incrementalPlan.Requirements.AsSpan(), writer);
        }

        WriteOperation(incrementalPlan.Operation, writer);

        if (!incrementalPlan.AllNodes.IsDefaultOrEmpty)
        {
            writer.WriteLine("nodes:");
            writer.Indent();

            foreach (var node in incrementalPlan.AllNodes)
            {
                WriteNode(node, incrementalPlan.Operation, nodeTrace: null, writer);
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
        WriteOperation(plan.Operation, writer);
        writer.WriteLine("searchSpace: {0}", plan.SearchSpace);
        writer.WriteLine("expandedNodes: {0}", plan.ExpandedNodes);

        if (trace is not null)
        {
            if (!string.IsNullOrEmpty(trace.AppId))
            {
                writer.WriteLine("appId: {0}", FormatScalar(trace.AppId));
            }

            if (!string.IsNullOrEmpty(trace.EnvironmentName))
            {
                writer.WriteLine("environment: {0}", FormatScalar(trace.EnvironmentName));
            }

            if (!string.IsNullOrEmpty(trace.TraceId))
            {
                writer.WriteLine("traceId: {0}", FormatScalar(trace.TraceId));
            }

            writer.WriteLine("duration: {0}", trace.Duration.TotalMilliseconds.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static void WriteOperation(Operation operation, CodeWriter writer)
    {
        writer.WriteLine("operation:");
        writer.Indent();

        writer.WriteLine("- document: |");
        writer.Indent();
        writer.Indent();
        var reader = new StringReader(operation.Definition.ToString(indented: true));
        var line = reader.ReadLine();
        while (line != null)
        {
            writer.WriteLine(line);
            line = reader.ReadLine();
        }
        writer.Unindent();

        if (!string.IsNullOrEmpty(operation.Name))
        {
            writer.WriteLine("name: {0}", FormatScalar(operation.Name));
        }

        writer.WriteLine("kind: {0}", operation.Definition.Operation);
        writer.WriteLine("id: {0}", FormatScalar(operation.Id));
        writer.WriteLine("hash: {0}", FormatScalar(operation.Hash));
        writer.WriteLine("shortHash: {0}", FormatScalar(operation.ShortHash));
        writer.Unindent();
        writer.Unindent();
    }

    private static string FormatSourceText(ReadOnlyMemory<byte> value)
        => Utf8GraphQLParser.Parse(value.Span).ToString(indented: true);

    private static void WriteOperationNode(OperationExecutionNode node, Operation operation, ExecutionNodeTrace? trace, CodeWriter writer)
    {
        writer.WriteLine("- id: {0}", node.Id);
        writer.Indent();

        writer.WriteLine("type: {0}", "Operation");

        if (node.SchemaName is not null)
        {
            writer.WriteLine("schema: {0}", FormatScalar(node.SchemaName));
        }

        WriteSourceOperation(node.Operation, writer);
        WriteLookupTypeName(node.LookupTypeName, writer);
        WriteResultSelectionSet(node.ResultSelectionSet, writer);

        if (!node.Source.IsRoot)
        {
            writer.WriteLine("source: {0}", node.Source.ToString());
        }

        if (!node.Target.IsRoot)
        {
            writer.WriteLine("target: {0}", node.Target.ToString());
        }

        WriteRequirements(node.Requirements, writer);

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

        WriteDependencies(node.Dependencies, node.OptionalDependencies, node.ParentDependencies, writer);

        TryWriteNodeTrace(writer, operation, trace);

        writer.Unindent();
    }

    private static void WriteEventStreamNode(
        EventStreamExecutionNode node,
        Operation operation,
        ExecutionNodeTrace? trace,
        CodeWriter writer)
    {
        writer.WriteLine("- id: {0}", node.Id);
        writer.Indent();

        writer.WriteLine("type: {0}", node.Type.ToString());
        writer.WriteLine("fieldName: {0}", FormatScalar(node.FieldName));
        WriteResultSelectionSet(node.ResultSelectionSet, writer);

        if (!node.Source.IsRoot)
        {
            writer.WriteLine("source: {0}", node.Source.ToString());
        }

        if (!node.Target.IsRoot)
        {
            writer.WriteLine("target: {0}", node.Target.ToString());
        }

        TryWriteConditions(writer, node);

        var eventStreamSource = node.EventStreamSource;

        writer.WriteLine("eventStream:");
        writer.Indent();

        writer.WriteLine("schema: {0}", FormatScalar(eventStreamSource.SchemaName));

        if (!eventStreamSource.Topics.IsDefaultOrEmpty)
        {
            writer.WriteLine(
                "topics: [{0}]",
                string.Join(", ", eventStreamSource.Topics.Select(FormatScalar)));
        }

        if (eventStreamSource.Broker is { } broker)
        {
            writer.WriteLine("broker: {0}", FormatScalar(broker));
        }

        writer.WriteLine("message: {0}", FormatScalar(node.Message));

        if (eventStreamSource.CursorField is { } cursorField)
        {
            writer.WriteLine("cursorField: {0}", FormatScalar(cursorField));
        }

        if (eventStreamSource.CursorArgument is { } cursorArgument)
        {
            writer.WriteLine("cursorArgument: {0}", FormatScalar(cursorArgument));
        }

        writer.Unindent();

        WriteDependencies(node.Dependencies, node.OptionalDependencies, node.ParentDependencies, writer);
        TryWriteNodeTrace(writer, operation, trace);

        writer.Unindent();
    }

    private static void WriteBatchExecutionNode(OperationBatchExecutionNode batchNode, Operation operation, ExecutionNodeTrace? trace, CodeWriter writer)
    {
        foreach (var opDef in batchNode.Operations)
        {
            switch (opDef)
            {
                case SingleOperationDefinition single:
                    WriteOperationDefinitionAsNode(batchNode, single, operation, trace, writer);
                    break;

                case BatchOperationDefinition batch:
                    WriteBatchOperationDefinitionAsNode(batchNode, batch, operation, trace, writer);
                    break;
            }
        }
    }

    private static void WriteOperationDefinitionAsNode(
        OperationBatchExecutionNode batchNode,
        SingleOperationDefinition opDef,
        Operation operation,
        ExecutionNodeTrace? trace,
        CodeWriter writer)
    {
        writer.WriteLine("- id: {0}", opDef.Id);
        writer.Indent();

        writer.WriteLine("type: {0}", "Operation");

        if (opDef.SchemaName is not null)
        {
            writer.WriteLine("schema: {0}", FormatScalar(opDef.SchemaName));
        }

        WriteSourceOperation(opDef.SourceText, writer);
        WriteLookupTypeName(opDef.LookupTypeName, writer);
        WriteResultSelectionSet(opDef.ResultSelectionSet, writer);

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
        WriteConditions(opDef.Conditions, opDef.ConditionAlternatives, writer);
        WriteForwardedVariables(opDef.ForwardedVariables, writer);

        if (opDef.RequiresFileUpload)
        {
            writer.WriteLine("requiresFileUpload: true");
        }

        WriteDependencies(opDef.Dependencies, opDef.OptionalDependencies, opDef.ParentDependencies, writer);
        TryWriteNodeTrace(writer, operation, trace);

        writer.Unindent();
    }

    private static void WriteBatchOperationDefinitionAsNode(
        OperationBatchExecutionNode batchNode,
        BatchOperationDefinition opDef,
        Operation operation,
        ExecutionNodeTrace? trace,
        CodeWriter writer)
    {
        writer.WriteLine("- id: {0}", opDef.Id);
        writer.Indent();

        writer.WriteLine("type: {0}", "OperationBatch");

        if (opDef.SchemaName is not null)
        {
            writer.WriteLine("schema: {0}", FormatScalar(opDef.SchemaName));
        }

        WriteSourceOperation(opDef.SourceText, writer);
        WriteLookupTypeName(opDef.LookupTypeName, writer);
        WriteResultSelectionSet(opDef.ResultSelectionSet, writer);

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
        WriteConditions(opDef.Conditions, opDef.ConditionAlternatives, writer);
        WriteForwardedVariables(opDef.ForwardedVariables, writer);

        if (opDef.RequiresFileUpload)
        {
            writer.WriteLine("requiresFileUpload: true");
        }

        WriteDependencies(opDef.Dependencies, opDef.OptionalDependencies, opDef.ParentDependencies, writer);
        TryWriteNodeTrace(writer, operation, trace);

        writer.Unindent();
    }

    private static void WriteApolloOperationNode(
        ApolloOperationExecutionNode node,
        Operation operation,
        ExecutionNodeTrace? trace,
        CodeWriter writer)
    {
        writer.WriteLine("- id: {0}", node.Id);
        writer.Indent();

        writer.WriteLine("type: {0}", "ApolloOperation");

        if (node.SchemaName is not null)
        {
            writer.WriteLine("schema: {0}", FormatScalar(node.SchemaName));
        }

        WriteSourceOperation(node.Lookup.Operation, writer);
        writer.WriteLine("entityType: {0}", FormatScalar(node.Lookup.EntityTypeName));
        WriteResultSelectionSet(node.ResultSelectionSet, writer);

        if (!node.Source.IsRoot)
        {
            writer.WriteLine("source: {0}", node.Source.ToString());
        }

        if (!node.Target.IsRoot)
        {
            writer.WriteLine("target: {0}", node.Target.ToString());
        }

        WriteRequirements(node.Requirements, writer);
        TryWriteConditions(writer, node);
        WriteForwardedVariables(node.ForwardedVariables, writer);

        if (node.RequiresFileUpload)
        {
            writer.WriteLine("requiresFileUpload: true");
        }

        WriteDependencies(node.Dependencies, node.OptionalDependencies, node.ParentDependencies, writer);
        TryWriteNodeTrace(writer, operation, trace);

        writer.Unindent();
    }

    private static void WriteApolloBatchExecutionNode(
        ApolloOperationBatchExecutionNode batchNode,
        Operation operation,
        ExecutionNodeTrace? trace,
        CodeWriter writer)
    {
        var operations = batchNode.Operations;
        var lookups = batchNode.Lookups;

        for (var i = 0; i < operations.Length; i++)
        {
            WriteApolloOperationDefinitionAsNode(
                batchNode,
                operations[i],
                lookups[i],
                operation,
                trace,
                writer);
        }
    }

    private static void WriteApolloOperationDefinitionAsNode(
        ApolloOperationBatchExecutionNode batchNode,
        OperationDefinition opDef,
        ApolloEntityLookup lookup,
        Operation operation,
        ExecutionNodeTrace? trace,
        CodeWriter writer)
    {
        writer.WriteLine("- id: {0}", opDef.Id);
        writer.Indent();

        writer.WriteLine("type: {0}", "ApolloOperationBatch");

        if (opDef.SchemaName is not null)
        {
            writer.WriteLine("schema: {0}", FormatScalar(opDef.SchemaName));
        }

        WriteSourceOperation(lookup.Operation, writer);
        writer.WriteLine("entityType: {0}", FormatScalar(lookup.EntityTypeName));
        WriteResultSelectionSet(opDef.ResultSelectionSet, writer);

        if (!opDef.Source.IsRoot)
        {
            writer.WriteLine("source: {0}", opDef.Source.ToString());
        }

        switch (opDef)
        {
            case SingleOperationDefinition { Target.IsRoot: false } single:
                writer.WriteLine("target: {0}", single.Target.ToString());
                break;

            case BatchOperationDefinition batch:
                writer.WriteLine("targets:");
                writer.Indent();
                foreach (var target in batch.Targets)
                {
                    writer.WriteLine("- {0}", target.ToString());
                }
                writer.Unindent();
                break;
        }

        writer.WriteLine("batchingGroupId: {0}", batchNode.Id);

        WriteRequirements(opDef.Requirements, writer);
        WriteConditions(opDef.Conditions, opDef.ConditionAlternatives, writer);
        WriteForwardedVariables(opDef.ForwardedVariables, writer);

        if (opDef.RequiresFileUpload)
        {
            writer.WriteLine("requiresFileUpload: true");
        }

        WriteDependencies(opDef.Dependencies, opDef.OptionalDependencies, opDef.ParentDependencies, writer);
        TryWriteNodeTrace(writer, operation, trace);

        writer.Unindent();
    }

    private static void WritePolicies(OperationPlan plan, CodeWriter writer)
    {
        var policies = plan.Policies;
        if (policies.IsEmpty)
        {
            return;
        }

        writer.WriteLine("policies:");
        writer.Indent();
        writer.WriteLine(policies.Descriptors.IsEmpty ? "descriptors: []" : "descriptors:");
        writer.Indent();
        foreach (var descriptor in policies.Descriptors)
        {
            writer.WriteLine("- directiveName: {0}", FormatScalar(descriptor.DirectiveName));
            writer.Indent();
            if (descriptor.PolicyName is not null)
            {
                writer.WriteLine("policyName: {0}", FormatScalar(descriptor.PolicyName));
            }

            if (!descriptor.Scopes.IsEmpty)
            {
                writer.WriteLine("scopes:");
                writer.Indent();
                foreach (var group in descriptor.Scopes)
                {
                    writer.WriteLine("- [{0}]", string.Join(", ", group.Select(FormatScalar)));
                }
                writer.Unindent();
            }

            if (descriptor.Selection is not Selection selection)
            {
                throw ThrowHelper.PolicyPlanSelectionNotInPlan();
            }

            writer.WriteLine("selection:");
            writer.Indent();
            WriteSelectionReference(plan, selection, writer);
            writer.Unindent();
            writer.Unindent();
        }
        writer.Unindent();

        writer.WriteLine(policies.Variables.IsEmpty ? "variables: []" : "variables:");
        writer.Indent();
        foreach (var variable in policies.Variables)
        {
            writer.WriteLine("- name: {0}", FormatScalar(variable.Name));
            writer.Indent();
            if (!variable.Selections.IsEmpty)
            {
                writer.WriteLine("selections:");
                writer.Indent();
                foreach (var selection in variable.Selections)
                {
                    writer.WriteLine("-");
                    writer.Indent();
                    WriteSelectionReference(plan, selection, writer);
                    writer.Unindent();
                }
                writer.Unindent();
            }

            if (!variable.Operands.IsEmpty)
            {
                writer.WriteLine("operands:");
                writer.Indent();
                foreach (var operand in variable.Operands)
                {
                    writer.WriteLine("- {0}", FormatScalar(operand.Name));
                }
                writer.Unindent();
            }
            writer.Unindent();
        }
        writer.Unindent();
        writer.Unindent();
    }

    private static void WriteSelectionReference(OperationPlan plan, Selection selection, CodeWriter writer)
    {
        var operation = selection.DeclaringSelectionSet.DeclaringOperation;
        if (!ReferenceEquals(operation, plan.Operation))
        {
            writer.WriteLine("incrementalPlan: {0}", GetIncrementalPlanIndex(plan, operation));
        }

        writer.WriteLine("path: {0}", GetSelectionPath(selection));
    }

    // Plain scalars must retain their string type when read by a YAML parser.
    private static string FormatScalar(string value)
    {
        if (value.Length > 0 && (char.IsAsciiLetter(value[0]) || value[0] is '_' or '$')
            && value[^1] != ':'
            && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-' or '.' or '!' or '$' or '/' or ':' or '+')
            && !s_yamlKeywords.Contains(value))
        {
            return value;
        }

        if (value.Length > 0 && char.IsAsciiDigit(value[0])
            && value.All(char.IsAsciiHexDigit)
            && !(value.StartsWith("0b", StringComparison.OrdinalIgnoreCase)
                && value.AsSpan(2).IndexOfAnyExcept('0', '1') < 0)
            && !double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
        {
            return value;
        }

        return $"\"{JsonEncodedText.Encode(value, JavaScriptEncoder.UnsafeRelaxedJsonEscaping)}\"";
    }

    private static void WriteSourceOperation(OperationSourceText source, CodeWriter writer)
    {
        writer.WriteLine("operation:");
        writer.Indent();
        writer.WriteLine("name: {0}", FormatScalar(source.Name));
        writer.WriteLine("kind: {0}", source.Type);
        writer.WriteLine("document: |");
        writer.Indent();
        var reader = new StringReader(FormatSourceText(source.Value));
        while (reader.ReadLine() is { } line)
        {
            writer.WriteLine(line);
        }
        writer.Unindent();
        writer.WriteLine("hash: {0}", FormatScalar(source.Hash.Sha256));
        writer.WriteLine("shortHash: {0}", FormatScalar(source.Hash.Sha256Short));
        writer.WriteLine("xxHash: {0}", source.Hash.Xxx);
        writer.Unindent();
    }

    private static void WriteLookupTypeName(string? typeName, CodeWriter writer)
    {
        if (typeName is not null)
        {
            writer.WriteLine("lookupTypeName: {0}", FormatScalar(typeName));
        }
    }

    private static void WriteResultSelectionSet(ResultSelectionSet resultSelectionSet, CodeWriter writer)
    {
        writer.WriteLine("resultSelectionSet: >-");
        writer.Indent();
        writer.WriteLine(resultSelectionSet.ToString(indented: false));
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

                writer.WriteLine("type: {0}", FormatScalar(requirement.Type.ToString()));
                writer.WriteLine("path: {0}", requirement.Path);

                if (requirement.InternalAlias is not null)
                {
                    writer.WriteLine("internalAlias: {0}", requirement.InternalAlias);
                }

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

    private static void WriteConditions(
        ReadOnlySpan<ExecutionNodeCondition> conditions,
        ReadOnlySpan<ExecutionNodeCondition[]> conditionAlternatives,
        CodeWriter writer)
    {
        if (conditions.Length > 0)
        {
            writer.WriteLine("conditions:");
            writer.Indent();
            WriteConditionItems(conditions, writer);
            writer.Unindent();
        }

        if (conditionAlternatives.Length > 0)
        {
            writer.WriteLine("conditionAlternatives:");
            writer.Indent();

            foreach (var alternative in conditionAlternatives)
            {
                writer.WriteLine("-");
                writer.Indent();
                WriteConditionItems(alternative, writer);
                writer.Unindent();
            }

            writer.Unindent();
        }
    }

    private static void WriteConditionItems(
        ReadOnlySpan<ExecutionNodeCondition> conditions,
        CodeWriter writer)
    {
        foreach (var condition in conditions)
        {
            writer.WriteLine("- variable: {0}", "$" + condition.VariableName);
            writer.Indent();

            writer.WriteLine("passingValue: {0}", condition.PassingValue ? "true" : "false");
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

    private static void WriteDependencies(
        ReadOnlySpan<IOperationPlanNode> dependencies,
        ReadOnlySpan<IOperationPlanNode> optionalDependencies,
        ReadOnlySpan<int> parentDependencies,
        CodeWriter writer)
    {
        if (dependencies.Length == 0 && optionalDependencies.Length == 0 && parentDependencies.Length == 0)
        {
            return;
        }

        writer.WriteLine("dependencies:");
        writer.Indent();

        var i = 0;
        var j = 0;

        while (i < dependencies.Length || j < optionalDependencies.Length)
        {
            if (j == optionalDependencies.Length
                || (i < dependencies.Length && dependencies[i].Id < optionalDependencies[j].Id))
            {
                writer.WriteLine("- id: {0}", dependencies[i++].Id);
            }
            else
            {
                writer.WriteLine("- id: {0}", optionalDependencies[j++].Id);
            }
        }

        foreach (var parentStepId in parentDependencies)
        {
            writer.WriteLine("- parentNodeId: {0}", parentStepId);
        }

        writer.Unindent();
    }

    private static void WriteIntrospectionNode(IntrospectionExecutionNode node, Operation operation, ExecutionNodeTrace? trace, CodeWriter writer)
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
            writer.WriteLine("responseName: {0}", FormatScalar(selection.ResponseName));
            writer.WriteLine("fieldName: {0}", FormatScalar(selection.Field.Name));
            writer.Unindent();
        }
        writer.Unindent();

        TryWriteConditions(writer, node);

        TryWriteNodeTrace(writer, operation, trace);

        writer.Unindent();
    }

    private static void WriteNodeFieldNode(NodeFieldExecutionNode node, Operation operation, ExecutionNodeTrace? trace, CodeWriter writer)
    {
        writer.WriteLine("- id: {0}", node.Id);
        writer.Indent();

        writer.WriteLine("type: {0}", node.Type.ToString());

        writer.WriteLine("idValue: {0}", FormatScalar(node.IdValue.ToString()));
        writer.WriteLine("responseName: {0}", FormatScalar(node.ResponseName));

        if (node.Branches.Count > 0)
        {
            writer.WriteLine("branches:");
            writer.Indent();
            foreach (var branch in node.Branches.OrderBy(kvp => kvp.Key))
            {
                writer.WriteLine("{0}: {1}", FormatScalar(branch.Key), branch.Value.Id);
            }
            writer.Unindent();
        }
        else
        {
            writer.WriteLine("branches: {}");
        }

        writer.WriteLine("fallback: {0}", node.FallbackQuery.Id);

        TryWriteConditions(writer, node);

        TryWriteNodeTrace(writer, operation, trace);

        writer.Unindent();
    }

    private static void TryWriteNodeTrace(CodeWriter writer, Operation operation, ExecutionNodeTrace? trace)
    {
        if (trace is not null)
        {
            if (trace.SpanId is not null)
            {
                writer.WriteLine("spanId: {0}", FormatScalar(trace.SpanId));
            }

            writer.WriteLine("duration: {0}", trace.Duration.TotalMilliseconds.ToString(CultureInfo.InvariantCulture));
            writer.WriteLine("status: {0}", trace.Status.ToString());

            if (!trace.VariableSets.IsDefaultOrEmpty)
            {
                writer.WriteLine("variableSets:");
                writer.Indent();
                foreach (var variableSet in trace.VariableSets)
                {
                    writer.WriteLine("{0}:", FormatScalar(variableSet.Path.ToPath(operation).Print()));
                    writer.Indent();
                    var reader = new StringReader(Encoding.UTF8.GetString(variableSet.Values.AsSequence()));
                    while (reader.ReadLine() is { } line)
                    {
                        writer.WriteLine(line);
                    }
                    writer.Unindent();
                }
                writer.Unindent();
            }

            if (trace.Transport is { } transport)
            {
                writer.WriteLine("transport:");
                writer.Indent();
                writer.WriteLine("uri: {0}", FormatScalar(transport.Uri.ToString()));
                writer.WriteLine("contentType: {0}", FormatScalar(transport.ContentType));
                writer.Unindent();
            }
        }
    }

    private static void TryWriteConditions(CodeWriter writer, ExecutionNode node)
        => WriteConditions(node.Conditions, node.ConditionAlternatives, writer);
}
