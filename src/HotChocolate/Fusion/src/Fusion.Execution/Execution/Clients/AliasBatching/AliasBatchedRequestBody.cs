using System.Buffers;
using System.Text;
using System.Text.Json;
using HotChocolate.Fusion.Transport;
using HotChocolate.Fusion.Transport.Serialization;
using HotChocolate.Language;
using HotChocolate.Text.Json;

namespace HotChocolate.Fusion.Execution.Clients.AliasBatching;

/// <summary>
/// Represents the body of a batched request. The items of the batch are merged into one
/// spec-conformant GraphQL operation whose root selections are aliased per item.
/// </summary>
internal sealed class AliasBatchedRequestBody : IRequestBody
{
    private const byte NameSeparator = (byte)'_';
    private const int CompositionHashLength = 16;
    private const int StackBufferLength = 256;

    private static ReadOnlySpan<byte> AnonymousOperationName => "Op"u8;
    private static ReadOnlySpan<byte> Batch => "_Batch_"u8;

    private readonly AliasBatchItem[] _items;
    private readonly int _itemCount;
    private readonly List<Utf8VariableDefinitionNode> _sharedVariables;
    private readonly string? _operationName;
    private readonly string _operationShortHash;
    private readonly ulong _compositionHash;
    private readonly ErrorHandlingMode? _onError;

    /// <summary>
    /// Initializes a new instance of <see cref="AliasBatchedRequestBody"/>.
    /// </summary>
    /// <param name="items">The items that are merged into one operation.</param>
    /// <param name="itemCount">The number of items in <paramref name="items"/>.</param>
    /// <param name="sharedVariables">
    /// The variable definitions that the items share. They keep their name and are declared once.
    /// </param>
    /// <param name="operationName">
    /// The name of the client operation, or <see langword="null"/> when it is anonymous.
    /// </param>
    /// <param name="operationShortHash">
    /// The short hash of the client operation.
    /// </param>
    /// <param name="compositionHash">
    /// The hash of the batch composition.
    /// </param>
    /// <param name="onError">The requested error handling mode.</param>
    public AliasBatchedRequestBody(
        AliasBatchItem[] items,
        int itemCount,
        List<Utf8VariableDefinitionNode> sharedVariables,
        string? operationName,
        string operationShortHash,
        ulong compositionHash,
        ErrorHandlingMode? onError)
    {
        _items = items;
        _itemCount = itemCount;
        _sharedVariables = sharedVariables;
        _operationName = operationName;
        _operationShortHash = operationShortHash;
        _compositionHash = compositionHash;
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
    /// Writes the merged operation as a JSON string value.
    /// </summary>
    private void WriteQuery(JsonWriter writer, ReadOnlySpan<AliasBatchItem> items)
    {
        writer.WriteRawValueStart([]);

        var nameLength = GetOperationNameLength();
        byte[]? rented = null;
        Span<byte> stackBuffer = stackalloc byte[StackBufferLength];
        var nameBuffer = nameLength <= StackBufferLength
            ? stackBuffer[..nameLength]
            : (rented = ArrayPool<byte>.Shared.Rent(nameLength)).AsSpan(0, nameLength);

        try
        {
            var builder = Utf8OperationDocumentBuilder
                .New(writer.InnerWriter, formatAsJsonStringValue: true)
                .SetName(ComposeOperationName(nameBuffer));

            foreach (var sharedVariable in _sharedVariables)
            {
                builder = builder.AddSharedVariable(sharedVariable);
            }

            foreach (var item in items)
            {
                builder = builder.AddRootSelection(item.RootField, item.LookupTypeName);
            }

            builder.Complete();
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }

        writer.WriteRawValueEnd(JsonTokenType.String);
    }

    /// <summary>
    /// Gets the number of bytes the name of the merged operation takes.
    /// </summary>
    private int GetOperationNameLength()
        => (_operationName?.Length ?? AnonymousOperationName.Length)
            + 1
            + _operationShortHash.Length
            + Batch.Length
            + CompositionHashLength;

    /// <summary>
    /// Writes the name of the merged operation into <paramref name="buffer"/>. The name is derived
    /// from the client operation and the composition of the batch.
    /// </summary>
    private ReadOnlySpan<byte> ComposeOperationName(Span<byte> buffer)
    {
        int written;

        if (_operationName is null)
        {
            AnonymousOperationName.CopyTo(buffer);
            written = AnonymousOperationName.Length;
        }
        else
        {
            written = Encoding.ASCII.GetBytes(_operationName, buffer);
        }

        buffer[written++] = NameSeparator;
        written += Encoding.ASCII.GetBytes(_operationShortHash, buffer[written..]);
        Batch.CopyTo(buffer[written..]);
        written += Batch.Length;
        WriteHexLower(buffer[written..], _compositionHash);

        return buffer[..(written + CompositionHashLength)];
    }

    /// <summary>
    /// Writes <paramref name="value"/> as 16 lowercase hex digits.
    /// </summary>
    private static void WriteHexLower(Span<byte> buffer, ulong value)
    {
        for (var i = CompositionHashLength - 1; i >= 0; i--)
        {
            var digit = (int)(value & 0xF);
            buffer[i] = (byte)(digit < 10 ? '0' + digit : 'a' + digit - 10);
            value >>= 4;
        }
    }

    private static string GetErrorHandlingMode(ErrorHandlingMode mode)
        => mode switch
        {
            ErrorHandlingMode.Propagate => "PROPAGATE",
            ErrorHandlingMode.Null => "NULL",
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
}
