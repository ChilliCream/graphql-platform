using System.Buffers.Text;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class CompositeResultDocument
{
    internal bool TryGetValue(int index, out sbyte value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _metaDb.Get(index);

        CheckExpectedType(ElementTokenType.Number, row.TokenType);

        var rawValue = ReadRawValue(row);

        if (Utf8Parser.TryParse(rawValue, out sbyte tmp, out var consumed)
            && consumed == rawValue.Length)
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

        var row = _metaDb.Get(index);

        CheckExpectedType(ElementTokenType.Number, row.TokenType);

        var rawValue = ReadRawValue(row);

        if (Utf8Parser.TryParse(rawValue, out byte tmp, out var consumed)
            && consumed == rawValue.Length)
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

        var row = _metaDb.Get(index);

        CheckExpectedType(ElementTokenType.Number, row.TokenType);

        var rawValue = ReadRawValue(row);

        if (Utf8Parser.TryParse(rawValue, out short tmp, out var consumed)
            && consumed == rawValue.Length)
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

        var row = _metaDb.Get(index);

        CheckExpectedType(ElementTokenType.Number, row.TokenType);

        var rawValue = ReadRawValue(row);

        if (Utf8Parser.TryParse(rawValue, out ushort tmp, out var consumed)
            && consumed == rawValue.Length)
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

        var row = _metaDb.Get(index);

        CheckExpectedType(ElementTokenType.Number, row.TokenType);

        var rawValue = ReadRawValue(row);

        if (Utf8Parser.TryParse(rawValue, out int tmp, out var consumed)
            && consumed == rawValue.Length)
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

        var row = _metaDb.Get(index);

        CheckExpectedType(ElementTokenType.Number, row.TokenType);

        var rawValue = ReadRawValue(row);

        if (Utf8Parser.TryParse(rawValue, out uint tmp, out var consumed)
            && consumed == rawValue.Length)
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

        var row = _metaDb.Get(index);

        CheckExpectedType(ElementTokenType.Number, row.TokenType);

        var rawValue = ReadRawValue(row);

        if (Utf8Parser.TryParse(rawValue, out long tmp, out var consumed)
            && consumed == rawValue.Length)
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

        var row = _metaDb.Get(index);

        CheckExpectedType(ElementTokenType.Number, row.TokenType);

        var rawValue = ReadRawValue(row);

        if (Utf8Parser.TryParse(rawValue, out ulong tmp, out var consumed)
            && consumed == rawValue.Length)
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

        var row = _metaDb.Get(index);

        CheckExpectedType(ElementTokenType.Number, row.TokenType);

        var rawValue = ReadRawValue(row);

        if (Utf8Parser.TryParse(rawValue, out double tmp, out var bytesConsumed)
            && rawValue.Length == bytesConsumed)
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

        var row = _metaDb.Get(index);

        CheckExpectedType(ElementTokenType.Number, row.TokenType);

        var rawValue = ReadRawValue(row);

        if (Utf8Parser.TryParse(rawValue, out float tmp, out var bytesConsumed)
            && rawValue.Length == bytesConsumed)
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

        var row = _metaDb.Get(index);

        CheckExpectedType(ElementTokenType.Number, row.TokenType);

        var rawValue = ReadRawValue(row);

        if (Utf8Parser.TryParse(rawValue, out decimal tmp, out var bytesConsumed)
            && rawValue.Length == bytesConsumed)
        {
            value = tmp;
            return true;
        }

        value = 0;
        return false;
    }
}
