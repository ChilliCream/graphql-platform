using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Unicode;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class SourceResultDocument
{
    internal bool TryGetValue(int index, out sbyte value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _parsedData.Get(index);

        CheckExpectedType(JsonTokenType.Number, row.TokenType);

        var segment = ReadRawValue(row);

        if (Utf8Parser.TryParse(segment, out sbyte tmp, out var consumed)
            && consumed == segment.Length)
        {
            value = tmp;
            return true;
        }

        value = 0;
        return false;
    }

    internal bool TryGetValue(int index, out byte value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _parsedData.Get(index);

        CheckExpectedType(JsonTokenType.Number, row.TokenType);

        var segment = ReadRawValue(row);

        if (Utf8Parser.TryParse(segment, out byte tmp, out var consumed)
            && consumed == segment.Length)
        {
            value = tmp;
            return true;
        }

        value = 0;
        return false;
    }

    internal bool TryGetValue(int index, out short value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _parsedData.Get(index);

        CheckExpectedType(JsonTokenType.Number, row.TokenType);

        var segment = ReadRawValue(row);

        if (Utf8Parser.TryParse(segment, out short tmp, out var consumed)
            && consumed == segment.Length)
        {
            value = tmp;
            return true;
        }

        value = 0;
        return false;
    }

    internal bool TryGetValue(int index, out ushort value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _parsedData.Get(index);

        CheckExpectedType(JsonTokenType.Number, row.TokenType);

        var segment = ReadRawValue(row);

        if (Utf8Parser.TryParse(segment, out ushort tmp, out var consumed)
            && consumed == segment.Length)
        {
            value = tmp;
            return true;
        }

        value = 0;
        return false;
    }

    internal bool TryGetValue(int index, out int value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _parsedData.Get(index);

        CheckExpectedType(JsonTokenType.Number, row.TokenType);

        var segment = ReadRawValue(row);

        if (Utf8Parser.TryParse(segment, out int tmp, out var consumed)
            && consumed == segment.Length)
        {
            value = tmp;
            return true;
        }

        value = 0;
        return false;
    }

    internal bool TryGetValue(int index, out uint value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _parsedData.Get(index);

        CheckExpectedType(JsonTokenType.Number, row.TokenType);

        var segment = ReadRawValue(row);

        if (Utf8Parser.TryParse(segment, out uint tmp, out var consumed)
            && consumed == segment.Length)
        {
            value = tmp;
            return true;
        }

        value = 0;
        return false;
    }

    internal bool TryGetValue(int index, out long value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _parsedData.Get(index);

        CheckExpectedType(JsonTokenType.Number, row.TokenType);

        var segment = ReadRawValue(row);

        if (Utf8Parser.TryParse(segment, out long tmp, out var consumed)
            && consumed == segment.Length)
        {
            value = tmp;
            return true;
        }

        value = 0;
        return false;
    }

    internal bool TryGetValue(int index, out ulong value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _parsedData.Get(index);

        CheckExpectedType(JsonTokenType.Number, row.TokenType);

        var segment = ReadRawValue(row);

        if (Utf8Parser.TryParse(segment, out ulong tmp, out var consumed)
            && consumed == segment.Length)
        {
            value = tmp;
            return true;
        }

        value = 0;
        return false;
    }

    internal bool TryGetValue(int index, out double value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _parsedData.Get(index);

        CheckExpectedType(JsonTokenType.Number, row.TokenType);

        var segment = ReadRawValue(row);

        if (Utf8Parser.TryParse(segment, out double tmp, out var bytesConsumed)
            && segment.Length == bytesConsumed)
        {
            value = tmp;
            return true;
        }

        value = 0;
        return false;
    }

    internal bool TryGetValue(int index, out float value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _parsedData.Get(index);

        CheckExpectedType(JsonTokenType.Number, row.TokenType);

        var segment = ReadRawValue(row);

        if (Utf8Parser.TryParse(segment, out float tmp, out var bytesConsumed)
            && segment.Length == bytesConsumed)
        {
            value = tmp;
            return true;
        }

        value = 0;
        return false;
    }

    internal bool TryGetValue(int index, out decimal value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _parsedData.Get(index);

        CheckExpectedType(JsonTokenType.Number, row.TokenType);

        var segment = ReadRawValue(row);

        if (Utf8Parser.TryParse(segment, out decimal tmp, out var bytesConsumed)
            && segment.Length == bytesConsumed)
        {
            value = tmp;
            return true;
        }

        value = 0;
        return false;
    }
}
