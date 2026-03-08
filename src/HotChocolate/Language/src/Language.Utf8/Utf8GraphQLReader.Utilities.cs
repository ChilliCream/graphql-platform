using System.Buffers;
using System.Runtime.CompilerServices;
using static HotChocolate.Language.Properties.LangUtf8Resources;

namespace HotChocolate.Language;

public ref partial struct Utf8GraphQLReader
{
    public readonly string GetString()
        => _value.Length == 0
            ? string.Empty
            : GetString(_value, _kind == TokenKind.BlockString);

    public static string GetString(
        ReadOnlySpan<byte> escapedValue,
        bool isBlockString)
    {
        if (escapedValue.Length == 0)
        {
            return string.Empty;
        }

        var length = escapedValue.Length;
        byte[]? unescapedArray = null;

        var unescapedSpan = length <= GraphQLCharacters.StackallocThreshold
            ? stackalloc byte[length]
            : unescapedArray = ArrayPool<byte>.Shared.Rent(length);

        try
        {
            UnescapeValue(escapedValue, ref unescapedSpan, isBlockString);
            return GetString(unescapedSpan);
        }
        finally
        {
            if (unescapedArray != null)
            {
                unescapedSpan.Clear();
                ArrayPool<byte>.Shared.Return(unescapedArray);
            }
        }
    }

    public readonly int GetRawString(IBufferWriter<byte> writer)
        => _value.Length == 0 ? 0 : GetRawString(_value, _kind == TokenKind.BlockString, writer);

    public static int GetRawString(
        ReadOnlySpan<byte> escapedValue,
        bool isBlockString,
        IBufferWriter<byte> writer)
    {
        // we have an empty string
        if (escapedValue.Length == 0)
        {
            return 0;
        }

        var length = escapedValue.Length;
        byte[]? unescapedArray = null;

        var unescapedSpan = length <= GraphQLCharacters.StackallocThreshold
            ? stackalloc byte[length]
            : unescapedArray = ArrayPool<byte>.Shared.Rent(length);

        try
        {
            UnescapeValue(escapedValue, ref unescapedSpan, isBlockString);

            // we have an empty string
            if (unescapedSpan.Length == 0)
            {
                return 0;
            }

            unescapedSpan.CopyTo(writer.GetSpan(unescapedSpan.Length));
            writer.Advance(unescapedSpan.Length);
            return unescapedSpan.Length;
        }
        finally
        {
            if (unescapedArray != null)
            {
                unescapedSpan.Clear();
                ArrayPool<byte>.Shared.Return(unescapedArray);
            }
        }
    }

    internal static bool TryGetRawString(
        ReadOnlySpan<byte> escapedValue,
        bool isBlockString,
        Span<byte> rawStringValue,
        out int written)
    {
        if (escapedValue.Length == 0)
        {
            written = 0;
            return true;
        }

        var length = escapedValue.Length;
        byte[]? unescapedArray = null;

        var unescapedSpan = length <= GraphQLCharacters.StackallocThreshold
            ? stackalloc byte[length]
            : unescapedArray = ArrayPool<byte>.Shared.Rent(length);

        try
        {
            UnescapeValue(escapedValue, ref unescapedSpan, isBlockString);

            if (unescapedSpan.Length > rawStringValue.Length)
            {
                written = 0;
                return false;
            }

            unescapedSpan.CopyTo(rawStringValue);
            written = unescapedSpan.Length;
            return true;
        }
        finally
        {
            if (unescapedArray != null)
            {
                unescapedSpan.Clear();
                ArrayPool<byte>.Shared.Return(unescapedArray);
            }
        }
    }

    public static unsafe string GetString(ReadOnlySpan<byte> unescapedValue)
    {
        if (unescapedValue.Length == 0)
        {
            return string.Empty;
        }

        fixed (byte* bytePtr = unescapedValue)
        {
            return StringHelper.UTF8Encoding.GetString(bytePtr, unescapedValue.Length);
        }
    }

    public string GetComment()
    {
        if (_value.Length != 0)
        {
            StringHelper.TrimStringToken(ref _value);
        }

        return GetString(_value);
    }

    public readonly string GetName()
        => WellKnownNames.TryGetWellKnownName(_value, out var name)
            ? name
            : GetString(_value);

    public readonly string GetScalarValue() => GetString(_value);

    private static void UnescapeValue(
        in ReadOnlySpan<byte> escaped,
        ref Span<byte> unescapedValue,
        bool isBlockString)
    {
        Utf8Helper.Unescape(in escaped, ref unescapedValue, isBlockString);

        if (isBlockString)
        {
            StringHelper.TrimBlockStringToken(unescapedValue, ref unescapedValue);
        }
    }

    public readonly void UnescapeValue(ref Span<byte> unescapedValue)
    {
        if (_value.Length == 0)
        {
            unescapedValue = unescapedValue.Slice(0, 0);
        }
        else
        {
            UnescapeValue(
                in _value,
                ref unescapedValue,
                _kind == TokenKind.BlockString);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool MoveNext()
    {
        bool read;

        do
        {
            read = Read();
        } while (read && _kind == TokenKind.Comment);

        return read;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool Skip(TokenKind kind)
    {
        if (_kind == kind)
        {
            MoveNext();
            return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ReadOnlySpan<byte> Expect(TokenKind kind)
    {
        var value = Value;

        if (!Skip(kind))
        {
            throw new SyntaxException(this, Parser_InvalidToken, kind, Kind);
        }

        return value;
    }
}
