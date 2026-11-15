using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using HotChocolate.Text.Json;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class SourceResultDocument
{
    internal ref struct RawJsonFormatter(SourceResultDocument document, JsonWriter writer)
    {
        public void WriteValue(Cursor cursor)
        {
            var row = document._parsedData.Get(cursor);
            WriteValue(cursor, row);
        }

        private void WriteValue(Cursor cursor, DbRow row)
        {
            var tokenType = row.TokenType;

            Debug.Assert(tokenType is not JsonTokenType.EndObject);
            Debug.Assert(tokenType is not JsonTokenType.EndArray);

            switch (tokenType)
            {
                case JsonTokenType.StartObject:
                    WriteObject(cursor, row);
                    break;

                case JsonTokenType.StartArray:
                    WriteArray(cursor, row);
                    break;

                case JsonTokenType.None:
                case JsonTokenType.Null:
                    writer.WriteNullValue();
                    break;

                case JsonTokenType.True:
                    writer.WriteBooleanValue(true);
                    break;

                case JsonTokenType.False:
                    writer.WriteBooleanValue(false);
                    break;

                case JsonTokenType.String:
                    document.WriteRawStringValueTo(writer, row.Location, row.SizeOrLength);
                    break;

                case JsonTokenType.Number:
                    document.WriteRawNumberValueTo(writer, row.Location, row.SizeOrLength);
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        private void WriteObject(Cursor start, DbRow startRow)
        {
            Debug.Assert(startRow.TokenType is JsonTokenType.StartObject);

            var current = start + 1;
            var end = start + startRow.NumberOfRows - 1;

            writer.WriteStartObject();

            while (current < end)
            {
                var nameRow = document._parsedData.Get(current);
                Debug.Assert(nameRow.TokenType is JsonTokenType.PropertyName);

                // property name
                writer.WritePropertyNameUnescaped(document.ReadRawValue(nameRow, includeQuotes: false));

                // property value
                var valueCursor = current + 1;
                var valueRow = document._parsedData.Get(valueCursor);
                current = valueCursor;
                WriteValue(current, valueRow);

                // next property (move past value)
                if (valueRow.IsSimpleValue)
                {
                    current++;
                }
                else
                {
                    current += valueRow.NumberOfRows;
                }
            }

            writer.WriteEndObject();
        }

        private void WriteArray(Cursor start, DbRow startRow)
        {
            Debug.Assert(startRow.TokenType is JsonTokenType.StartArray);

            var current = start + 1;
            var end = start + startRow.NumberOfRows - 1;

            writer.WriteStartArray();

            while (current < end)
            {
                var row = document._parsedData.Get(current);
                WriteValue(current, row);

                if (row.IsSimpleValue)
                {
                    current++;
                }
                else
                {
                    current += row.NumberOfRows;
                }
            }

            writer.WriteEndArray();
        }
    }

    /// <summary>
    /// Writes the raw string value at <paramref name="location"/> into <paramref name="emitter"/>.
    /// </summary>
    internal void WriteRawStringValueTo(ref MinimizedJsonEmitter emitter, int location, int size)
    {
        if (TryGetSingleChunkValue(location, size, out var buffer))
        {
            emitter.WriteStringValue(buffer, size);
            return;
        }

        WriteCrossChunkValueTo(ref emitter, location, size);
    }

    /// <summary>
    /// Writes the raw number value at <paramref name="location"/> into <paramref name="emitter"/>.
    /// </summary>
    internal void WriteRawNumberValueTo(ref MinimizedJsonEmitter emitter, int location, int size)
    {
        if (TryGetSingleChunkValue(location, size, out var buffer))
        {
            emitter.WriteRawValue(buffer, size);
            return;
        }

        WriteCrossChunkValueTo(ref emitter, location, size);
    }

    /// <summary>
    /// Writes the value at <paramref name="cursor"/> into <paramref name="emitter"/>.
    /// </summary>
    internal void WriteValueTo(ref MinimizedJsonEmitter emitter, Cursor cursor)
        => WriteValueTo(ref emitter, cursor, _parsedData.Get(cursor));

    /// <summary>
    /// Gets the data from <paramref name="location"/> to the end of its chunk when the
    /// value of <paramref name="size"/> bytes at that location lies within a single chunk.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetSingleChunkValue(int location, int size, out ReadOnlySpan<byte> buffer)
    {
        if (size > JsonConstants.MaxUnescapedTokenSize)
        {
            ThrowHelper.ThrowArgumentException_ValueTooLarge(size);
        }

        var startChunkIndex = location >>> DataOffsetBits;
        var offsetInStartChunk = location & DataOffsetMask;

        Debug.Assert((uint)startChunkIndex < (uint)_segments.Length);
        ref readonly var startSeg = ref Unsafe.Add(
            ref MemoryMarshal.GetArrayDataReference(_segments),
            startChunkIndex);

        var available = startSeg.Length - offsetInStartChunk;

        if (size <= available)
        {
            ref var start = ref Unsafe.Add(
                ref MemoryMarshal.GetArrayDataReference(startSeg.Buffer),
                startSeg.Offset + offsetInStartChunk);
            buffer = MemoryMarshal.CreateReadOnlySpan(ref start, available);
            return true;
        }

        buffer = default;
        return false;
    }

    private void WriteCrossChunkValueTo(ref MinimizedJsonEmitter emitter, int location, int size)
    {
        var bytesRead = 0;
        var chunkIndex = location >>> DataOffsetBits;
        var offsetInChunk = location & DataOffsetMask;

        emitter.WriteRawValueStart();

        while (bytesRead < size)
        {
            var seg = _segments[chunkIndex];
            var bytesToWrite = Math.Min(size - bytesRead, seg.Length - offsetInChunk);
            emitter.WriteRawValuePart(seg.Buffer.AsSpan(seg.Offset + offsetInChunk, bytesToWrite));
            bytesRead += bytesToWrite;
            chunkIndex++;
            offsetInChunk = 0;
        }

        emitter.WriteRawValueEnd();
    }

    private void WriteValueTo(ref MinimizedJsonEmitter emitter, Cursor cursor, DbRow row)
    {
        var tokenType = row.TokenType;

        Debug.Assert(tokenType is not JsonTokenType.EndObject);
        Debug.Assert(tokenType is not JsonTokenType.EndArray);

        switch (tokenType)
        {
            case JsonTokenType.StartObject:
                WriteObjectTo(ref emitter, cursor, row);
                break;

            case JsonTokenType.StartArray:
                WriteArrayTo(ref emitter, cursor, row);
                break;

            case JsonTokenType.None:
            case JsonTokenType.Null:
                emitter.WriteRawValue(JsonConstants.NullValue);
                break;

            case JsonTokenType.True:
                emitter.WriteRawValue(JsonConstants.TrueValue);
                break;

            case JsonTokenType.False:
                emitter.WriteRawValue(JsonConstants.FalseValue);
                break;

            case JsonTokenType.String:
                WriteRawStringValueTo(ref emitter, row.Location, row.SizeOrLength);
                break;

            case JsonTokenType.Number:
                WriteRawNumberValueTo(ref emitter, row.Location, row.SizeOrLength);
                break;

            default:
                throw new NotSupportedException();
        }
    }

    private void WriteObjectTo(ref MinimizedJsonEmitter emitter, Cursor start, DbRow startRow)
    {
        Debug.Assert(startRow.TokenType is JsonTokenType.StartObject);

        var current = start + 1;
        var end = start + startRow.NumberOfRows - 1;

        emitter.WriteStartObject();

        while (current < end)
        {
            var nameRow = _parsedData.Get(current);
            Debug.Assert(nameRow.TokenType is JsonTokenType.PropertyName);

            emitter.WriteQuotedPropertyName(ReadRawValue(nameRow, includeQuotes: true));

            var valueCursor = current + 1;
            var valueRow = _parsedData.Get(valueCursor);
            current = valueCursor;
            WriteValueTo(ref emitter, current, valueRow);

            if (valueRow.IsSimpleValue)
            {
                current++;
            }
            else
            {
                current += valueRow.NumberOfRows;
            }
        }

        emitter.WriteEndObject();
    }

    private void WriteArrayTo(ref MinimizedJsonEmitter emitter, Cursor start, DbRow startRow)
    {
        Debug.Assert(startRow.TokenType is JsonTokenType.StartArray);

        var current = start + 1;
        var end = start + startRow.NumberOfRows - 1;

        emitter.WriteStartArray();

        while (current < end)
        {
            var row = _parsedData.Get(current);
            WriteValueTo(ref emitter, current, row);

            if (row.IsSimpleValue)
            {
                current++;
            }
            else
            {
                current += row.NumberOfRows;
            }
        }

        emitter.WriteEndArray();
    }
}
