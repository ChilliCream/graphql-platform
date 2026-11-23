using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace HotChocolate.Fusion.Transport.Http;

/// <summary>
/// Finds the end of the root object or array of a UTF-8 JSON document that arrives in several reads.
/// </summary>
internal struct JsonRootEndScanner
{
    private const int BlockSize = 64;
    private const ulong OddBits = 0xAAAAAAAAAAAAAAAA;

    private int _depth;
    private bool _inString;
    private bool _escaped;
    private bool _completed;

    private static ReadOnlySpan<byte> StructuralBytes => "\"{}[]"u8;

    private static ReadOnlySpan<byte> WhitespaceBytes => " \t\r\n"u8;

    /// <summary>
    /// Scans the next bytes of the document and returns the index just past the end of the root
    /// object or array, or <c>-1</c> when the root has not ended within <paramref name="span"/>.
    /// After it reported an end, or after a closing bracket without a matching opening bracket,
    /// it returns <c>-1</c> for every further call.
    /// </summary>
    public int Scan(ReadOnlySpan<byte> span)
    {
        if (_completed)
        {
            return -1;
        }

        var i = 0;

        if (Vector128.IsHardwareAccelerated)
        {
            while (span.Length - i >= BlockSize)
            {
                var end = ScanBlock(span.Slice(i, BlockSize));

                if (end >= 0)
                {
                    return i + end;
                }

                if (_completed)
                {
                    return -1;
                }

                i += BlockSize;
            }
        }

        return ScanScalar(span, i);
    }

    /// <summary>
    /// Returns <c>true</c> when <paramref name="span"/> contains only JSON whitespace.
    /// </summary>
    public static bool IsWhitespace(ReadOnlySpan<byte> span)
        => span.IndexOfAnyExcept(WhitespaceBytes) < 0;

    private int ScanBlock(ReadOnlySpan<byte> block)
    {
        ref var start = ref MemoryMarshal.GetReference(block);
        var v0 = Vector128.LoadUnsafe(ref start);
        var v1 = Vector128.LoadUnsafe(ref start, 16);
        var v2 = Vector128.LoadUnsafe(ref start, 32);
        var v3 = Vector128.LoadUnsafe(ref start, 48);

        var quotes = Mask(v0, v1, v2, v3, (byte)'"');
        var backslashes = Mask(v0, v1, v2, v3, (byte)'\\');

        // '{' and '[' as well as '}' and ']' only differ in bit 0x20.
        var lowerCase = Vector128.Create((byte)0x20);
        var opens = Mask(v0 | lowerCase, v1 | lowerCase, v2 | lowerCase, v3 | lowerCase, (byte)'{');
        var closes = Mask(v0 | lowerCase, v1 | lowerCase, v2 | lowerCase, v3 | lowerCase, (byte)'}');

        var nextIsEscaped = _escaped ? 1UL : 0UL;
        ulong escaped;

        if (backslashes == 0)
        {
            escaped = nextIsEscaped;
            _escaped = false;
        }
        else
        {
            var potentialEscape = backslashes & ~nextIsEscaped;
            var escapeAndTerminalCode = (((potentialEscape << 1) | OddBits) - potentialEscape) ^ OddBits;
            escaped = escapeAndTerminalCode ^ (backslashes | nextIsEscaped);
            _escaped = ((escapeAndTerminalCode & backslashes) >> 63) != 0;
        }

        quotes &= ~escaped;

        var inString = PrefixXor(quotes);

        if (_inString)
        {
            inString = ~inString;
        }

        _inString = (long)inString < 0;

        opens &= ~inString;
        closes &= ~inString;

        if (closes == 0)
        {
            _depth += BitOperations.PopCount(opens);
            return -1;
        }

        var closeCount = BitOperations.PopCount(closes);

        if (_depth - closeCount > 0)
        {
            _depth += BitOperations.PopCount(opens) - closeCount;
            return -1;
        }

        var structural = opens | closes;

        while (structural != 0)
        {
            var index = BitOperations.TrailingZeroCount(structural);

            if ((closes & (1UL << index)) != 0)
            {
                _depth--;

                if (_depth == 0)
                {
                    _completed = true;
                    return index + 1;
                }

                if (_depth < 0)
                {
                    _completed = true;
                    return -1;
                }
            }
            else
            {
                _depth++;
            }

            structural &= structural - 1;
        }

        return -1;
    }

    private int ScanScalar(ReadOnlySpan<byte> span, int i)
    {
        while (i < span.Length)
        {
            if (_inString)
            {
                if (_escaped)
                {
                    _escaped = false;
                    i++;
                    continue;
                }

                var next = span[i..].IndexOfAny((byte)'"', (byte)'\\');

                if (next < 0)
                {
                    return -1;
                }

                i += next;

                if (span[i] == (byte)'\\')
                {
                    _escaped = true;
                    i++;
                    continue;
                }

                _inString = false;
                i++;
                continue;
            }

            var structural = span[i..].IndexOfAny(StructuralBytes);

            if (structural < 0)
            {
                return -1;
            }

            i += structural;

            switch (span[i])
            {
                case (byte)'"':
                    _inString = true;
                    break;

                case (byte)'{':
                case (byte)'[':
                    _depth++;
                    break;

                default:
                    _depth--;

                    if (_depth == 0)
                    {
                        _completed = true;
                        return i + 1;
                    }

                    if (_depth < 0)
                    {
                        _completed = true;
                        return -1;
                    }

                    break;
            }

            i++;
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Mask(
        Vector128<byte> v0,
        Vector128<byte> v1,
        Vector128<byte> v2,
        Vector128<byte> v3,
        byte value)
    {
        var search = Vector128.Create(value);
        return Vector128.Equals(v0, search).ExtractMostSignificantBits()
            | ((ulong)Vector128.Equals(v1, search).ExtractMostSignificantBits() << 16)
            | ((ulong)Vector128.Equals(v2, search).ExtractMostSignificantBits() << 32)
            | ((ulong)Vector128.Equals(v3, search).ExtractMostSignificantBits() << 48);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong PrefixXor(ulong bits)
    {
        bits ^= bits << 1;
        bits ^= bits << 2;
        bits ^= bits << 4;
        bits ^= bits << 8;
        bits ^= bits << 16;
        bits ^= bits << 32;
        return bits;
    }
}
