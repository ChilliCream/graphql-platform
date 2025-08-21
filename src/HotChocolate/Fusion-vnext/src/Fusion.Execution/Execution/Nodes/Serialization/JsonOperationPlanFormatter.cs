using System.Buffers;
using System.Collections.Immutable;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes.Serialization;

public sealed class JsonOperationPlanFormatter : OperationPlanFormatter
{
    private readonly JsonWriterOptions _writerOptions;

    public JsonOperationPlanFormatter(JsonWriterOptions? options = null)
    {
        _writerOptions = options ?? new JsonWriterOptions
        {
            Indented = false,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }

    public override string Format(OperationPlan plan, OperationPlanTrace? trace = null)
    {
        using var writer = new PooledArrayWriter();
        Format(writer, plan, trace);
        return Encoding.UTF8.GetString(writer.WrittenSpan);
    }

    public void Format(IBufferWriter<byte> writer, OperationPlan plan, OperationPlanTrace? trace = null)
    {
        using var jsonWriter = new Utf8JsonWriter(writer, _writerOptions);
        jsonWriter.WriteStartObject();

        jsonWriter.WriteString("id", plan.Id);

        jsonWriter.WritePropertyName("operation");
        WriteOperation(jsonWriter, plan.Operation, trace);

        jsonWriter.WritePropertyName("nodes");
        WriteNodes(jsonWriter, plan.AllNodes, trace);

        jsonWriter.WriteEndObject();
    }

    internal void Format(IBufferWriter<byte> writer, Operation operation, ImmutableArray<ExecutionNode> allNodes)
    {
        using var jsonWriter = new Utf8JsonWriter(writer, _writerOptions);
        jsonWriter.WriteStartObject();

        jsonWriter.WritePropertyName("operation");
        WriteOperation(jsonWriter, operation, null);

        jsonWriter.WritePropertyName("nodes");
        WriteNodes(jsonWriter, allNodes, null);

        jsonWriter.WriteEndObject();
    }

    private static void WriteOperation(
        Utf8JsonWriter jsonWriter,
        Operation operation,
        OperationPlanTrace? trace)
    {
        jsonWriter.WriteStartObject();

        if (!string.IsNullOrEmpty(operation.Name))
        {
            jsonWriter.WriteString("name", operation.Name);
        }

        jsonWriter.WriteString("kind", operation.Definition.Operation.ToString());
        jsonWriter.WriteString("document", operation.Definition.ToString(indented: true));

        jsonWriter.WriteString("id", operation.Id);
        jsonWriter.WriteString("hash", operation.Hash);
        jsonWriter.WriteString("shortHash", operation.Hash[..8]);

        if (trace is not null)
        {
            if (!string.IsNullOrEmpty(trace.AppId))
            {
                jsonWriter.WriteString("appId", trace.AppId);
            }

            if (!string.IsNullOrEmpty(trace.EnvironmentName))
            {
                jsonWriter.WriteString("environment", trace.EnvironmentName);
            }

            if (!string.IsNullOrEmpty(trace.TraceId))
            {
                jsonWriter.WriteString("traceId", trace.TraceId);
            }

            jsonWriter.WriteNumber("duration", trace.Duration.TotalMilliseconds);
        }

        jsonWriter.WriteEndObject();
    }

    private static void WriteNodes(
        Utf8JsonWriter jsonWriter,
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
                    WriteOperationNode(jsonWriter, operationNode, nodeTrace);
                    break;

                case IntrospectionExecutionNode introspectionNode:
                    WriteIntrospectionNode(jsonWriter, introspectionNode, nodeTrace);
                    break;

                case NodeExecutionNode nodeExecutionNode:
                    WriteNodeNode(jsonWriter, nodeExecutionNode, nodeTrace);
                    break;
            }
        }

        jsonWriter.WriteEndArray();
    }

    private static void WriteOperationNode(
        Utf8JsonWriter jsonWriter,
        OperationExecutionNode node,
        ExecutionNodeTrace? trace)
    {
        jsonWriter.WriteStartObject();
        jsonWriter.WriteNumber("id", node.Id);
        jsonWriter.WriteString("type", node.Type.ToString());
        jsonWriter.WriteString("schema", node.SchemaName);

        jsonWriter.WriteStartObject("operation");
        jsonWriter.WriteString("name", node.Operation.Name);
        jsonWriter.WriteString("type", node.Operation.Type.ToString());
        jsonWriter.WriteString("document", node.Operation.SourceText);
        jsonWriter.WriteString("hash", node.Operation.Hash);
        jsonWriter.WriteString("shortHash", node.Operation.Hash[..8]);
        jsonWriter.WriteEndObject();

        jsonWriter.WriteStartArray("responseNames");

        foreach (var responseName in node.ResponseNames)
        {
            jsonWriter.WriteStringValue(responseName);
        }

        jsonWriter.WriteEndArray();

        if (!node.Source.IsRoot)
        {
            jsonWriter.WriteString("source", node.Source.ToString());
        }

        if (!node.Target.IsRoot)
        {
            jsonWriter.WriteString("target", node.Target.ToString());
        }

        if (node.Requirements.Length > 0)
        {
            jsonWriter.WritePropertyName("requirements");
            jsonWriter.WriteStartArray();

            foreach (var requirement in node.Requirements)
            {
                jsonWriter.WriteStartObject();
                jsonWriter.WriteString("name", requirement.Key);
                jsonWriter.WriteString("type", requirement.Type.ToString());
                jsonWriter.WriteString("path", requirement.Path.ToString());
                jsonWriter.WriteString("selectionMap", requirement.Map.ToString());
                jsonWriter.WriteEndObject();
            }

            jsonWriter.WriteEndArray();
        }

        if (node.Requirements.Length > 0)
        {
            jsonWriter.WriteStartArray("forwardedVariables");

            foreach (var variableName in node.ForwardedVariables)
            {
                jsonWriter.WriteStringValue(variableName);
            }

            jsonWriter.WriteEndArray();
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

        TryWriteNodeTrace(jsonWriter, trace);

        jsonWriter.WriteEndObject();
    }

    private static void WriteIntrospectionNode(
        Utf8JsonWriter jsonWriter,
        IntrospectionExecutionNode node,
        ExecutionNodeTrace? trace)
    {
        jsonWriter.WriteStartObject();
        jsonWriter.WriteNumber("id", node.Id);
        jsonWriter.WriteString("type", node.Type.ToString());

        jsonWriter.WriteStartArray("selections");

        foreach (var selection in node.Selections)
        {
            jsonWriter.WriteStartObject();
            jsonWriter.WriteNumber("id", selection.Id);
            jsonWriter.WriteString("responseName", selection.ResponseName);
            jsonWriter.WriteString("fieldName", selection.Field.Name);
            jsonWriter.WriteEndObject();
        }

        jsonWriter.WriteEndArray();

        TryWriteNodeTrace(jsonWriter, trace);

        jsonWriter.WriteEndObject();
    }

    private static void TryWriteNodeTrace(Utf8JsonWriter jsonWriter, ExecutionNodeTrace? trace)
    {
        if (trace is not null)
        {
            if (!string.IsNullOrEmpty(trace.SpanId))
            {
                jsonWriter.WriteString("spanId", trace.SpanId);
            }

            jsonWriter.WriteNumber("duration", trace.Duration.TotalMilliseconds);
            jsonWriter.WriteString("status", trace.Status.ToString());

            if (trace.VariableSets.Length > 0)
            {
                jsonWriter.WriteStartObject("variableSets");

                foreach (var variableSet in trace.VariableSets)
                {
                    jsonWriter.WritePropertyName(variableSet.Path.ToString());
                    WriteObjectValueNode(jsonWriter, variableSet.Values);
                }

                jsonWriter.WriteEndObject();
            }
        }
    }

    private static void WriteObjectValueNode(Utf8JsonWriter jsonWriter, ObjectValueNode node)
    {
        jsonWriter.WriteStartObject();

        foreach (var field in node.Fields)
        {
            jsonWriter.WritePropertyName(field.Name.Value);
            WriteValueNode(jsonWriter, field.Value);
        }

        jsonWriter.WriteEndObject();
    }

    private static void WriteNodeNode(Utf8JsonWriter jsonWriter, NodeExecutionNode node, ExecutionNodeTrace? trace)
    {
        jsonWriter.WriteStartObject();
        jsonWriter.WriteNumber("id", node.Id);
        jsonWriter.WriteString("type", node.Type.ToString());

        jsonWriter.WriteString("idValue", node.IdValue.ToString());
        jsonWriter.WriteString("responseName", node.ResponseName);

        jsonWriter.WriteStartObject("branches");

        foreach (var branch in node.Branches)
        {
            jsonWriter.WriteNumber(branch.Key, branch.Value.Id);
        }

        jsonWriter.WriteEndObject();

        jsonWriter.WriteStartObject("fallback");
        jsonWriter.WriteString("name", node.FallbackQuery.Name);
        jsonWriter.WriteString("type", node.FallbackQuery.Type.ToString());
        jsonWriter.WriteString("document", node.FallbackQuery.SourceText);
        jsonWriter.WriteString("hash", node.FallbackQuery.Hash);
        jsonWriter.WriteString("shortHash", node.FallbackQuery.Hash[..8]);
        jsonWriter.WriteEndObject();

        if (trace is not null)
        {
            if (!string.IsNullOrEmpty(trace.SpanId))
            {
                jsonWriter.WriteString("spanId", trace.SpanId);
            }

            jsonWriter.WriteNumber("duration", trace.Duration.TotalMilliseconds);
            jsonWriter.WriteString("status", trace.Status.ToString());
        }

        jsonWriter.WriteEndObject();
    }

    private static void WriteValueNode(Utf8JsonWriter jsonWriter, IValueNode value)
    {
        switch (value)
        {
            case EnumValueNode enumValue:
                jsonWriter.WriteStringValue(enumValue.Value);
                break;

            case FloatValueNode floatValue:
                jsonWriter.WriteRawValue(floatValue.AsSpan());
                break;

            case IntValueNode intValue:
                jsonWriter.WriteRawValue(intValue.AsSpan());
                break;

            case BooleanValueNode booleanValue:
                jsonWriter.WriteBooleanValue(booleanValue.Value);
                break;

            case ListValueNode listValue:
                jsonWriter.WriteStartArray();

                foreach (var item in listValue.Items)
                {
                    WriteValueNode(jsonWriter, item);
                }

                jsonWriter.WriteEndArray();
                break;

            case NullValueNode:
                jsonWriter.WriteNullValue();
                break;

            case ObjectValueNode objectValue:
                WriteObjectValueNode(jsonWriter, objectValue);
                break;

            case StringValueNode stringValue:
                jsonWriter.WriteStringValue(stringValue.AsSpan());
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(value));
        }
    }
}
