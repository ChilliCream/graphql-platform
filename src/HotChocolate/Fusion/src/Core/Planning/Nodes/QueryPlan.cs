using System.Buffers;
using System.Text;
using HotChocolate.Execution.Processing;
using System.Text.Json;
using HotChocolate.Fusion.Execution;

namespace HotChocolate.Fusion.Planning;

internal sealed class QueryPlan
{
    private readonly IOperation _operation;
    private readonly IReadOnlyDictionary<ISelectionSet, string[]> _exportKeysLookup;
    private readonly IReadOnlySet<ISelectionSet> _hasNodes;

    public QueryPlan(
        IOperation operation,
        QueryPlanNode rootNode,
        IReadOnlyDictionary<ISelectionSet, string[]> exportKeysLookup,
        IReadOnlySet<ISelectionSet> hasNodes)
    {
        _operation = operation;
        _exportKeysLookup = exportKeysLookup;
        _hasNodes = hasNodes;
        RootNode = rootNode;
    }

    public QueryPlanNode RootNode { get; }

    public bool HasNodes(ISelectionSet selectionSet)
        => _hasNodes.Contains(selectionSet);

    public IReadOnlyList<string> GetExportKeys(ISelectionSet selectionSet)
        => _exportKeysLookup.TryGetValue(selectionSet, out var keys) ? keys : Array.Empty<string>();

    public async Task ExecuteAsync(
        IFederationContext context,
        CancellationToken cancellationToken)
    {
        await RootNode.ExecuteAsync(context, cancellationToken);
    }

    public void Format(IBufferWriter<byte> writer)
    {
        var jsonOptions = new JsonWriterOptions { Indented = true };
        using var jsonWriter = new Utf8JsonWriter(writer, jsonOptions);
        Format(jsonWriter);
        jsonWriter.Flush();
    }

    public void Format(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();

        writer.WriteString("document", _operation.Document.ToString(false));

        if (!string.IsNullOrEmpty(_operation.Name))
        {
            writer.WriteString("operation", _operation.Name);
        }

        writer.WritePropertyName("rootNode");
        RootNode.Format(writer);

        writer.WriteEndObject();
    }

    public override string ToString()
    {
        var bufferWriter = new ArrayBufferWriter<byte>();
        var jsonOptions = new JsonWriterOptions { Indented = true };
        using var jsonWriter = new Utf8JsonWriter(bufferWriter, jsonOptions);

        Format(jsonWriter);
        jsonWriter.Flush();

        return Encoding.UTF8.GetString(bufferWriter.WrittenSpan);
    }
}
