using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HotChocolate.Buffers;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class SourceResultDocument
{
    // The maximum container nesting of a source document, the default of Utf8JsonReader.
    private const int MaxSourceDepth = 64;

    // The number of bytes at the start of a string that are checked one at a time before the
    // vectorized search.
    private const int ScalarStringStartScanLength = 8;

    // The number of bytes after an escape sequence that are checked one at a time before the
    // vectorized search.
    private const int ScalarStringScanLength = 16;

    // The number of white space bytes checked one at a time before the vectorized search.
    private const int ScalarWhiteSpaceScanLength = 4;

    private static readonly SearchValues<byte> s_whiteSpaceBytes = SearchValues.Create(" \n\r\t"u8);

    // The bytes that stop a string scan: the closing quote, an escape and the control characters.
    private static readonly SearchValues<byte> s_stringStopBytes = SearchValues.Create(CreateStringStopBytes());

    /// <summary>
    /// Parses the JSON value in the first <paramref name="length"/> bytes of the gap-free data chunks
    /// <paramref name="segments"/> into <paramref name="metaDb"/>.
    /// </summary>
    private static void ParseJson(
        MemorySegment[] segments,
        int usedChunks,
        int length,
        ref MetaDb metaDb)
    {
        var first = segments[0];

        if (usedChunks == 1)
        {
            ParseJson(first.Buffer.AsSpan(first.Offset, length), ref metaDb, singleChunk: true);
            return;
        }

        var adjacentLength = first.Length;

        for (var i = 1; i < usedChunks; i++)
        {
            var segment = segments[i];

            if (!ReferenceEquals(segment.Buffer, first.Buffer) || segment.Offset != first.Offset + adjacentLength)
            {
                break;
            }

            adjacentLength += segment.Length;
        }

        if (adjacentLength >= length)
        {
            ParseJson(first.Buffer.AsSpan(first.Offset, length), ref metaDb, singleChunk: false);
            return;
        }

        // Token locations are linear offsets, so they are the same in a contiguous copy of the chunks.
        var buffer = ArrayPool<byte>.Shared.Rent(length);
        var written = 0;

        try
        {
            for (var i = 0; i < usedChunks; i++)
            {
                var segment = segments[i];
                var chunkLength = Math.Min(segment.Length, length - written);
                segment.Buffer.AsSpan(segment.Offset, chunkLength).CopyTo(buffer.AsSpan(written));
                written += chunkLength;
            }

            ParseJson(buffer.AsSpan(0, length), ref metaDb, singleChunk: false);
        }
        finally
        {
            buffer.AsSpan(0, written).Clear();
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Parses the JSON value in <paramref name="data"/> into <paramref name="metaDb"/>. The input is
    /// accepted and rejected like the <see cref="Utf8JsonReader"/> based parser accepts and rejects it.
    /// </summary>
    /// <param name="data">The contiguous UTF-8 JSON input.</param>
    /// <param name="metaDb">The database that receives one row per token.</param>
    /// <param name="singleChunk">
    /// <c>true</c> when the whole input lies in data chunk 0, so a linear offset is its packed location.
    /// </param>
    [SkipLocalsInit]
    private static void ParseJson(
        ReadOnlySpan<byte> data,
        ref MetaDb metaDb,
        bool singleChunk)
    {
        Span<Cursor> containerStart = stackalloc Cursor[MaxSourceDepth];
        Span<int> containerStartLocation = stackalloc int[MaxSourceDepth];
        Span<int> containerChildCount = stackalloc int[MaxSourceDepth];
        var containerIsArrayBits = 0UL;
        var containerHasComplexBits = 0UL;
        var stackIndex = 0;
        var locations = new DataLocationMap(singleChunk);
        int location;
        int end;
        bool escaped;
        bool isArray;

        var position = SkipWhiteSpace(data, 0);

        if (position == data.Length)
        {
            throw ThrowHelper.InvalidSourceJson(data, position);
        }

ReadValue:
        location = locations.Map(position);

        switch (data[position])
        {
            case (byte)'{':
            case (byte)'[':
                if (stackIndex == MaxSourceDepth)
                {
                    throw ThrowHelper.SourceJsonDepthExceeded(MaxSourceDepth, position);
                }

                // The new container is a complex child of its parent.
                if (stackIndex > 0)
                {
                    containerHasComplexBits |= 1UL << (stackIndex - 1);
                }

                isArray = data[position] == (byte)'[';
                containerStart[stackIndex] = metaDb.Reserve();
                containerStartLocation[stackIndex] = location;
                containerChildCount[stackIndex] = 0;
                containerHasComplexBits &= ~(1UL << stackIndex);
                containerIsArrayBits = isArray
                    ? containerIsArrayBits | (1UL << stackIndex)
                    : containerIsArrayBits & ~(1UL << stackIndex);
                stackIndex++;

                position = SkipWhiteSpace(data, position + 1);

                if (position == data.Length)
                {
                    throw ThrowHelper.InvalidSourceJson(data, position);
                }

                if (data[position] == (isArray ? (byte)']' : (byte)'}'))
                {
                    goto CloseContainer;
                }

                if (isArray)
                {
                    containerChildCount[stackIndex - 1]++;
                    goto ReadValue;
                }

                goto ReadPropertyName;

            case (byte)'"':
                // Strings are stored quote-inclusive, see AppendStringToken.
                end = ReadString(data, position, out escaped);
                metaDb.Append(JsonTokenType.String, location, end - position, hasComplexChildren: escaped);
                position = end;
                break;

            case (byte)'t':
                position = ReadLiteral(data, position, "true"u8);
                metaDb.Append(JsonTokenType.True, location, 4);
                break;

            case (byte)'f':
                position = ReadLiteral(data, position, "false"u8);
                metaDb.Append(JsonTokenType.False, location, 5);
                break;

            case (byte)'n':
                position = ReadLiteral(data, position, "null"u8);
                metaDb.Append(JsonTokenType.Null, location, 4);
                break;

            default:
                end = ReadNumber(data, position);
                AppendNumberToken(ref metaDb, location, end - position);
                position = end;
                break;
        }

AfterValue:
        position = SkipWhiteSpace(data, position);

        if (stackIndex == 0)
        {
            // A root array or a root scalar must be the only value of the input.
            if (position != data.Length)
            {
                throw ThrowHelper.InvalidSourceJson(data, position);
            }

            return;
        }

        if (position == data.Length)
        {
            throw ThrowHelper.InvalidSourceJson(data, position);
        }

        isArray = (containerIsArrayBits & (1UL << (stackIndex - 1))) != 0;

        if (data[position] == (byte)',')
        {
            position = SkipWhiteSpace(data, position + 1);

            if (position == data.Length)
            {
                throw ThrowHelper.InvalidSourceJson(data, position);
            }

            if (isArray)
            {
                containerChildCount[stackIndex - 1]++;
                goto ReadValue;
            }

            goto ReadPropertyName;
        }

        if (data[position] != (isArray ? (byte)']' : (byte)'}'))
        {
            throw ThrowHelper.InvalidSourceJson(data, position);
        }

CloseContainer:
        stackIndex--;
        isArray = (containerIsArrayBits & (1UL << stackIndex)) != 0;
        CloseContainer(
            ref metaDb,
            isArray ? JsonTokenType.StartArray : JsonTokenType.StartObject,
            isArray ? JsonTokenType.EndArray : JsonTokenType.EndObject,
            containerStart[stackIndex],
            containerStartLocation[stackIndex],
            locations.Map(position),
            containerChildCount[stackIndex],
            (containerHasComplexBits & (1UL << stackIndex)) != 0);
        position++;

        // A root object ends the document, the input after it is not read.
        if (stackIndex == 0 && !isArray)
        {
            return;
        }

        goto AfterValue;

ReadPropertyName:
        if (data[position] != (byte)'"')
        {
            throw ThrowHelper.InvalidSourceJson(data, position);
        }

        end = ReadString(data, position, out escaped);
        metaDb.Append(
            JsonTokenType.PropertyName,
            locations.Map(position),
            end - position,
            hasComplexChildren: escaped);
        containerChildCount[stackIndex - 1]++;
        position = SkipWhiteSpace(data, end);

        if (position == data.Length || data[position] != (byte)':')
        {
            throw ThrowHelper.InvalidSourceJson(data, position);
        }

        position = SkipWhiteSpace(data, position + 1);

        if (position == data.Length)
        {
            throw ThrowHelper.InvalidSourceJson(data, position);
        }

        goto ReadValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int SkipWhiteSpace(ReadOnlySpan<byte> data, int position)
    {
        if ((uint)position < (uint)data.Length && data[position] > (byte)' ')
        {
            return position;
        }

        return SkipWhiteSpaceRun(data, position);
    }

    private static int SkipWhiteSpaceRun(ReadOnlySpan<byte> data, int position)
    {
        var scalarEnd = Math.Min(data.Length, position + ScalarWhiteSpaceScanLength);

        while (position < scalarEnd)
        {
            if (!IsWhiteSpace(data[position]))
            {
                return position;
            }

            position++;
        }

        var index = data[position..].IndexOfAnyExcept(s_whiteSpaceBytes);
        return index == -1 ? data.Length : position + index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsWhiteSpace(byte value)
        => value is (byte)' ' or (byte)'\n' or (byte)'\r' or (byte)'\t';

    /// <summary>
    /// Reads the string that starts with the quote at <paramref name="start"/> and returns the
    /// position after its closing quote.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ReadString(ReadOnlySpan<byte> data, int start, out bool escaped)
    {
        var position = start + 1;
        var scalarEnd = Math.Min(data.Length, position + ScalarStringStartScanLength);

        while (position < scalarEnd)
        {
            var current = data[position];

            if (current == (byte)'"')
            {
                escaped = false;
                return position + 1;
            }

            if (current == (byte)'\\' || current < (byte)' ')
            {
                break;
            }

            position++;
        }

        return ReadStringRemainder(data, position, out escaped);
    }

    /// <summary>
    /// Reads the rest of a string from <paramref name="position"/> and returns the position after its
    /// closing quote.
    /// </summary>
    private static int ReadStringRemainder(ReadOnlySpan<byte> data, int position, out bool escaped)
    {
        escaped = false;

        while (true)
        {
            var index = data[position..].IndexOfAny(s_stringStopBytes);

            if (index == -1)
            {
                throw ThrowHelper.InvalidSourceJson(data, data.Length);
            }

            position += index;
            var stop = data[position];

            if (stop == (byte)'"')
            {
                return position + 1;
            }

            if (stop != (byte)'\\')
            {
                throw ThrowHelper.InvalidSourceJson(data, position);
            }

            escaped = true;
            position = ReadEscapeSequence(data, position);
            var scalarEnd = Math.Min(data.Length, position + ScalarStringScanLength);

            while (position < scalarEnd)
            {
                var current = data[position];

                if (current == (byte)'"')
                {
                    return position + 1;
                }

                if (current == (byte)'\\')
                {
                    position = ReadEscapeSequence(data, position);
                    scalarEnd = Math.Min(data.Length, position + ScalarStringScanLength);
                    continue;
                }

                if (current < (byte)' ')
                {
                    throw ThrowHelper.InvalidSourceJson(data, position);
                }

                position++;
            }
        }
    }

    /// <summary>
    /// Validates the escape sequence that starts with the backslash at <paramref name="start"/> and
    /// returns the position after it.
    /// </summary>
    private static int ReadEscapeSequence(ReadOnlySpan<byte> data, int start)
    {
        var position = start + 1;

        if (position == data.Length)
        {
            throw ThrowHelper.InvalidSourceJson(data, position);
        }

        switch (data[position])
        {
            case (byte)'"':
            case (byte)'\\':
            case (byte)'/':
            case (byte)'b':
            case (byte)'f':
            case (byte)'n':
            case (byte)'r':
            case (byte)'t':
                return position + 1;

            case (byte)'u':
                for (var i = 1; i <= 4; i++)
                {
                    if (position + i == data.Length || !char.IsAsciiHexDigit((char)data[position + i]))
                    {
                        throw ThrowHelper.InvalidSourceJson(data, position + i);
                    }
                }

                return position + 5;

            default:
                throw ThrowHelper.InvalidSourceJson(data, position);
        }
    }

    private static int ReadLiteral(ReadOnlySpan<byte> data, int start, ReadOnlySpan<byte> literal)
    {
        var remaining = data[start..];

        if (!remaining.StartsWith(literal))
        {
            var matched = remaining.CommonPrefixLength(literal);
            throw ThrowHelper.InvalidSourceJson(data, start + matched);
        }

        return start + literal.Length;
    }

    /// <summary>
    /// Reads the number that starts at <paramref name="start"/> and returns the position after it.
    /// </summary>
    private static int ReadNumber(ReadOnlySpan<byte> data, int start)
    {
        var position = start;

        if (data[position] == (byte)'-')
        {
            position++;

            if (position == data.Length)
            {
                throw ThrowHelper.InvalidSourceJson(data, position);
            }
        }

        if (data[position] == (byte)'0')
        {
            position++;
        }
        else if (IsNonZeroDigit(data[position]))
        {
            position = SkipDigits(data, position + 1);
        }
        else
        {
            throw ThrowHelper.InvalidSourceJson(data, position);
        }

        if (position < data.Length && data[position] == (byte)'.')
        {
            position = ReadRequiredDigits(data, position + 1);
        }

        if (position < data.Length && (data[position] | 0x20) == (byte)'e')
        {
            position++;

            if (position < data.Length && data[position] is (byte)'+' or (byte)'-')
            {
                position++;
            }

            position = ReadRequiredDigits(data, position);
        }

        if (position < data.Length && !IsNumberDelimiter(data[position]))
        {
            throw ThrowHelper.InvalidSourceJson(data, position);
        }

        return position;
    }

    private static int ReadRequiredDigits(ReadOnlySpan<byte> data, int position)
    {
        if (position == data.Length || !char.IsAsciiDigit((char)data[position]))
        {
            throw ThrowHelper.InvalidSourceJson(data, position);
        }

        return SkipDigits(data, position + 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int SkipDigits(ReadOnlySpan<byte> data, int position)
    {
        while ((uint)position < (uint)data.Length && char.IsAsciiDigit((char)data[position]))
        {
            position++;
        }

        return position;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsNonZeroDigit(byte value) => (uint)(value - '1') <= '9' - '1';

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsNumberDelimiter(byte value)
        => value is (byte)',' or (byte)'}' or (byte)']' or (byte)' ' or (byte)'\n' or (byte)'\r' or (byte)'\t';

    private static byte[] CreateStringStopBytes()
    {
        var bytes = new byte[0x22];

        for (var i = 0; i < 0x20; i++)
        {
            bytes[i] = (byte)i;
        }

        bytes[0x20] = (byte)'"';
        bytes[0x21] = (byte)'\\';
        return bytes;
    }

    /// <summary>
    /// Maps the positions of the tokens of one input, in increasing order, to packed data locations.
    /// </summary>
    private struct DataLocationMap(bool singleChunk)
    {
        private int _chunkIndex;
        private long _chunkStart;
        private long _chunkEnd = GetDataChunkSize(0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Map(int position)
        {
            if (singleChunk)
            {
                return position;
            }

            while (position >= _chunkEnd)
            {
                _chunkStart = _chunkEnd;
                _chunkEnd += GetDataChunkSize(++_chunkIndex);
            }

            return EncodeDataLocation(_chunkIndex, (int)(position - _chunkStart));
        }
    }
}
