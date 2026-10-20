using System.Buffers;
using System.Diagnostics;
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

    // The result of a string read that reaches the end of the window before the closing quote.
    private const int IncompleteString = -1;

    // The minimum number of bytes a copied window reads past the end of the window before it.
    private const int MinStitchExtension = 256;

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
            ParseJson<SingleWindow>(first.Buffer.AsSpan(first.Offset, length), null, length, singleChunk: true, ref metaDb);
            return;
        }

        var runLength = Math.Min(GetRunEnd(segments, length, chunkIndex: 0, chunkStart: 0), length);
        var data = first.Buffer.AsSpan(first.Offset, runLength);

        if (runLength == length)
        {
            ParseJson<SingleWindow>(data, null, length, singleChunk: false, ref metaDb);
            return;
        }

        ParseJson<MultipleWindows>(data, segments, length, singleChunk: false, ref metaDb);
    }

    /// <summary>
    /// Parses the JSON value of the input into <paramref name="metaDb"/>. The input is accepted and
    /// rejected like the <see cref="Utf8JsonReader"/> based parser accepts and rejects it.
    /// </summary>
    /// <param name="input">The first bytes of the UTF-8 JSON input.</param>
    /// <param name="segments">
    /// The gap-free data chunks that hold the input, or <c>null</c> when <paramref name="input"/> is
    /// the whole input.
    /// </param>
    /// <param name="length">The length of the input in bytes.</param>
    /// <param name="singleChunk">
    /// <c>true</c> when the whole input lies in data chunk 0, so a linear offset is its packed location.
    /// </param>
    /// <param name="metaDb">The database that receives one row per token.</param>
    /// <typeparam name="TMode">Whether the input is read in more than one window.</typeparam>
    [SkipLocalsInit]
    private static void ParseJson<TMode>(
        ReadOnlySpan<byte> input,
        MemorySegment[]? segments,
        int length,
        bool singleChunk,
        ref MetaDb metaDb)
        where TMode : struct, IWindowMode
    {
        Span<Container> containers = stackalloc Container[MaxSourceDepth];
        var containerIsArrayBits = 0UL;
        var containerHasComplexBits = 0UL;
        var stackIndex = 0;

        // The data chunk that the last mapped token position lies in, with its window positions.
        var locationChunk = 0;
        var locationChunkStart = 0;
        var locationChunkEnd = GetDataChunkSize(0);

        // The parser reads one window of the input at a time. offset is the position of the window
        // in the input, and a step that reaches the end of a window that is not the last one resumes
        // at resumeStep in the next window.
        scoped ReadOnlySpan<byte> data = input;
        var windows = new InputWindows(segments, length);
        var offset = 0;
        var isFinal = data.Length == length;
        var position = 0;
        ResumeStep resumeStep;
        int location;
        int end;
        bool escaped;
        bool isArray;

        // A string that continues in the next window: its position in the input, its location, whether
        // it is a property name and whether the part read so far has escape sequences.
        var stringStart = 0;
        var stringLocation = 0;
        var stringIsPropertyName = false;
        var stringEscaped = false;

SkipToValue:
        position = SkipWhiteSpace(data, position);

        if (position == data.Length)
        {
            if (!TMode.IsWindowed || isFinal)
            {
                throw ThrowHelper.InvalidSourceJson(data, position, offset);
            }

            resumeStep = ResumeStep.Value;
            goto NextWindow;
        }

ReadValue:
        location = MapLocation(position, singleChunk, ref locationChunk, ref locationChunkStart, ref locationChunkEnd);

        switch (data[position])
        {
            case (byte)'{':
            case (byte)'[':
                if (stackIndex == MaxSourceDepth)
                {
                    throw ThrowHelper.SourceJsonDepthExceeded(MaxSourceDepth, offset + position);
                }

                // The new container is a complex child of its parent.
                if (stackIndex > 0)
                {
                    containerHasComplexBits |= 1UL << (stackIndex - 1);
                }

                containers[stackIndex] = new Container(metaDb.Reserve(), location);
                containerHasComplexBits &= ~(1UL << stackIndex);
                containerIsArrayBits = data[position] == (byte)'['
                    ? containerIsArrayBits | (1UL << stackIndex)
                    : containerIsArrayBits & ~(1UL << stackIndex);
                stackIndex++;
                position++;
                goto ContainerStart;

            case (byte)'"':
                // Strings are stored quote-inclusive, see AppendStringToken.
                end = ReadString(data, position, offset);
                escaped = false;

                if (end < 0)
                {
                    if (end == IncompleteString)
                    {
                        if (!TMode.IsWindowed || isFinal)
                        {
                            throw ThrowHelper.InvalidSourceJson(data, data.Length, offset);
                        }

                        stringLocation = location;
                        stringIsPropertyName = false;
                        goto SuspendString;
                    }

                    end = ~end;
                    escaped = true;
                }

                metaDb.Append(JsonTokenType.String, location, end - position, hasComplexChildren: escaped);
                position = end;
                goto AfterValue;

            case (byte)'t':
                end = ReadLiteral(data, position, offset, "true"u8);

                if (end < 0)
                {
                    goto IncompleteValue;
                }

                metaDb.Append(JsonTokenType.True, location, 4);
                position = end;
                goto AfterValue;

            case (byte)'f':
                end = ReadLiteral(data, position, offset, "false"u8);

                if (end < 0)
                {
                    goto IncompleteValue;
                }

                metaDb.Append(JsonTokenType.False, location, 5);
                position = end;
                goto AfterValue;

            case (byte)'n':
                end = ReadLiteral(data, position, offset, "null"u8);

                if (end < 0)
                {
                    goto IncompleteValue;
                }

                metaDb.Append(JsonTokenType.Null, location, 4);
                position = end;
                goto AfterValue;

            default:
                end = ReadNumber(data, position, offset);

                if (end < 0)
                {
                    if (TMode.IsWindowed && !isFinal)
                    {
                        goto IncompleteValue;
                    }

                    end = ReadNumberAtEndOfInput(data, position, offset);
                }

                AppendNumberToken(ref metaDb, location, end - position);
                position = end;
                goto AfterValue;
        }

ContainerStart:
        position = SkipWhiteSpace(data, position);

        if (position == data.Length)
        {
            if (!TMode.IsWindowed || isFinal)
            {
                throw ThrowHelper.InvalidSourceJson(data, position, offset);
            }

            resumeStep = ResumeStep.ContainerStart;
            goto NextWindow;
        }

        isArray = (containerIsArrayBits & (1UL << (stackIndex - 1))) != 0;

        if (data[position] == (isArray ? (byte)']' : (byte)'}'))
        {
            goto EndContainer;
        }

        if (isArray)
        {
            containers[stackIndex - 1].ChildCount++;
            goto ReadValue;
        }

        goto ReadPropertyName;

AfterValue:
        position = SkipWhiteSpace(data, position);

        if (position == data.Length)
        {
            if (TMode.IsWindowed && !isFinal)
            {
                resumeStep = ResumeStep.AfterValue;
                goto NextWindow;
            }

            if (stackIndex == 0)
            {
                if (TMode.IsWindowed)
                {
                    windows.ReturnStitch();
                }

                return;
            }

            throw ThrowHelper.InvalidSourceJson(data, position, offset);
        }

        // A root array or a root scalar must be the only value of the input.
        if (stackIndex == 0)
        {
            throw ThrowHelper.InvalidSourceJson(data, position, offset);
        }

        if (data[position] == (byte)',')
        {
            position++;
            goto AfterComma;
        }

        isArray = (containerIsArrayBits & (1UL << (stackIndex - 1))) != 0;

        if (data[position] != (isArray ? (byte)']' : (byte)'}'))
        {
            throw ThrowHelper.InvalidSourceJson(data, position, offset);
        }

EndContainer:
        stackIndex--;
        isArray = (containerIsArrayBits & (1UL << stackIndex)) != 0;
        CloseContainer(
            ref metaDb,
            isArray ? JsonTokenType.StartArray : JsonTokenType.StartObject,
            isArray ? JsonTokenType.EndArray : JsonTokenType.EndObject,
            containers[stackIndex].Start,
            containers[stackIndex].StartLocation,
            MapLocation(position, singleChunk, ref locationChunk, ref locationChunkStart, ref locationChunkEnd),
            containers[stackIndex].ChildCount,
            (containerHasComplexBits & (1UL << stackIndex)) != 0);
        position++;

        // A root object ends the document, the input after it is not read.
        if (stackIndex == 0 && !isArray)
        {
            if (TMode.IsWindowed)
            {
                windows.ReturnStitch();
            }

            return;
        }

        goto AfterValue;

AfterComma:
        position = SkipWhiteSpace(data, position);

        if (position == data.Length)
        {
            if (!TMode.IsWindowed || isFinal)
            {
                throw ThrowHelper.InvalidSourceJson(data, position, offset);
            }

            resumeStep = ResumeStep.AfterComma;
            goto NextWindow;
        }

        if ((containerIsArrayBits & (1UL << (stackIndex - 1))) != 0)
        {
            containers[stackIndex - 1].ChildCount++;
            goto ReadValue;
        }

ReadPropertyName:
        if (data[position] != (byte)'"')
        {
            throw ThrowHelper.InvalidSourceJson(data, position, offset);
        }

        end = ReadString(data, position, offset);
        escaped = false;

        if (end < 0)
        {
            if (end == IncompleteString)
            {
                if (!TMode.IsWindowed || isFinal)
                {
                    throw ThrowHelper.InvalidSourceJson(data, data.Length, offset);
                }

                stringLocation = MapLocation(position, singleChunk, ref locationChunk, ref locationChunkStart, ref locationChunkEnd);
                stringIsPropertyName = true;
                goto SuspendString;
            }

            end = ~end;
            escaped = true;
        }

        metaDb.Append(
            JsonTokenType.PropertyName,
            MapLocation(position, singleChunk, ref locationChunk, ref locationChunkStart, ref locationChunkEnd),
            end - position,
            hasComplexChildren: escaped);
        containers[stackIndex - 1].ChildCount++;
        position = end;

AfterPropertyName:
        position = SkipWhiteSpace(data, position);

        if (position == data.Length)
        {
            if (!TMode.IsWindowed || isFinal)
            {
                throw ThrowHelper.InvalidSourceJson(data, position, offset);
            }

            resumeStep = ResumeStep.AfterPropertyName;
            goto NextWindow;
        }

        if (data[position] != (byte)':')
        {
            throw ThrowHelper.InvalidSourceJson(data, position, offset);
        }

        position++;
        goto SkipToValue;

// The string at position continues after the end of the window, where its scan resumes.
SuspendString:
        stringStart = offset + position;
        end = ScanIncompleteString(data, position + 1, offset);
        stringEscaped = end < 0;
        position = stringEscaped ? ~end : end;
        resumeStep = ResumeStep.StringRemainder;
        goto NextWindow;

ContinueString:
        end = ReadStringRemainder(data, position, offset);

        if (end == IncompleteString)
        {
            if (!TMode.IsWindowed || isFinal)
            {
                throw ThrowHelper.InvalidSourceJson(data, data.Length, offset);
            }

            end = ScanIncompleteString(data, position, offset);
            stringEscaped |= end < 0;
            position = end < 0 ? ~end : end;
            resumeStep = ResumeStep.StringRemainder;
            goto NextWindow;
        }

        if (end < 0)
        {
            end = ~end;
            stringEscaped = true;
        }

        metaDb.Append(
            stringIsPropertyName ? JsonTokenType.PropertyName : JsonTokenType.String,
            stringLocation,
            offset + end - stringStart,
            hasComplexChildren: stringEscaped);
        position = end;

        if (!stringIsPropertyName)
        {
            goto AfterValue;
        }

        containers[stackIndex - 1].ChildCount++;
        goto AfterPropertyName;

// The value at position continues after the end of the window.
IncompleteValue:
        if (!TMode.IsWindowed || isFinal)
        {
            throw ThrowHelper.InvalidSourceJson(data, data.Length, offset);
        }

        resumeStep = ResumeStep.Value;

NextWindow:
        data = windows.GetNextWindow(offset + position, offset + data.Length);
        locationChunkStart -= position;
        locationChunkEnd -= position;
        offset += position;
        isFinal = offset + data.Length == windows.Length;
        position = 0;

        switch (resumeStep)
        {
            case ResumeStep.Value:
                goto SkipToValue;

            case ResumeStep.ContainerStart:
                goto ContainerStart;

            case ResumeStep.AfterValue:
                goto AfterValue;

            case ResumeStep.AfterComma:
                goto AfterComma;

            case ResumeStep.StringRemainder:
                goto ContinueString;

            default:
                goto AfterPropertyName;
        }
    }

    /// <summary>
    /// Gets the end position of the adjacent chunks that start with the chunk at
    /// <paramref name="chunkIndex"/>, which starts at <paramref name="chunkStart"/>.
    /// </summary>
    private static int GetRunEnd(MemorySegment[] segments, int length, int chunkIndex, int chunkStart)
    {
        var first = segments[chunkIndex];
        var runEnd = chunkStart + first.Length;

        for (var i = chunkIndex + 1; runEnd < length; i++)
        {
            var segment = segments[i];

            if (!ReferenceEquals(segment.Buffer, first.Buffer)
                || segment.Offset != first.Offset + (runEnd - chunkStart))
            {
                break;
            }

            runEnd += segment.Length;
        }

        return runEnd;
    }

    /// <summary>
    /// Maps the window <paramref name="position"/> of a token to its packed data location. Positions
    /// are mapped in increasing order, starting with the data chunk at <paramref name="chunk"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int MapLocation(int position, bool singleChunk, ref int chunk, ref int chunkStart, ref int chunkEnd)
    {
        if (singleChunk)
        {
            return position;
        }

        while (position >= chunkEnd)
        {
            chunkStart = chunkEnd;
            chunkEnd += GetDataChunkSize(++chunk);
        }

        return EncodeDataLocation(chunk, position - chunkStart);
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
    /// position after its closing quote. The bitwise complement of that position is returned for a
    /// string with escape sequences, and <see cref="IncompleteString"/> when <paramref name="data"/>
    /// ends before the closing quote.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ReadString(ReadOnlySpan<byte> data, int start, int offset)
    {
        var position = start + 1;
        var scalarEnd = Math.Min(data.Length, position + ScalarStringStartScanLength);

        while (position < scalarEnd)
        {
            var current = data[position];

            if (current == (byte)'"')
            {
                return position + 1;
            }

            if (current == (byte)'\\' || current < (byte)' ')
            {
                break;
            }

            position++;
        }

        return ReadStringRemainder(data, position, offset);
    }

    /// <summary>
    /// Reads the rest of a string from <paramref name="position"/>, with the result of
    /// <see cref="ReadString"/>.
    /// </summary>
    private static int ReadStringRemainder(ReadOnlySpan<byte> data, int position, int offset)
    {
        var escaped = false;

        while (true)
        {
            var index = data[position..].IndexOfAny(s_stringStopBytes);

            if (index == -1)
            {
                return IncompleteString;
            }

            position += index;
            var stop = data[position];

            if (stop == (byte)'"')
            {
                return escaped ? ~(position + 1) : position + 1;
            }

            if (stop != (byte)'\\')
            {
                throw ThrowHelper.InvalidSourceJson(data, position, offset);
            }

            escaped = true;
            position = ReadEscapeSequence(data, position, offset);

            if (position < 0)
            {
                return IncompleteString;
            }

            var scalarEnd = Math.Min(data.Length, position + ScalarStringScanLength);

            while (position < scalarEnd)
            {
                var current = data[position];

                if (current == (byte)'"')
                {
                    return ~(position + 1);
                }

                if (current == (byte)'\\')
                {
                    position = ReadEscapeSequence(data, position, offset);

                    if (position < 0)
                    {
                        return IncompleteString;
                    }

                    scalarEnd = Math.Min(data.Length, position + ScalarStringScanLength);
                    continue;
                }

                if (current < (byte)' ')
                {
                    throw ThrowHelper.InvalidSourceJson(data, position, offset);
                }

                position++;
            }
        }
    }

    /// <summary>
    /// Gets the position at which the read of a string that continues after the end of
    /// <paramref name="data"/> resumes, from <paramref name="position"/> in the string. The bitwise
    /// complement of the position is returned when the bytes from <paramref name="position"/> have
    /// escape sequences.
    /// </summary>
    private static int ScanIncompleteString(ReadOnlySpan<byte> data, int position, int offset)
    {
        var escaped = false;

        while (true)
        {
            var index = data[position..].IndexOfAny(s_stringStopBytes);

            if (index == -1)
            {
                position = data.Length;
                break;
            }

            // The string is incomplete, so the scan only stops at escape sequences.
            position += index;
            Debug.Assert(data[position] == (byte)'\\', "An incomplete string only stops at escapes.");
            escaped = true;
            var next = ReadEscapeSequence(data, position, offset);

            // An escape sequence that continues after the end of the data is read again.
            if (next < 0)
            {
                break;
            }

            position = next;
        }

        return escaped ? ~position : position;
    }

    /// <summary>
    /// Validates the escape sequence that starts with the backslash at <paramref name="start"/> and
    /// returns the position after it, or -1 when <paramref name="data"/> ends before it.
    /// </summary>
    private static int ReadEscapeSequence(ReadOnlySpan<byte> data, int start, int offset)
    {
        var position = start + 1;

        if (position == data.Length)
        {
            return -1;
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
                    if (position + i == data.Length)
                    {
                        return -1;
                    }

                    if (!char.IsAsciiHexDigit((char)data[position + i]))
                    {
                        throw ThrowHelper.InvalidSourceJson(data, position + i, offset);
                    }
                }

                return position + 5;

            default:
                throw ThrowHelper.InvalidSourceJson(data, position, offset);
        }
    }

    /// <summary>
    /// Reads the literal that starts at <paramref name="start"/> and returns the position after it,
    /// or -1 when <paramref name="data"/> ends before it.
    /// </summary>
    private static int ReadLiteral(ReadOnlySpan<byte> data, int start, int offset, ReadOnlySpan<byte> literal)
    {
        var remaining = data[start..];

        if (!remaining.StartsWith(literal))
        {
            var matched = remaining.CommonPrefixLength(literal);

            if (matched == remaining.Length)
            {
                return -1;
            }

            throw ThrowHelper.InvalidSourceJson(data, start + matched, offset);
        }

        return start + literal.Length;
    }

    /// <summary>
    /// Reads the number that starts at <paramref name="start"/> and returns the position after it, or
    /// -1 when the number reaches the end of <paramref name="data"/>.
    /// </summary>
    private static int ReadNumber(ReadOnlySpan<byte> data, int start, int offset)
    {
        var position = start;

        if (data[position] == (byte)'-')
        {
            position++;

            if (position == data.Length)
            {
                return -1;
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
            throw ThrowHelper.InvalidSourceJson(data, position, offset);
        }

        if (position == data.Length)
        {
            return -1;
        }

        if (data[position] == (byte)'.')
        {
            position++;

            if (position == data.Length)
            {
                return -1;
            }

            if (!char.IsAsciiDigit((char)data[position]))
            {
                throw ThrowHelper.InvalidSourceJson(data, position, offset);
            }

            position = SkipDigits(data, position + 1);

            if (position == data.Length)
            {
                return -1;
            }
        }

        if ((data[position] | 0x20) == (byte)'e')
        {
            position++;

            if (position < data.Length && data[position] is (byte)'+' or (byte)'-')
            {
                position++;
            }

            if (position == data.Length)
            {
                return -1;
            }

            if (!char.IsAsciiDigit((char)data[position]))
            {
                throw ThrowHelper.InvalidSourceJson(data, position, offset);
            }

            position = SkipDigits(data, position + 1);

            if (position == data.Length)
            {
                return -1;
            }
        }

        if (!IsNumberDelimiter(data[position]))
        {
            throw ThrowHelper.InvalidSourceJson(data, position, offset);
        }

        return position;
    }

    /// <summary>
    /// Reads the number that starts at <paramref name="start"/> and ends with the input, and returns
    /// the end of <paramref name="data"/>.
    /// </summary>
    private static int ReadNumberAtEndOfInput(ReadOnlySpan<byte> data, int start, int offset)
    {
        // The number is read again followed by a delimiter, which ends a complete number.
        var number = data[start..];
        var buffer = ArrayPool<byte>.Shared.Rent(number.Length + 1);

        try
        {
            number.CopyTo(buffer);
            buffer[number.Length] = (byte)' ';

            if (ReadNumber(buffer.AsSpan(0, number.Length + 1), 0, offset + start) != number.Length)
            {
                throw ThrowHelper.InvalidSourceJson(data, data.Length, offset);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return data.Length;
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
    /// The step of the parser that continues in the next window.
    /// </summary>
    private enum ResumeStep
    {
        Value,
        ContainerStart,
        AfterValue,
        AfterComma,
        StringRemainder,
        AfterPropertyName
    }

    /// <summary>
    /// Whether the parser reads its input in more than one window.
    /// </summary>
    private interface IWindowMode
    {
        static abstract bool IsWindowed { get; }
    }

    private readonly struct SingleWindow : IWindowMode
    {
        public static bool IsWindowed => false;
    }

    private readonly struct MultipleWindows : IWindowMode
    {
        public static bool IsWindowed => true;
    }

    /// <summary>
    /// An open container of the parser.
    /// </summary>
    private struct Container(Cursor start, int startLocation)
    {
        public readonly Cursor Start = start;

        public readonly int StartLocation = startLocation;

        public int ChildCount;
    }

    /// <summary>
    /// The windows of an input that lies in gap-free data chunks.
    /// </summary>
    private struct InputWindows(MemorySegment[]? segments, int length)
    {
        private byte[]? _stitch;

        /// <summary>
        /// The length of the input in bytes.
        /// </summary>
        public readonly int Length = length;

        /// <summary>
        /// Gets the window of the input that starts at <paramref name="start"/> and ends after
        /// <paramref name="previousEnd"/>, the end of the window before it.
        /// </summary>
        public ReadOnlySpan<byte> GetNextWindow(int start, int previousEnd)
        {
            var chunks = segments!;
            var chunkIndex = 0;
            var chunkStart = 0;

            while (chunkStart + chunks[chunkIndex].Length <= start)
            {
                chunkStart += chunks[chunkIndex].Length;
                chunkIndex++;
            }

            var runEnd = Math.Min(GetRunEnd(chunks, Length, chunkIndex, chunkStart), Length);
            var segment = chunks[chunkIndex];

            if (runEnd > previousEnd)
            {
                return segment.Buffer.AsSpan(segment.Offset + (start - chunkStart), runEnd - start);
            }

            // The window would end where the window before it ended, so it is read from a copy that
            // extends into the chunks after it.
            var stitchEnd = (int)Math.Min(Length, previousEnd + Math.Max(MinStitchExtension, (long)previousEnd - start));
            var stitchLength = stitchEnd - start;

            if (_stitch is null || _stitch.Length < stitchLength)
            {
                ReturnStitch();
                _stitch = ArrayPool<byte>.Shared.Rent(stitchLength);
            }

            var written = 0;

            while (written < stitchLength)
            {
                segment = chunks[chunkIndex];
                var chunkOffset = start + written - chunkStart;
                var count = Math.Min(segment.Length - chunkOffset, stitchLength - written);
                segment.Buffer.AsSpan(segment.Offset + chunkOffset, count).CopyTo(_stitch.AsSpan(written));
                written += count;
                chunkStart += segment.Length;
                chunkIndex++;
            }

            return _stitch.AsSpan(0, stitchLength);
        }

        /// <summary>
        /// Clears the copy buffer and returns it to the pool.
        /// </summary>
        public void ReturnStitch()
        {
            if (_stitch is not null)
            {
                _stitch.AsSpan().Clear();
                ArrayPool<byte>.Shared.Return(_stitch);
                _stitch = null;
            }
        }
    }
}
