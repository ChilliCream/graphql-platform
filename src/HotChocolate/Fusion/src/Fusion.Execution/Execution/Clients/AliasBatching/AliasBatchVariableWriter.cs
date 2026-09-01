using System.Buffers;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Text.Json;

namespace HotChocolate.Fusion.Execution.Clients.AliasBatching;

/// <summary>
/// Writes the <c>variables</c> object of a batched request. A shared variable keeps its name and
/// is written once; every other variable is written under the prefixed name of its item.
/// </summary>
internal static class AliasBatchVariableWriter
{
    private const int StackBufferLength = 256;

    /// <summary>
    /// Writes the merged variable values of <paramref name="items"/>.
    /// </summary>
    /// <param name="writer">The writer that receives the variables object.</param>
    /// <param name="items">The items of the batched request.</param>
    /// <param name="sharedVariables">The variable definitions that the items share.</param>
    public static void Write(
        JsonWriter writer,
        ReadOnlySpan<AliasBatchItem> items,
        List<Utf8VariableDefinitionNode> sharedVariables)
    {
        var sharedWritten = sharedVariables.Count == 0
            ? []
            : new bool[sharedVariables.Count];

        writer.WriteStartObject();

        for (var i = 0; i < items.Length; i++)
        {
            var values = items[i].Variables.Values;

            if (values.IsEmpty)
            {
                continue;
            }

            WriteItem(writer, i, values.AsSequence(), sharedVariables, sharedWritten);
        }

        writer.WriteEndObject();
    }

    private static void WriteItem(
        JsonWriter writer,
        int index,
        ReadOnlySequence<byte> values,
        List<Utf8VariableDefinitionNode> sharedVariables,
        Span<bool> sharedWritten)
    {
        var reader = new Utf8JsonReader(values, isFinalBlock: true, state: default);

        if (!reader.Read() || reader.TokenType is not JsonTokenType.StartObject)
        {
            return;
        }

        // A gathered name is still read while the prefixed name is composed from it, so the two
        // buffers must stay separate.
        Span<byte> prefixedNameStackBuffer = stackalloc byte[StackBufferLength];
        Span<byte> gatheredNameStackBuffer = stackalloc byte[StackBufferLength];
        var prefixedNameBuffer = new ScratchBuffer(prefixedNameStackBuffer);
        var gatheredNameBuffer = new ScratchBuffer(gatheredNameStackBuffer);

        try
        {
            while (reader.Read() && reader.TokenType is JsonTokenType.PropertyName)
            {
                // A name that crosses a chunk boundary is gathered before it is compared and
                // written; the value it names never is.
                var name = reader.HasValueSequence
                    ? Gather(reader.ValueSequence, gatheredNameBuffer.GetSpan((int)reader.ValueSequence.Length))
                    : reader.ValueSpan;

                var sharedIndex = IndexOfShared(sharedVariables, name);

                reader.Read();
                var valueStart = reader.TokenStartIndex;
                reader.Skip();
                var value = values.Slice(valueStart, reader.BytesConsumed - valueStart);

                if (sharedIndex >= 0)
                {
                    if (sharedWritten[sharedIndex])
                    {
                        continue;
                    }

                    sharedWritten[sharedIndex] = true;
                    writer.WritePropertyName(name);
                }
                else
                {
                    var buffer = prefixedNameBuffer.GetSpan(AliasBatchPrefixHelper.GetNameBufferLength(name.Length));
                    writer.WritePropertyName(AliasBatchPrefixHelper.ComposeName(buffer, index, name));
                }

                WriteValue(writer, value);
            }
        }
        finally
        {
            prefixedNameBuffer.Dispose();
            gatheredNameBuffer.Dispose();
        }
    }

    /// <summary>
    /// Copies bytes that are stored in several chunks into one buffer.
    /// </summary>
    /// <param name="value">The bytes to gather.</param>
    /// <param name="destination">The buffer that receives them.</param>
    private static ReadOnlySpan<byte> Gather(ReadOnlySequence<byte> value, Span<byte> destination)
    {
        value.CopyTo(destination);
        return destination[..(int)value.Length];
    }

    /// <summary>
    /// Writes the value of one variable, chunk by chunk when it crosses a chunk boundary.
    /// </summary>
    private static void WriteValue(JsonWriter writer, ReadOnlySequence<byte> value)
    {
        if (value.IsSingleSegment)
        {
            writer.WriteRawValue(value.FirstSpan);
            return;
        }

        var isFirst = true;

        foreach (var segment in value)
        {
            if (isFirst)
            {
                isFirst = false;
                writer.WriteRawValueStart(segment.Span);
                continue;
            }

            writer.WriteRawValueContinuation(segment.Span);
        }

        writer.WriteRawValueEnd();
    }

    private static int IndexOfShared(
        List<Utf8VariableDefinitionNode> sharedVariables,
        ReadOnlySpan<byte> name)
    {
        for (var i = 0; i < sharedVariables.Count; i++)
        {
            if (sharedVariables[i].Utf8Name.SequenceEqual(name))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Hands out spans of a requested length, backed by the caller's stack buffer while the
    /// length fits into it and by a pooled array beyond it.
    /// </summary>
    private ref struct ScratchBuffer(Span<byte> stackBuffer)
    {
        private readonly Span<byte> _stackBuffer = stackBuffer;
        private byte[]? _rented;

        public Span<byte> GetSpan(int length)
        {
            if (length <= _stackBuffer.Length)
            {
                return _stackBuffer[..length];
            }

            if (_rented is not null && _rented.Length < length)
            {
                ArrayPool<byte>.Shared.Return(_rented);
                _rented = null;
            }

            return (_rented ??= ArrayPool<byte>.Shared.Rent(length)).AsSpan(0, length);
        }

        public void Dispose()
        {
            if (_rented is not null)
            {
                ArrayPool<byte>.Shared.Return(_rented);
                _rented = null;
            }
        }
    }
}
