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

        var unescapedSpan = length <= GraphQLConstants.StackallocThreshold
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

        var unescapedSpan = length <= GraphQLConstants.StackallocThreshold
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

    public readonly string GetName() => GetString(_value);

    public readonly string GetScalarValue() => GetString(_value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void UnescapeValue(
        in ReadOnlySpan<byte> escaped,
        ref Span<byte> unescapedValue,
        bool isBlockString)
    {
        Utf8Helper.Unescape(
            in escaped,
            ref unescapedValue,
            isBlockString);

        if (isBlockString)
        {
            StringHelper.TrimBlockStringToken(
                unescapedValue, ref unescapedValue);
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
