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

        if (Vector128.IsHardwareAccelerated && span.Length >= BlockSize)
        {
            var end = ScanBlocks(span, out i);

            if (_completed)
            {
                return end;
            }
        }

        return ScanScalar(span, i);
    }

    /// <summary>
    /// Returns <c>true</c> when <paramref name="span"/> contains only JSON whitespace.
    /// </summary>
    public static bool IsWhitespace(ReadOnlySpan<byte> span)
        => span.IndexOfAnyExcept(WhitespaceBytes) < 0;

    /// <summary>
    /// Scans the blocks of <see cref="BlockSize"/> bytes that <paramref name="span"/> holds in full,
    /// with the result of <see cref="Scan"/>. <paramref name="scanned"/> is the number of bytes
    /// that were scanned when the scan has not completed.
    /// </summary>
    private int ScanBlocks(ReadOnlySpan<byte> span, out int scanned)
    {
        ref var start = ref MemoryMarshal.GetReference(span);
        var lastBlock = span.Length - BlockSize;
        var depth = _depth;
        var inString = _inString ? ulong.MaxValue : 0UL;
        var nextIsEscaped = _escaped ? 1UL : 0UL;
        var i = 0;

        for (; i <= lastBlock; i += BlockSize)
        {
            ref var block = ref Unsafe.Add(ref start, i);
            var quotes = Mask(ref block, (byte)'"');
            var backslashes = Mask(ref block, (byte)'\\');

            if ((quotes | (backslashes >> (BlockSize - 1)) | ~inString) == 0)
            {
                // The block lies inside a string and does not escape the first byte of the next block,
                // like the blocks after it that neither have a quote nor end with a backslash.
                nextIsEscaped = 0;

                while (i + BlockSize <= lastBlock && IsStringContent(ref Unsafe.Add(ref start, i + BlockSize)))
                {
                    i += BlockSize;
                }

                continue;
            }

            if (backslashes == 0)
            {
                quotes &= ~nextIsEscaped;
                nextIsEscaped = 0;
            }
            else
            {
                var potentialEscape = backslashes & ~nextIsEscaped;
                var escapeAndTerminalCode = (((potentialEscape << 1) | OddBits) - potentialEscape) ^ OddBits;
                quotes &= ~(escapeAndTerminalCode ^ (backslashes | nextIsEscaped));
                nextIsEscaped = (escapeAndTerminalCode & backslashes) >> 63;
            }

            // A bit of inStringBits is set for an opening quote and the bytes of its string.
            var inStringBits = PrefixXor(quotes) ^ inString;
            inString = (ulong)((long)inStringBits >> 63);

            var opens = BracketMask(ref block, (byte)'{') & ~inStringBits;
            var closes = BracketMask(ref block, (byte)'}') & ~inStringBits;
            var closeCount = BitOperations.PopCount(closes);

            if (depth > closeCount || closeCount == 0)
            {
                depth += BitOperations.PopCount(opens) - closeCount;
                continue;
            }

            // Applies the brackets of a block in which the root can end one at a time.
            var structural = opens | closes;

            while (structural != 0)
            {
                var index = BitOperations.TrailingZeroCount(structural);

                if ((closes & (1UL << index)) == 0)
                {
                    depth++;
                }
                else if (--depth <= 0)
                {
                    _depth = depth;
                    _completed = true;
                    scanned = i;
                    return depth == 0 ? i + index + 1 : -1;
                }

                structural &= structural - 1;
            }
        }

        _depth = depth;
        _inString = inString != 0;
        _escaped = nextIsEscaped != 0;
        scanned = i;
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

    /// <summary>
    /// Gets a bit for every byte of the block at <paramref name="block"/> that is <paramref name="value"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Mask(ref byte block, byte value)
    {
        if (Vector256.IsHardwareAccelerated)
        {
            var search = Vector256.Create(value);
            var m0 = Vector256.Equals(Vector256.LoadUnsafe(ref block), search).ExtractMostSignificantBits();
            var m1 = Vector256.Equals(Vector256.LoadUnsafe(ref block, 32), search).ExtractMostSignificantBits();

            return m0 | ((ulong)m1 << 32);
        }
        else
        {
            var search = Vector128.Create(value);
            var m0 = Vector128.Equals(Vector128.LoadUnsafe(ref block), search).ExtractMostSignificantBits();
            var m1 = Vector128.Equals(Vector128.LoadUnsafe(ref block, 16), search).ExtractMostSignificantBits();
            var m2 = Vector128.Equals(Vector128.LoadUnsafe(ref block, 32), search).ExtractMostSignificantBits();
            var m3 = Vector128.Equals(Vector128.LoadUnsafe(ref block, 48), search).ExtractMostSignificantBits();

            return m0 | ((ulong)m1 << 16) | ((ulong)m2 << 32) | ((ulong)m3 << 48);
        }
    }

    /// <summary>
    /// Gets a bit for every byte of the block at <paramref name="block"/> that is the curly bracket
    /// <paramref name="curlyBracket"/> or the square bracket of the same direction.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong BracketMask(ref byte block, byte curlyBracket)
    {
        // '{' and '[' as well as '}' and ']' only differ in bit 0x20.
        if (Vector256.IsHardwareAccelerated)
        {
            var search = Vector256.Create(curlyBracket);
            var lowerCase = Vector256.Create((byte)0x20);
            var v0 = Vector256.LoadUnsafe(ref block) | lowerCase;
            var v1 = Vector256.LoadUnsafe(ref block, 32) | lowerCase;
            var m0 = Vector256.Equals(v0, search).ExtractMostSignificantBits();
            var m1 = Vector256.Equals(v1, search).ExtractMostSignificantBits();

            return m0 | ((ulong)m1 << 32);
        }
        else
        {
            var search = Vector128.Create(curlyBracket);
            var lowerCase = Vector128.Create((byte)0x20);
            var v0 = Vector128.LoadUnsafe(ref block) | lowerCase;
            var v1 = Vector128.LoadUnsafe(ref block, 16) | lowerCase;
            var v2 = Vector128.LoadUnsafe(ref block, 32) | lowerCase;
            var v3 = Vector128.LoadUnsafe(ref block, 48) | lowerCase;
            var m0 = Vector128.Equals(v0, search).ExtractMostSignificantBits();
            var m1 = Vector128.Equals(v1, search).ExtractMostSignificantBits();
            var m2 = Vector128.Equals(v2, search).ExtractMostSignificantBits();
            var m3 = Vector128.Equals(v3, search).ExtractMostSignificantBits();

            return m0 | ((ulong)m1 << 16) | ((ulong)m2 << 32) | ((ulong)m3 << 48);
        }
    }

    /// <summary>
    /// Returns <c>true</c> when the block at <paramref name="block"/> neither has a quote nor ends
    /// with a backslash.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsStringContent(ref byte block)
    {
        if (Unsafe.Add(ref block, BlockSize - 1) == (byte)'\\')
        {
            return false;
        }

        if (Vector256.IsHardwareAccelerated)
        {
            var quote = Vector256.Create((byte)'"');
            var v0 = Vector256.LoadUnsafe(ref block);
            var v1 = Vector256.LoadUnsafe(ref block, 32);

            return (Vector256.Equals(v0, quote) | Vector256.Equals(v1, quote)) == Vector256<byte>.Zero;
        }
        else
        {
            var quote = Vector128.Create((byte)'"');
            var v0 = Vector128.LoadUnsafe(ref block);
            var v1 = Vector128.LoadUnsafe(ref block, 16);
            var v2 = Vector128.LoadUnsafe(ref block, 32);
            var v3 = Vector128.LoadUnsafe(ref block, 48);

            return (Vector128.Equals(v0, quote)
                | Vector128.Equals(v1, quote)
                | Vector128.Equals(v2, quote)
                | Vector128.Equals(v3, quote)) == Vector128<byte>.Zero;
        }
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
