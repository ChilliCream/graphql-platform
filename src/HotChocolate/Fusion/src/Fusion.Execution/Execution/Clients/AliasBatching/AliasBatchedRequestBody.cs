using System.Buffers;
using System.Buffers.Text;
using System.Text.Json;
using HotChocolate.Fusion.Transport;
using HotChocolate.Fusion.Transport.Serialization;
using HotChocolate.Language;
using HotChocolate.Text.Json;

namespace HotChocolate.Fusion.Execution.Clients.AliasBatching;

/// <summary>
/// Represents the body of a batched request. The items of the batch are merged into one
/// spec-conformant GraphQL operation whose root selections are aliased per item, and the merged
/// operation is written straight into the request buffer when the transport serializes the body.
/// </summary>
internal sealed class AliasBatchedRequestBody : IRequestBody
{
    // 'Batch_' plus the 16 hexadecimal digits of the operation hash.
    private const int OperationNameLength = 22;

    private readonly AliasBatchItem[] _items;
    private readonly int _itemCount;
    private readonly List<Utf8VariableDefinitionNode> _sharedVariables;
    private readonly ulong _operationHash;
    private readonly ErrorHandlingMode? _onError;

    /// <summary>
    /// Initializes a new instance of <see cref="AliasBatchedRequestBody"/>.
    /// </summary>
    /// <param name="items">The items that are merged into one operation.</param>
    /// <param name="itemCount">The number of items in <paramref name="items"/>.</param>
    /// <param name="sharedVariables">
    /// The variable definitions that the items share. A shared variable keeps its name and is
    /// declared once for the whole operation.
    /// </param>
    /// <param name="operationHash">The hash the generated operation name is derived from.</param>
    /// <param name="onError">The requested error handling mode.</param>
    public AliasBatchedRequestBody(
        AliasBatchItem[] items,
        int itemCount,
        List<Utf8VariableDefinitionNode> sharedVariables,
        ulong operationHash,
        ErrorHandlingMode? onError)
    {
        _items = items;
        _itemCount = itemCount;
        _sharedVariables = sharedVariables;
        _operationHash = operationHash;
        _onError = onError;
    }

    /// <inheritdoc />
    public void WriteTo(JsonWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        var items = _items.AsSpan(0, _itemCount);

        writer.WriteStartObject();

        writer.WritePropertyName(Utf8GraphQLRequestProperties.QueryProp);
        WriteQuery(writer, items);

        writer.WritePropertyName(Utf8GraphQLRequestProperties.VariablesProp);
        AliasBatchVariableWriter.Write(writer, items, _sharedVariables);

        if (_onError is { } onError)
        {
            writer.WritePropertyName(Utf8GraphQLRequestProperties.OnErrorProp);
            writer.WriteStringValue(GetErrorHandlingMode(onError));
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Writes the merged operation as a JSON string value. The builder escapes while it walks, so
    /// the complete string literal is placed into the writer's output without an intermediate
    /// buffer.
    /// </summary>
    private void WriteQuery(JsonWriter writer, ReadOnlySpan<AliasBatchItem> items)
    {
        writer.WriteRawValueStart([]);

        Span<byte> operationName = stackalloc byte[OperationNameLength];
        var builder = Utf8OperationDocumentBuilder
            .New(writer.InnerWriter, formatAsJsonStringValue: true)
            .SetName(ComposeOperationName(operationName, _operationHash));

        foreach (var sharedVariable in _sharedVariables)
        {
            builder = builder.AddSharedVariable(sharedVariable);
        }

        foreach (var item in items)
        {
            builder = builder.AddRootSelection(item.RootField, item.LookupTypeName);
        }

        builder.Complete();

        writer.WriteRawValueEnd(JsonTokenType.String);
    }

    private static ReadOnlySpan<byte> ComposeOperationName(Span<byte> buffer, ulong operationHash)
    {
        "Batch_"u8.CopyTo(buffer);
        Utf8Formatter.TryFormat(operationHash, buffer[6..], out var written, new StandardFormat('x', 16));
        return buffer[..(6 + written)];
    }

    private static string GetErrorHandlingMode(ErrorHandlingMode mode)
        => mode switch
        {
            ErrorHandlingMode.Propagate => "PROPAGATE",
            ErrorHandlingMode.Null => "NULL",
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
}
