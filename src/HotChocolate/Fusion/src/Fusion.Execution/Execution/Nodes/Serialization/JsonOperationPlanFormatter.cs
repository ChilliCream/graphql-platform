using System.Buffers;
using System.Collections.Immutable;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using HotChocolate.Buffers;
using JsonWriter = HotChocolate.Text.Json.JsonWriter;

namespace HotChocolate.Fusion.Execution.Nodes.Serialization;

/// <summary>
/// Formats an <see cref="OperationPlan"/> as a JSON document,
/// including its operation metadata, execution nodes, and optional trace information.
/// </summary>
/// <param name="options">
/// Optional <see cref="JsonWriterOptions"/> to control JSON formatting.
/// Defaults to compact (non-indented) output with relaxed encoding.
/// </param>
public sealed class JsonOperationPlanFormatter(JsonWriterOptions? options = null) : OperationPlanFormatter
{
    private readonly JsonWriterOptions _writerOptions = options ?? new JsonWriterOptions
    {
        Indented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <inheritdoc />
    public override string Format(OperationPlan plan, OperationPlanTrace? trace = null)
    {
        using var writer = new PooledArrayWriter();
        Format(writer, plan, trace);
        return Encoding.UTF8.GetString(writer.WrittenSpan);
    }

    /// <summary>
    /// Formats the specified <paramref name="plan"/> as JSON and writes the
    /// UTF-8 encoded output to <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">The buffer writer to receive the JSON output.</param>
    /// <param name="plan">The operation plan to format.</param>
    /// <param name="trace">Optional trace information to include in the output.</param>
    public void Format(IBufferWriter<byte> writer, OperationPlan plan, OperationPlanTrace? trace = null)
    {
        var jsonWriter = new JsonWriter(writer, _writerOptions);
        jsonWriter.WriteStartObject();

        jsonWriter.WritePropertyName("id");
        jsonWriter.WriteStringValue(plan.Id);

        jsonWriter.WritePropertyName("operation");
        WriteOperation(jsonWriter, plan.Operation);

        jsonWriter.WritePropertyName("searchSpace");
        jsonWriter.WriteNumberValue(plan.SearchSpace);

        jsonWriter.WritePropertyName("expandedNodes");
        jsonWriter.WriteNumberValue(plan.ExpandedNodes);

        if (trace is not null)
        {
            if (!string.IsNullOrEmpty(trace.AppId))
            {
                jsonWriter.WritePropertyName("appId");
                jsonWriter.WriteStringValue(trace.AppId);
            }

            if (!string.IsNullOrEmpty(trace.EnvironmentName))
            {
                jsonWriter.WritePropertyName("environment");
                jsonWriter.WriteStringValue(trace.EnvironmentName);
            }

            if (!string.IsNullOrEmpty(trace.TraceId))
            {
                jsonWriter.WritePropertyName("traceId");
                jsonWriter.WriteStringValue(trace.TraceId);
            }

            jsonWriter.WritePropertyName("duration");
            jsonWriter.WriteNumberValue(trace.Duration.TotalMilliseconds);
        }

        jsonWriter.WritePropertyName("nodes");
        WriteNodes(jsonWriter, plan.Operation, plan.AllNodes, trace);

        jsonWriter.WriteEndObject();
    }

    internal void Format(IBufferWriter<byte> writer, Operation operation, ImmutableArray<ExecutionNode> allNodes)
    {
        var jsonWriter = new JsonWriter(writer, _writerOptions);
        jsonWriter.WriteStartObject();

        jsonWriter.WritePropertyName("operation");
        WriteOperation(jsonWriter, operation);

        jsonWriter.WritePropertyName("nodes");
        WriteNodes(jsonWriter, operation, allNodes, null);

        jsonWriter.WriteEndObject();
    }

    private static void WriteOperation(
        JsonWriter jsonWriter,
        Operation operation)
    {
        jsonWriter.WriteStartObject();

        if (!string.IsNullOrEmpty(operation.Name))
        {
            jsonWriter.WritePropertyName("name");
            jsonWriter.WriteStringValue(operation.Name);
        }

        jsonWriter.WritePropertyName("kind");
        jsonWriter.WriteStringValue(operation.Definition.Operation.ToString());

        jsonWriter.WritePropertyName("document");
        jsonWriter.WriteStringValue(operation.Definition.ToString(indented: true));

        jsonWriter.WritePropertyName("id");
        jsonWriter.WriteStringValue(operation.Id);

        jsonWriter.WritePropertyName("hash");
        jsonWriter.WriteStringValue(operation.Hash);

        jsonWriter.WritePropertyName("shortHash");
        jsonWriter.WriteStringValue(operation.Hash[..8]);

        jsonWriter.WriteEndObject();
    }

    private static void WriteNodes(
        JsonWriter jsonWriter,
        Operation operation,
        ImmutableArray<ExecutionNode> allNodes,
        OperationPlanTrace? trace)
    {
        jsonWriter.WriteStartArray();

        foreach (var node in allNodes)
        {
            ExecutionNodeTrace? nodeTrace = null;
            trace?.Nodes.TryGetValue(node.Id, out nodeTrace);

            switch (node)
            {
                case OperationExecutionNode operationNode:
                    WriteOperationNode(jsonWriter, operation, operationNode, nodeTrace);
                    break;

                case OperationBatchExecutionNode batchNode:
                    WriteBatchExecutionNode(jsonWriter, operation, batchNode, nodeTrace);
                    break;

                case IntrospectionExecutionNode introspectionNode:
                    WriteIntrospectionNode(jsonWriter, operation, introspectionNode, nodeTrace);
                    break;

                case NodeFieldExecutionNode nodeExecutionNode:
                    WriteNodeFieldNode(jsonWriter, operation, nodeExecutionNode, nodeTrace);
                    break;
            }
        }

        jsonWriter.WriteEndArray();
    }

    private static void WriteOperationNode(
        JsonWriter jsonWriter,
        Operation operation,
        OperationExecutionNode node,
        ExecutionNodeTrace? trace)
    {
        jsonWriter.WriteStartObject();

        jsonWriter.WritePropertyName("id");
        jsonWriter.WriteNumberValue(node.Id);

        jsonWriter.WritePropertyName("type");
        jsonWriter.WriteStringValue(node.Type.ToString());

        if (!string.IsNullOrEmpty(node.SchemaName))
        {
            jsonWriter.WritePropertyName("schema");
            jsonWriter.WriteStringValue(node.SchemaName);
        }

        jsonWriter.WritePropertyName("operation");
        jsonWriter.WriteStartObject();

        jsonWriter.WritePropertyName("name");
        jsonWriter.WriteStringValue(node.Operation.Name);

        jsonWriter.WritePropertyName("kind");
        jsonWriter.WriteStringValue(node.Operation.Type.ToString());

        jsonWriter.WritePropertyName("document");
        jsonWriter.WriteStringValue(node.Operation.SourceText);

        jsonWriter.WritePropertyName("hash");
        jsonWriter.WriteStringValue(node.Operation.Hash);

        jsonWriter.WritePropertyName("shortHash");
        jsonWriter.WriteStringValue(node.Operation.Hash[..8]);

        jsonWriter.WriteEndObject();

        jsonWriter.WritePropertyName("resultSelectionSet");
        jsonWriter.WriteStringValue(node.ResultSelectionSet.ToString(indented: false));

        if (!node.Source.IsRoot)
        {
            jsonWriter.WritePropertyName("source");
            jsonWriter.WriteStringValue(node.Source.ToString());
        }

        if (!node.Target.IsRoot)
        {
            jsonWriter.WritePropertyName("target");
            jsonWriter.WriteStringValue(node.Target.ToString());
        }

        if (node.Requirements.Length > 0)
        {
            jsonWriter.WritePropertyName("requirements");
            jsonWriter.WriteStartArray();

            foreach (var requirement in node.Requirements)
            {
                jsonWriter.WriteStartObject();

                jsonWriter.WritePropertyName("name");
                jsonWriter.WriteStringValue(requirement.Key);

                jsonWriter.WritePropertyName("type");
                jsonWriter.WriteStringValue(requirement.Type.ToString());

                jsonWriter.WritePropertyName("path");
                jsonWriter.WriteStringValue(requirement.Path.ToString());

                jsonWriter.WritePropertyName("selectionMap");
                jsonWriter.WriteStringValue(requirement.Map.ToString());

                jsonWriter.WriteEndObject();
            }

            jsonWriter.WriteEndArray();
        }

        TryWriteConditions(jsonWriter, node);

        if (node.ForwardedVariables.Length > 0)
        {
            jsonWriter.WritePropertyName("forwardedVariables");
            jsonWriter.WriteStartArray();

            foreach (var variableName in node.ForwardedVariables)
            {
                jsonWriter.WriteStringValue(variableName);
            }

            jsonWriter.WriteEndArray();
        }

        if (node.RequiresFileUpload)
        {
            jsonWriter.WritePropertyName("requiresFileUpload");
            jsonWriter.WriteBooleanValue(true);
        }

        if (node.Dependencies.Length > 0)
        {
            jsonWriter.WritePropertyName("dependencies");
            jsonWriter.WriteStartArray();

            foreach (var dependency in node.Dependencies)
            {
                jsonWriter.WriteNumberValue(dependency.Id);
            }

            jsonWriter.WriteEndArray();
        }

        TryWriteNodeTrace(jsonWriter, operation, trace);

        jsonWriter.WriteEndObject();
    }

    private static void WriteBatchExecutionNode(
        JsonWriter jsonWriter,
        Operation operation,
        OperationBatchExecutionNode batchNode,
        ExecutionNodeTrace? trace)
    {
        // Each operation within the batch is serialized as its own node entry,
        // using the batch node's ID as batchGroupId to preserve the grouping.
        foreach (var operationDef in batchNode.Operations)
        {
            switch (operationDef)
            {
                case SingleOperationDefinition single:
                    WriteOperationDefinitionAsNode(jsonWriter, operation, batchNode, single, trace);
                    break;

                case BatchOperationDefinition batch:
                    WriteBatchOperationDefinitionAsNode(jsonWriter, operation, batchNode, batch, trace);
                    break;
            }
        }
    }

    private static void WriteOperationDefinitionAsNode(
        JsonWriter jsonWriter,
        Operation operation,
        OperationBatchExecutionNode batchNode,
        SingleOperationDefinition operationDef,
        ExecutionNodeTrace? trace)
    {
        jsonWriter.WriteStartObject();

        jsonWriter.WritePropertyName("id");
        jsonWriter.WriteNumberValue(operationDef.Id);

        jsonWriter.WritePropertyName("type");
        jsonWriter.WriteStringValue(nameof(ExecutionNodeType.Operation));

        if (!string.IsNullOrEmpty(operationDef.SchemaName))
        {
            jsonWriter.WritePropertyName("schema");
            jsonWriter.WriteStringValue(operationDef.SchemaName);
        }

        jsonWriter.WritePropertyName("operation");
        jsonWriter.WriteStartObject();

        jsonWriter.WritePropertyName("name");
        jsonWriter.WriteStringValue(operationDef.Operation.Name);

        jsonWriter.WritePropertyName("kind");
        jsonWriter.WriteStringValue(operationDef.Operation.Type.ToString());

        jsonWriter.WritePropertyName("document");
        jsonWriter.WriteStringValue(operationDef.Operation.SourceText);

        jsonWriter.WritePropertyName("hash");
        jsonWriter.WriteStringValue(operationDef.Operation.Hash);

        jsonWriter.WritePropertyName("shortHash");
        jsonWriter.WriteStringValue(operationDef.Operation.Hash[..8]);

        jsonWriter.WriteEndObject();

        jsonWriter.WritePropertyName("resultSelectionSet");
        jsonWriter.WriteStringValue(operationDef.ResultSelectionSet.ToString(indented: false));

        if (!operationDef.Source.IsRoot)
        {
            jsonWriter.WritePropertyName("source");
            jsonWriter.WriteStringValue(operationDef.Source.ToString());
        }

        if (!operationDef.Target.IsRoot)
        {
            jsonWriter.WritePropertyName("target");
            jsonWriter.WriteStringValue(operationDef.Target.ToString());
        }

        jsonWriter.WritePropertyName("batchingGroupId");
        jsonWriter.WriteNumberValue(batchNode.Id);

        if (operationDef.Requirements.Length > 0)
        {
            jsonWriter.WritePropertyName("requirements");
            jsonWriter.WriteStartArray();

            foreach (var requirement in operationDef.Requirements)
            {
                jsonWriter.WriteStartObject();

                jsonWriter.WritePropertyName("name");
                jsonWriter.WriteStringValue(requirement.Key);

                jsonWriter.WritePropertyName("type");
                jsonWriter.WriteStringValue(requirement.Type.ToString());

                jsonWriter.WritePropertyName("path");
                jsonWriter.WriteStringValue(requirement.Path.ToString());

                jsonWriter.WritePropertyName("selectionMap");
                jsonWriter.WriteStringValue(requirement.Map.ToString());

                jsonWriter.WriteEndObject();
            }

            jsonWriter.WriteEndArray();
        }

        WriteConditions(jsonWriter, operationDef.Conditions);

        if (operationDef.ForwardedVariables.Length > 0)
        {
            jsonWriter.WritePropertyName("forwardedVariables");
            jsonWriter.WriteStartArray();

            foreach (var variableName in operationDef.ForwardedVariables)
            {
                jsonWriter.WriteStringValue(variableName);
            }

            jsonWriter.WriteEndArray();
        }

        if (operationDef.RequiresFileUpload)
        {
            jsonWriter.WritePropertyName("requiresFileUpload");
            jsonWriter.WriteBooleanValue(true);
        }

        if (operationDef.Dependencies.Length > 0)
        {
            jsonWriter.WritePropertyName("dependencies");
            jsonWriter.WriteStartArray();

            foreach (var dependency in operationDef.Dependencies)
            {
                jsonWriter.WriteNumberValue(dependency.Id);
            }

            jsonWriter.WriteEndArray();
        }

        TryWriteNodeTrace(jsonWriter, operation, trace);

        jsonWriter.WriteEndObject();
    }

    private static void WriteBatchOperationDefinitionAsNode(
        JsonWriter jsonWriter,
        Operation operation,
        OperationBatchExecutionNode batchNode,
        BatchOperationDefinition operationDef,
        ExecutionNodeTrace? trace)
    {
        jsonWriter.WriteStartObject();

        jsonWriter.WritePropertyName("id");
        jsonWriter.WriteNumberValue(operationDef.Id);

        jsonWriter.WritePropertyName("type");
        jsonWriter.WriteStringValue(ExecutionNodeType.OperationBatch.ToString());

        if (!string.IsNullOrEmpty(operationDef.SchemaName))
        {
            jsonWriter.WritePropertyName("schema");
            jsonWriter.WriteStringValue(operationDef.SchemaName);
        }

        jsonWriter.WritePropertyName("operation");
        jsonWriter.WriteStartObject();

        jsonWriter.WritePropertyName("name");
        jsonWriter.WriteStringValue(operationDef.Operation.Name);

        jsonWriter.WritePropertyName("kind");
        jsonWriter.WriteStringValue(operationDef.Operation.Type.ToString());

        jsonWriter.WritePropertyName("document");
        jsonWriter.WriteStringValue(operationDef.Operation.SourceText);

        jsonWriter.WritePropertyName("hash");
        jsonWriter.WriteStringValue(operationDef.Operation.Hash);

        jsonWriter.WritePropertyName("shortHash");
        jsonWriter.WriteStringValue(operationDef.Operation.Hash[..8]);

        jsonWriter.WriteEndObject();

        jsonWriter.WritePropertyName("resultSelectionSet");
        jsonWriter.WriteStringValue(operationDef.ResultSelectionSet.ToString(indented: false));

        if (!operationDef.Source.IsRoot)
        {
            jsonWriter.WritePropertyName("source");
            jsonWriter.WriteStringValue(operationDef.Source.ToString());
        }

        jsonWriter.WritePropertyName("targets");
        jsonWriter.WriteStartArray();

        foreach (var target in operationDef.Targets)
        {
            jsonWriter.WriteStringValue(target.ToString());
        }

        jsonWriter.WriteEndArray();

        jsonWriter.WritePropertyName("batchingGroupId");
        jsonWriter.WriteNumberValue(batchNode.Id);

        if (operationDef.Requirements.Length > 0)
        {
            jsonWriter.WritePropertyName("requirements");
            jsonWriter.WriteStartArray();

            foreach (var requirement in operationDef.Requirements)
            {
                jsonWriter.WriteStartObject();

                jsonWriter.WritePropertyName("name");
                jsonWriter.WriteStringValue(requirement.Key);

                jsonWriter.WritePropertyName("type");
                jsonWriter.WriteStringValue(requirement.Type.ToString());

                jsonWriter.WritePropertyName("path");
                jsonWriter.WriteStringValue(requirement.Path.ToString());

                jsonWriter.WritePropertyName("selectionMap");
                jsonWriter.WriteStringValue(requirement.Map.ToString());

                jsonWriter.WriteEndObject();
            }

            jsonWriter.WriteEndArray();
        }

        WriteConditions(jsonWriter, operationDef.Conditions);

        if (operationDef.ForwardedVariables.Length > 0)
        {
            jsonWriter.WritePropertyName("forwardedVariables");
            jsonWriter.WriteStartArray();

            foreach (var variableName in operationDef.ForwardedVariables)
            {
                jsonWriter.WriteStringValue(variableName);
            }

            jsonWriter.WriteEndArray();
        }

        if (operationDef.RequiresFileUpload)
        {
            jsonWriter.WritePropertyName("requiresFileUpload");
            jsonWriter.WriteBooleanValue(true);
        }

        if (operationDef.Dependencies.Length > 0)
        {
            jsonWriter.WritePropertyName("dependencies");
            jsonWriter.WriteStartArray();

            foreach (var dependency in operationDef.Dependencies)
            {
                jsonWriter.WriteNumberValue(dependency.Id);
            }

            jsonWriter.WriteEndArray();
        }

        TryWriteNodeTrace(jsonWriter, operation, trace);

        jsonWriter.WriteEndObject();
    }

    private static void WriteConditions(JsonWriter jsonWriter, ReadOnlySpan<ExecutionNodeCondition> conditions)
    {
        if (conditions.Length > 0)
        {
            jsonWriter.WritePropertyName("conditions");
            jsonWriter.WriteStartArray();

            foreach (var condition in conditions)
            {
                jsonWriter.WriteStartObject();

                jsonWriter.WritePropertyName("variable");
                jsonWriter.WriteStringValue("$" + condition.VariableName);

                jsonWriter.WritePropertyName("passingValue");
                jsonWriter.WriteBooleanValue(condition.PassingValue);

                jsonWriter.WriteEndObject();
            }

            jsonWriter.WriteEndArray();
        }
    }

    private static void WriteIntrospectionNode(
        JsonWriter jsonWriter,
        Operation operation,
        IntrospectionExecutionNode node,
        ExecutionNodeTrace? trace)
    {
        jsonWriter.WriteStartObject();

        jsonWriter.WritePropertyName("id");
        jsonWriter.WriteNumberValue(node.Id);

        jsonWriter.WritePropertyName("type");
        jsonWriter.WriteStringValue(node.Type.ToString());

        jsonWriter.WritePropertyName("selections");
        jsonWriter.WriteStartArray();

        foreach (var selection in node.Selections)
        {
            jsonWriter.WriteStartObject();

            jsonWriter.WritePropertyName("id");
            jsonWriter.WriteNumberValue(selection.Id);

            jsonWriter.WritePropertyName("responseName");
            jsonWriter.WriteStringValue(selection.ResponseName);

            jsonWriter.WritePropertyName("fieldName");
            jsonWriter.WriteStringValue(selection.Field.Name);

            jsonWriter.WriteEndObject();
        }

        jsonWriter.WriteEndArray();

        TryWriteConditions(jsonWriter, node);

        TryWriteNodeTrace(jsonWriter, operation, trace);

        jsonWriter.WriteEndObject();
    }

    private static void WriteNodeFieldNode(
        JsonWriter jsonWriter,
        Operation operation,
        NodeFieldExecutionNode node,
        ExecutionNodeTrace? trace)
    {
        jsonWriter.WriteStartObject();

        jsonWriter.WritePropertyName("id");
        jsonWriter.WriteNumberValue(node.Id);

        jsonWriter.WritePropertyName("type");
        jsonWriter.WriteStringValue(node.Type.ToString());

        jsonWriter.WritePropertyName("idValue");
        jsonWriter.WriteStringValue(node.IdValue.ToString());

        jsonWriter.WritePropertyName("responseName");
        jsonWriter.WriteStringValue(node.ResponseName);

        jsonWriter.WritePropertyName("branches");
        jsonWriter.WriteStartObject();

        foreach (var branch in node.Branches.OrderBy(kvp => kvp.Key))
        {
            jsonWriter.WritePropertyName(branch.Key);
            jsonWriter.WriteNumberValue(branch.Value.Id);
        }

        jsonWriter.WriteEndObject();

        jsonWriter.WritePropertyName("fallback");
        jsonWriter.WriteNumberValue(node.FallbackQuery.Id);

        TryWriteConditions(jsonWriter, node);

        TryWriteNodeTrace(jsonWriter, operation, trace);

        jsonWriter.WriteEndObject();
    }

    private static void TryWriteNodeTrace(JsonWriter jsonWriter, Operation operation, ExecutionNodeTrace? trace)
    {
        if (trace is not null)
        {
            if (!string.IsNullOrEmpty(trace.SpanId))
            {
                jsonWriter.WritePropertyName("spanId");
                jsonWriter.WriteStringValue(trace.SpanId);
            }

            jsonWriter.WritePropertyName("duration");
            jsonWriter.WriteNumberValue(trace.Duration.TotalMilliseconds);

            jsonWriter.WritePropertyName("status");
            jsonWriter.WriteStringValue(trace.Status.ToString());

            if (trace.VariableSets.Length > 0)
            {
                jsonWriter.WritePropertyName("variableSets");
                jsonWriter.WriteStartObject();

                foreach (var variableSet in trace.VariableSets)
                {
                    jsonWriter.WritePropertyName(variableSet.Path.ToPath(operation).Print());
                    variableSet.Values.WriteTo(jsonWriter);
                }

                jsonWriter.WriteEndObject();
            }

            if (trace.Transport is not null)
            {
                jsonWriter.WritePropertyName("transport");
                jsonWriter.WriteStartObject();

                jsonWriter.WritePropertyName("uri");
                jsonWriter.WriteStringValue(trace.Transport.Uri.ToString());

                jsonWriter.WritePropertyName("contentType");
                jsonWriter.WriteStringValue(trace.Transport.ContentType);

                jsonWriter.WriteEndObject();
            }
        }
    }

    private static void TryWriteConditions(JsonWriter jsonWriter, ExecutionNode node)
    {
        if (node.Conditions.Length > 0)
        {
            jsonWriter.WritePropertyName("conditions");
            jsonWriter.WriteStartArray();

            foreach (var condition in node.Conditions)
            {
                jsonWriter.WriteStartObject();

                jsonWriter.WritePropertyName("variable");
                jsonWriter.WriteStringValue("$" + condition.VariableName);

                jsonWriter.WritePropertyName("passingValue");
                jsonWriter.WriteBooleanValue(condition.PassingValue);

                jsonWriter.WriteEndObject();
            }

            jsonWriter.WriteEndArray();
        }
    }
}
