using System.Buffers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using HotChocolate.Buffers;

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

        jsonWriter.WritePropertyName("operation");
        WriteOperation(jsonWriter, plan, trace);

        jsonWriter.WritePropertyName("nodes");
        WriteNodes(jsonWriter, plan, trace);

        jsonWriter.WriteEndObject();
    }

    private static void WriteOperation(
        Utf8JsonWriter jsonWriter,
        OperationPlan plan,
        OperationPlanTrace? trace)
    {
        jsonWriter.WriteStartObject();
        jsonWriter.WriteString("document", plan.Operation.Definition.ToString(indented: true));

        if (!string.IsNullOrEmpty(plan.Operation.Name))
        {
            jsonWriter.WriteString("name", plan.Operation.Name);
        }

        jsonWriter.WriteString("hash", plan.Operation.Id);

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
        OperationPlan plan,
        OperationPlanTrace? trace)
    {
        jsonWriter.WriteStartArray();

        foreach (var node in plan.AllNodes)
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
        jsonWriter.WriteString("type", "Operation");
        jsonWriter.WriteString("schema", node.SchemaName);
        jsonWriter.WriteString("operation", node.Operation.ToString(indented: true));

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
                jsonWriter.WriteString("selectionMap", requirement.Map.ToString());
                jsonWriter.WriteEndObject();
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

    private static void WriteIntrospectionNode(
        Utf8JsonWriter jsonWriter,
        IntrospectionExecutionNode node,
        ExecutionNodeTrace? trace)
    {
        jsonWriter.WriteStartObject();
        jsonWriter.WriteNumber("id", node.Id);

        jsonWriter.WriteStartArray("selections");

        foreach (var selection in node.Selections)
        {
            jsonWriter.WriteStartObject();
            jsonWriter.WriteNumber("id", selection.Id);
            jsonWriter.WriteString("type", "Introspection");
            jsonWriter.WriteString("responseName", selection.ResponseName);
            jsonWriter.WriteString("fieldName", selection.Field.Name);
            jsonWriter.WriteEndObject();
        }

        jsonWriter.WriteEndArray();

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
}
