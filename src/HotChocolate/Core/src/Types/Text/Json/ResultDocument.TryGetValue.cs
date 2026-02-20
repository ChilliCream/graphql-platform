using System.Buffers.Text;

namespace HotChocolate.Text.Json;

public sealed partial class ResultDocument
{
    internal bool TryGetValue(Cursor cursor, out sbyte value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _metaDb.Get(cursor);
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

    internal bool TryGetValue(Cursor cursor, out byte value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _metaDb.Get(cursor);
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

    internal bool TryGetValue(Cursor cursor, out short value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _metaDb.Get(cursor);
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

    internal bool TryGetValue(Cursor cursor, out ushort value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _metaDb.Get(cursor);
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

    internal bool TryGetValue(Cursor cursor, out int value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _metaDb.Get(cursor);
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

    internal bool TryGetValue(Cursor cursor, out uint value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _metaDb.Get(cursor);
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

    internal bool TryGetValue(Cursor cursor, out long value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _metaDb.Get(cursor);
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

    internal bool TryGetValue(Cursor cursor, out ulong value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _metaDb.Get(cursor);
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

    internal bool TryGetValue(Cursor cursor, out double value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _metaDb.Get(cursor);
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

    internal bool TryGetValue(Cursor cursor, out float value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _metaDb.Get(cursor);
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

    internal bool TryGetValue(Cursor cursor, out decimal value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _metaDb.Get(cursor);
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
