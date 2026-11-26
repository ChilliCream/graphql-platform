using System.Buffers;
using System.Text;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Text.Yaml;

namespace HotChocolate.Fusion.Execution.Nodes.Serialization;

/// <summary>
/// Formats an <see cref="OperationPlan"/> as a YAML document for test snapshots.
/// </summary>
public sealed class YamlOperationPlanFormatter : OperationPlanFormatter
{
    /// <inheritdoc />
    public override string Format(OperationPlan plan, OperationPlanTrace? trace = null)
    {
        using var buffer = new PooledArrayWriter();
        Format(buffer, plan, trace);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <summary>
    /// Formats the specified <paramref name="plan"/> as YAML and writes the
    /// UTF-8 encoded output to <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">The buffer writer to receive the YAML output.</param>
    /// <param name="plan">The operation plan to format.</param>
    /// <param name="trace">Optional trace information to include in the output.</param>
    public void Format(IBufferWriter<byte> writer, OperationPlan plan, OperationPlanTrace? trace = null)
        => Format(new YamlWriter(writer), plan, trace);

    /// <summary>
    /// Writes the specified <paramref name="plan"/> as a YAML mapping at the current
    /// position of <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">The YAML writer to write the plan to.</param>
    /// <param name="plan">The operation plan to format.</param>
    /// <param name="trace">The trace information to include, or <c>null</c>.</param>
    public static void Format(YamlWriter writer, OperationPlan plan, OperationPlanTrace? trace)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("id");
        writer.WriteStringValue(plan.Id);

        WriteOperation(plan, trace, writer);

        writer.WritePropertyName("nodes");
        writer.WriteStartArray();

        foreach (var node in plan.AllNodes)
        {
            ExecutionNodeTrace? nodeTrace = null;
            trace?.Nodes.TryGetValue(node.Id, out nodeTrace);

            WriteNode(node, plan.Operation, nodeTrace, writer);
        }

        writer.WriteEndArray();

        if (!plan.DeliveryGroups.IsDefaultOrEmpty)
        {
            writer.WritePropertyName("deliveryGroups");
            writer.WriteStartArray();

            foreach (var deliveryGroup in plan.DeliveryGroups)
            {
                WriteDeliveryGroup(deliveryGroup, writer);
            }

            writer.WriteEndArray();
        }

        if (!plan.IncrementalPlans.IsDefaultOrEmpty)
        {
            writer.WritePropertyName("incrementalPlans");
            writer.WriteStartArray();

            foreach (var incrementalPlan in plan.IncrementalPlans)
            {
                WriteIncrementalPlan(incrementalPlan, writer);
            }

            writer.WriteEndArray();
        }

        WritePolicies(plan, writer);

        writer.WriteEndObject();
    }

    private static void WriteNode(ExecutionNode node, Operation operation, ExecutionNodeTrace? nodeTrace, YamlWriter writer)
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

    private static void WriteDeliveryGroup(DeliveryGroup deliveryGroup, YamlWriter writer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("id");
        writer.WriteNumberValue(deliveryGroup.Id);

        writer.WritePropertyName("path");
        writer.WriteStringValue((deliveryGroup.Path ?? SelectionPath.Root).ToString());

        if (deliveryGroup.Label is not null)
        {
            writer.WritePropertyName("label");
            writer.WriteStringValue(deliveryGroup.Label);
        }

        if (deliveryGroup.IfVariable is not null)
        {
            writer.WritePropertyName("ifVariable");
            writer.WriteStringValue("$" + deliveryGroup.IfVariable);
        }

        if (deliveryGroup.Parent is not null)
        {
            writer.WritePropertyName("parentId");
            writer.WriteNumberValue(deliveryGroup.Parent.Id);
        }

        writer.WriteEndObject();
    }

    private static void WriteIncrementalPlan(IncrementalPlan incrementalPlan, YamlWriter writer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("deliveryGroupIds");
        writer.WriteStartArray();

        foreach (var deliveryGroup in incrementalPlan.DeliveryGroups)
        {
            writer.WriteNumberValue(deliveryGroup.Id);
        }

        writer.WriteEndArray();

        writer.WritePropertyName("parentNodeId");
        writer.WriteNumberValue(incrementalPlan.ParentNodeId);

        if (!incrementalPlan.Requirements.IsDefaultOrEmpty)
        {
            WriteRequirements(incrementalPlan.Requirements.AsSpan(), writer);
        }

        WriteOperation(incrementalPlan.Operation, writer);

        if (!incrementalPlan.AllNodes.IsDefaultOrEmpty)
        {
            writer.WritePropertyName("nodes");
            writer.WriteStartArray();

            foreach (var node in incrementalPlan.AllNodes)
            {
                WriteNode(node, incrementalPlan.Operation, nodeTrace: null, writer);
            }

            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    private static void WriteOperation(
        OperationPlan plan,
        OperationPlanTrace? trace,
        YamlWriter writer)
    {
        WriteOperation(plan.Operation, writer);

        writer.WritePropertyName("searchSpace");
        writer.WriteNumberValue(plan.SearchSpace);

        writer.WritePropertyName("expandedNodes");
        writer.WriteNumberValue(plan.ExpandedNodes);

        if (trace is not null)
        {
            if (!string.IsNullOrEmpty(trace.AppId))
            {
                writer.WritePropertyName("appId");
                writer.WriteStringValue(trace.AppId);
            }

            if (!string.IsNullOrEmpty(trace.EnvironmentName))
            {
                writer.WritePropertyName("environment");
                writer.WriteStringValue(trace.EnvironmentName);
            }

            if (!string.IsNullOrEmpty(trace.TraceId))
            {
                writer.WritePropertyName("traceId");
                writer.WriteStringValue(trace.TraceId);
            }

            writer.WritePropertyName("duration");
            writer.WriteNumberValue(trace.Duration.TotalMilliseconds);
        }
    }

    private static void WriteOperation(Operation operation, YamlWriter writer)
    {
        writer.WritePropertyName("operation");
        writer.WriteStartArray();
        writer.WriteStartObject();

        writer.WritePropertyName("document");
        writer.WriteLiteralStringValue(ToLiteralText(operation.Definition.ToString(indented: true)));

        if (!string.IsNullOrEmpty(operation.Name))
        {
            writer.WritePropertyName("name");
            writer.WriteStringValue(operation.Name);
        }

        writer.WritePropertyName("kind");
        writer.WriteStringValue(operation.Definition.Operation.ToString());

        writer.WritePropertyName("id");
        writer.WriteStringValue(operation.Id);

        writer.WritePropertyName("hash");
        writer.WriteStringValue(operation.Hash);

        writer.WritePropertyName("shortHash");
        writer.WriteStringValue(operation.ShortHash);

        writer.WriteEndObject();
        writer.WriteEndArray();
    }

    private static void WriteOperationNode(OperationExecutionNode node, Operation operation, ExecutionNodeTrace? trace, YamlWriter writer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("id");
        writer.WriteNumberValue(node.Id);

        writer.WritePropertyName("type");
        writer.WriteStringValue("Operation");

        if (node.SchemaName is not null)
        {
            writer.WritePropertyName("schema");
            writer.WriteStringValue(node.SchemaName);
        }

        WriteSourceOperation(node.Operation, writer);
        WriteLookupTypeName(node.LookupTypeName, writer);
        WriteResultSelectionSet(node.ResultSelectionSet, writer);

        if (!node.Source.IsRoot)
        {
            writer.WritePropertyName("source");
            writer.WriteStringValue(node.Source.ToString());
        }

        if (!node.Target.IsRoot)
        {
            writer.WritePropertyName("target");
            writer.WriteStringValue(node.Target.ToString());
        }

        WriteRequirements(node.Requirements, writer);
        TryWriteConditions(writer, node);
        WriteForwardedVariables(node.ForwardedVariables, writer);

        if (node.RequiresFileUpload)
        {
            writer.WritePropertyName("requiresFileUpload");
            writer.WriteBooleanValue(true);
        }

        WriteDependencies(node.Dependencies, node.OptionalDependencies, node.ParentDependencies, writer);
        TryWriteNodeTrace(writer, operation, trace);

        writer.WriteEndObject();
    }

    private static void WriteEventStreamNode(
        EventStreamExecutionNode node,
        Operation operation,
        ExecutionNodeTrace? trace,
        YamlWriter writer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("id");
        writer.WriteNumberValue(node.Id);

        writer.WritePropertyName("type");
        writer.WriteStringValue(node.Type.ToString());

        writer.WritePropertyName("fieldName");
        writer.WriteStringValue(node.FieldName);

        WriteResultSelectionSet(node.ResultSelectionSet, writer);

        if (!node.Source.IsRoot)
        {
            writer.WritePropertyName("source");
            writer.WriteStringValue(node.Source.ToString());
        }

        if (!node.Target.IsRoot)
        {
            writer.WritePropertyName("target");
            writer.WriteStringValue(node.Target.ToString());
        }

        TryWriteConditions(writer, node);

        var eventStreamSource = node.EventStreamSource;

        writer.WritePropertyName("eventStream");
        writer.WriteStartObject();

        writer.WritePropertyName("schema");
        writer.WriteStringValue(eventStreamSource.SchemaName);

        if (!eventStreamSource.Topics.IsDefaultOrEmpty)
        {
            writer.WritePropertyName("topics");
            writer.WriteStartFlowArray();

            foreach (var topic in eventStreamSource.Topics)
            {
                writer.WriteStringValue(topic);
            }

            writer.WriteEndArray();
        }

        if (eventStreamSource.Broker is { } broker)
        {
            writer.WritePropertyName("broker");
            writer.WriteStringValue(broker);
        }

        writer.WritePropertyName("message");
        writer.WriteStringValue(node.Message);

        if (eventStreamSource.CursorField is { } cursorField)
        {
            writer.WritePropertyName("cursorField");
            writer.WriteStringValue(cursorField);
        }

        if (eventStreamSource.CursorArgument is { } cursorArgument)
        {
            writer.WritePropertyName("cursorArgument");
            writer.WriteStringValue(cursorArgument);
        }

        writer.WriteEndObject();

        WriteDependencies(node.Dependencies, node.OptionalDependencies, node.ParentDependencies, writer);
        TryWriteNodeTrace(writer, operation, trace);

        writer.WriteEndObject();
    }

    private static void WriteBatchExecutionNode(OperationBatchExecutionNode batchNode, Operation operation, ExecutionNodeTrace? trace, YamlWriter writer)
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
        YamlWriter writer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("id");
        writer.WriteNumberValue(opDef.Id);

        writer.WritePropertyName("type");
        writer.WriteStringValue("Operation");

        if (opDef.SchemaName is not null)
        {
            writer.WritePropertyName("schema");
            writer.WriteStringValue(opDef.SchemaName);
        }

        WriteSourceOperation(opDef.SourceText, writer);
        WriteLookupTypeName(opDef.LookupTypeName, writer);
        WriteResultSelectionSet(opDef.ResultSelectionSet, writer);

        if (!opDef.Source.IsRoot)
        {
            writer.WritePropertyName("source");
            writer.WriteStringValue(opDef.Source.ToString());
        }

        if (!opDef.Target.IsRoot)
        {
            writer.WritePropertyName("target");
            writer.WriteStringValue(opDef.Target.ToString());
        }

        writer.WritePropertyName("batchingGroupId");
        writer.WriteNumberValue(batchNode.Id);

        WriteRequirements(opDef.Requirements, writer);
        WriteConditions(opDef.Conditions, opDef.ConditionAlternatives, writer);
        WriteForwardedVariables(opDef.ForwardedVariables, writer);

        if (opDef.RequiresFileUpload)
        {
            writer.WritePropertyName("requiresFileUpload");
            writer.WriteBooleanValue(true);
        }

        WriteDependencies(opDef.Dependencies, opDef.OptionalDependencies, opDef.ParentDependencies, writer);
        TryWriteNodeTrace(writer, operation, trace);

        writer.WriteEndObject();
    }

    private static void WriteBatchOperationDefinitionAsNode(
        OperationBatchExecutionNode batchNode,
        BatchOperationDefinition opDef,
        Operation operation,
        ExecutionNodeTrace? trace,
        YamlWriter writer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("id");
        writer.WriteNumberValue(opDef.Id);

        writer.WritePropertyName("type");
        writer.WriteStringValue("OperationBatch");

        if (opDef.SchemaName is not null)
        {
            writer.WritePropertyName("schema");
            writer.WriteStringValue(opDef.SchemaName);
        }

        WriteSourceOperation(opDef.SourceText, writer);
        WriteLookupTypeName(opDef.LookupTypeName, writer);
        WriteResultSelectionSet(opDef.ResultSelectionSet, writer);

        if (!opDef.Source.IsRoot)
        {
            writer.WritePropertyName("source");
            writer.WriteStringValue(opDef.Source.ToString());
        }

        if (opDef.Targets.Length > 0)
        {
            WriteTargets(opDef.Targets, writer);
        }

        writer.WritePropertyName("batchingGroupId");
        writer.WriteNumberValue(batchNode.Id);

        WriteRequirements(opDef.Requirements, writer);
        WriteConditions(opDef.Conditions, opDef.ConditionAlternatives, writer);
        WriteForwardedVariables(opDef.ForwardedVariables, writer);

        if (opDef.RequiresFileUpload)
        {
            writer.WritePropertyName("requiresFileUpload");
            writer.WriteBooleanValue(true);
        }

        WriteDependencies(opDef.Dependencies, opDef.OptionalDependencies, opDef.ParentDependencies, writer);
        TryWriteNodeTrace(writer, operation, trace);

        writer.WriteEndObject();
    }

    private static void WriteApolloOperationNode(
        ApolloOperationExecutionNode node,
        Operation operation,
        ExecutionNodeTrace? trace,
        YamlWriter writer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("id");
        writer.WriteNumberValue(node.Id);

        writer.WritePropertyName("type");
        writer.WriteStringValue("ApolloOperation");

        if (node.SchemaName is not null)
        {
            writer.WritePropertyName("schema");
            writer.WriteStringValue(node.SchemaName);
        }

        WriteSourceOperation(node.Lookup.Operation, writer);

        writer.WritePropertyName("entityType");
        writer.WriteStringValue(node.Lookup.EntityTypeName);

        WriteResultSelectionSet(node.ResultSelectionSet, writer);

        if (!node.Source.IsRoot)
        {
            writer.WritePropertyName("source");
            writer.WriteStringValue(node.Source.ToString());
        }

        if (!node.Target.IsRoot)
        {
            writer.WritePropertyName("target");
            writer.WriteStringValue(node.Target.ToString());
        }

        WriteRequirements(node.Requirements, writer);
        TryWriteConditions(writer, node);
        WriteForwardedVariables(node.ForwardedVariables, writer);

        if (node.RequiresFileUpload)
        {
            writer.WritePropertyName("requiresFileUpload");
            writer.WriteBooleanValue(true);
        }

        WriteDependencies(node.Dependencies, node.OptionalDependencies, node.ParentDependencies, writer);
        TryWriteNodeTrace(writer, operation, trace);

        writer.WriteEndObject();
    }

    private static void WriteApolloBatchExecutionNode(
        ApolloOperationBatchExecutionNode batchNode,
        Operation operation,
        ExecutionNodeTrace? trace,
        YamlWriter writer)
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
        YamlWriter writer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("id");
        writer.WriteNumberValue(opDef.Id);

        writer.WritePropertyName("type");
        writer.WriteStringValue("ApolloOperationBatch");

        if (opDef.SchemaName is not null)
        {
            writer.WritePropertyName("schema");
            writer.WriteStringValue(opDef.SchemaName);
        }

        WriteSourceOperation(lookup.Operation, writer);

        writer.WritePropertyName("entityType");
        writer.WriteStringValue(lookup.EntityTypeName);

        WriteResultSelectionSet(opDef.ResultSelectionSet, writer);

        if (!opDef.Source.IsRoot)
        {
            writer.WritePropertyName("source");
            writer.WriteStringValue(opDef.Source.ToString());
        }

        switch (opDef)
        {
            case SingleOperationDefinition { Target.IsRoot: false } single:
                writer.WritePropertyName("target");
                writer.WriteStringValue(single.Target.ToString());
                break;

            case BatchOperationDefinition batch:
                WriteTargets(batch.Targets, writer);
                break;
        }

        writer.WritePropertyName("batchingGroupId");
        writer.WriteNumberValue(batchNode.Id);

        WriteRequirements(opDef.Requirements, writer);
        WriteConditions(opDef.Conditions, opDef.ConditionAlternatives, writer);
        WriteForwardedVariables(opDef.ForwardedVariables, writer);

        if (opDef.RequiresFileUpload)
        {
            writer.WritePropertyName("requiresFileUpload");
            writer.WriteBooleanValue(true);
        }

        WriteDependencies(opDef.Dependencies, opDef.OptionalDependencies, opDef.ParentDependencies, writer);
        TryWriteNodeTrace(writer, operation, trace);

        writer.WriteEndObject();
    }

    private static void WriteTargets(ReadOnlySpan<SelectionPath> targets, YamlWriter writer)
    {
        writer.WritePropertyName("targets");
        writer.WriteStartArray();

        foreach (var target in targets)
        {
            writer.WriteStringValue(target.ToString());
        }

        writer.WriteEndArray();
    }

    private static void WritePolicies(OperationPlan plan, YamlWriter writer)
    {
        var policies = plan.Policies;

        if (policies.IsEmpty)
        {
            return;
        }

        writer.WritePropertyName("policies");
        writer.WriteStartObject();

        writer.WritePropertyName("descriptors");
        writer.WriteStartArray();

        foreach (var descriptor in policies.Descriptors)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("directiveName");
            writer.WriteStringValue(descriptor.DirectiveName);

            if (descriptor.PolicyName is not null)
            {
                writer.WritePropertyName("policyName");
                writer.WriteStringValue(descriptor.PolicyName);
            }

            if (!descriptor.Scopes.IsEmpty)
            {
                writer.WritePropertyName("scopes");
                writer.WriteStartArray();

                foreach (var group in descriptor.Scopes)
                {
                    writer.WriteStartFlowArray();

                    foreach (var scope in group)
                    {
                        writer.WriteStringValue(scope);
                    }

                    writer.WriteEndArray();
                }

                writer.WriteEndArray();
            }

            if (descriptor.Selection is not Selection selection)
            {
                throw ThrowHelper.PolicyPlanSelectionNotInPlan();
            }

            writer.WritePropertyName("selection");
            WriteSelectionReference(plan, selection, writer);

            writer.WriteEndObject();
        }

        writer.WriteEndArray();

        writer.WritePropertyName("variables");
        writer.WriteStartArray();

        foreach (var variable in policies.Variables)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("name");
            writer.WriteStringValue(variable.Name);

            if (!variable.Selections.IsEmpty)
            {
                writer.WritePropertyName("selections");
                writer.WriteStartArray();

                foreach (var selection in variable.Selections)
                {
                    WriteSelectionReference(plan, selection, writer);
                }

                writer.WriteEndArray();
            }

            if (!variable.Operands.IsEmpty)
            {
                writer.WritePropertyName("operands");
                writer.WriteStartArray();

                foreach (var operand in variable.Operands)
                {
                    writer.WriteStringValue(operand.Name);
                }

                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();

        writer.WriteEndObject();
    }

    private static void WriteSelectionReference(OperationPlan plan, Selection selection, YamlWriter writer)
    {
        writer.WriteStartObject();

        var operation = selection.DeclaringSelectionSet.DeclaringOperation;

        if (!ReferenceEquals(operation, plan.Operation))
        {
            writer.WritePropertyName("incrementalPlan");
            writer.WriteNumberValue(GetIncrementalPlanIndex(plan, operation));
        }

        writer.WritePropertyName("path");
        writer.WriteStringValue(GetSelectionPath(selection).ToString());

        writer.WriteEndObject();
    }

    private static void WriteSourceOperation(OperationSourceText source, YamlWriter writer)
    {
        writer.WritePropertyName("operation");
        writer.WriteStartObject();

        writer.WritePropertyName("name");
        writer.WriteStringValue(source.Name);

        writer.WritePropertyName("kind");
        writer.WriteStringValue(source.Type.ToString());

        writer.WritePropertyName("document");
        writer.WriteLiteralStringValue(
            ToLiteralText(Utf8GraphQLParser.Parse(source.Value.Span).ToString(indented: true)));

        writer.WritePropertyName("hash");
        writer.WriteStringValue(source.Hash.Sha256);

        writer.WritePropertyName("shortHash");
        writer.WriteStringValue(source.Hash.Sha256Short);

        writer.WritePropertyName("xxHash");
        writer.WriteNumberValue(source.Hash.Xxx);

        writer.WriteEndObject();
    }

    private static void WriteLookupTypeName(string? typeName, YamlWriter writer)
    {
        if (typeName is not null)
        {
            writer.WritePropertyName("lookupTypeName");
            writer.WriteStringValue(typeName);
        }
    }

    private static void WriteResultSelectionSet(ResultSelectionSet resultSelectionSet, YamlWriter writer)
    {
        writer.WritePropertyName("resultSelectionSet");
        writer.WriteFoldedStringValue(resultSelectionSet.ToString(indented: false));
    }

    private static void WriteRequirements(ReadOnlySpan<OperationRequirement> requirements, YamlWriter writer)
    {
        if (requirements.Length == 0)
        {
            return;
        }

        writer.WritePropertyName("requirements");
        writer.WriteStartArray();

        foreach (var requirement in requirements)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("name");
            writer.WriteStringValue(requirement.Key);

            writer.WritePropertyName("type");
            writer.WriteStringValue(requirement.Type.ToString());

            writer.WritePropertyName("path");
            writer.WriteStringValue(requirement.Path.ToString());

            if (requirement.InternalAlias is not null)
            {
                writer.WritePropertyName("internalAlias");
                writer.WriteStringValue(requirement.InternalAlias);
            }

            writer.WritePropertyName("selectionMap");
            writer.WriteFoldedStringValue(requirement.Map.ToString(indented: true));

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteConditions(
        ReadOnlySpan<ExecutionNodeCondition> conditions,
        ReadOnlySpan<ExecutionNodeCondition[]> conditionAlternatives,
        YamlWriter writer)
    {
        if (conditions.Length > 0)
        {
            writer.WritePropertyName("conditions");
            WriteConditionItems(conditions, writer);
        }

        if (conditionAlternatives.Length > 0)
        {
            writer.WritePropertyName("conditionAlternatives");
            writer.WriteStartArray();

            foreach (var alternative in conditionAlternatives)
            {
                WriteConditionItems(alternative, writer);
            }

            writer.WriteEndArray();
        }
    }

    private static void WriteConditionItems(
        ReadOnlySpan<ExecutionNodeCondition> conditions,
        YamlWriter writer)
    {
        writer.WriteStartArray();

        foreach (var condition in conditions)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("variable");
            writer.WriteStringValue("$" + condition.VariableName);

            writer.WritePropertyName("passingValue");
            writer.WriteBooleanValue(condition.PassingValue);

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteForwardedVariables(ReadOnlySpan<string> forwardedVariables, YamlWriter writer)
    {
        if (forwardedVariables.Length == 0)
        {
            return;
        }

        writer.WritePropertyName("forwardedVariables");
        writer.WriteStartArray();

        foreach (var variableName in forwardedVariables)
        {
            writer.WriteStringValue(variableName);
        }

        writer.WriteEndArray();
    }

    private static void WriteDependencies(
        ReadOnlySpan<IOperationPlanNode> dependencies,
        ReadOnlySpan<IOperationPlanNode> optionalDependencies,
        ReadOnlySpan<int> parentDependencies,
        YamlWriter writer)
    {
        if (dependencies.Length == 0 && optionalDependencies.Length == 0 && parentDependencies.Length == 0)
        {
            return;
        }

        writer.WritePropertyName("dependencies");
        writer.WriteStartArray();

        var i = 0;
        var j = 0;

        while (i < dependencies.Length || j < optionalDependencies.Length)
        {
            var dependency =
                j == optionalDependencies.Length
                || (i < dependencies.Length && dependencies[i].Id < optionalDependencies[j].Id)
                    ? dependencies[i++]
                    : optionalDependencies[j++];

            writer.WriteStartObject();
            writer.WritePropertyName("id");
            writer.WriteNumberValue(dependency.Id);
            writer.WriteEndObject();
        }

        foreach (var parentStepId in parentDependencies)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("parentNodeId");
            writer.WriteNumberValue(parentStepId);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteIntrospectionNode(IntrospectionExecutionNode node, Operation operation, ExecutionNodeTrace? trace, YamlWriter writer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("id");
        writer.WriteNumberValue(node.Id);

        writer.WritePropertyName("type");
        writer.WriteStringValue("Introspection");

        writer.WritePropertyName("selections");
        writer.WriteStartArray();

        foreach (var selection in node.Selections)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("id");
            writer.WriteNumberValue(selection.Id);

            writer.WritePropertyName("responseName");
            writer.WriteStringValue(selection.ResponseName);

            writer.WritePropertyName("fieldName");
            writer.WriteStringValue(selection.Field.Name);

            writer.WriteEndObject();
        }

        writer.WriteEndArray();

        TryWriteConditions(writer, node);
        TryWriteNodeTrace(writer, operation, trace);

        writer.WriteEndObject();
    }

    private static void WriteNodeFieldNode(NodeFieldExecutionNode node, Operation operation, ExecutionNodeTrace? trace, YamlWriter writer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("id");
        writer.WriteNumberValue(node.Id);

        writer.WritePropertyName("type");
        writer.WriteStringValue(node.Type.ToString());

        writer.WritePropertyName("idValue");
        writer.WriteStringValue(node.IdValue.ToString());

        writer.WritePropertyName("responseName");
        writer.WriteStringValue(node.ResponseName);

        writer.WritePropertyName("branches");
        writer.WriteStartObject();

        foreach (var branch in node.Branches.OrderBy(kvp => kvp.Key))
        {
            writer.WritePropertyName(branch.Key);
            writer.WriteNumberValue(branch.Value.Id);
        }

        writer.WriteEndObject();

        writer.WritePropertyName("fallback");
        writer.WriteNumberValue(node.FallbackQuery.Id);

        TryWriteConditions(writer, node);
        TryWriteNodeTrace(writer, operation, trace);

        writer.WriteEndObject();
    }

    private static void TryWriteNodeTrace(YamlWriter writer, Operation operation, ExecutionNodeTrace? trace)
    {
        if (trace is null)
        {
            return;
        }

        if (trace.SpanId is not null)
        {
            writer.WritePropertyName("spanId");
            writer.WriteStringValue(trace.SpanId);
        }

        writer.WritePropertyName("duration");
        writer.WriteNumberValue(trace.Duration.TotalMilliseconds);

        writer.WritePropertyName("status");
        writer.WriteStringValue(trace.Status.ToString());

        if (!trace.VariableSets.IsDefaultOrEmpty)
        {
            writer.WritePropertyName("variableSets");
            writer.WriteStartObject();

            foreach (var variableSet in trace.VariableSets)
            {
                writer.WritePropertyName(variableSet.Path.ToPath(operation).Print());
                writer.WriteLiteralStringValue(Encoding.UTF8.GetString(variableSet.Values.AsSequence()));
            }

            writer.WriteEndObject();
        }

        if (trace.Transport is { } transport)
        {
            writer.WritePropertyName("transport");
            writer.WriteStartObject();

            writer.WritePropertyName("uri");
            writer.WriteStringValue(transport.Uri.ToString());

            writer.WritePropertyName("contentType");
            writer.WriteStringValue(transport.ContentType);

            writer.WriteEndObject();
        }
    }

    private static void TryWriteConditions(YamlWriter writer, ExecutionNode node)
        => WriteConditions(node.Conditions, node.ConditionAlternatives, writer);

    private static string ToLiteralText(string text)
        => text.TrimEnd('\n') + "\n";
}
