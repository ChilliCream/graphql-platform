#if !NET6_0_OR_GREATER
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#endif

namespace HotChocolate.Language;

internal static class HashCodeExtensions
{
#if !NET6_0_OR_GREATER
    public static void AddBytes(ref HashCode hashCode, ReadOnlySpan<byte> value)
    {
        ref var pos = ref MemoryMarshal.GetReference(value);
        ref var end = ref Unsafe.Add(ref pos, value.Length);

        // Add four bytes at a time until the input has fewer than four bytes remaining.
        while ((nint)Unsafe.ByteOffset(ref pos, ref end) >= sizeof(int))
        {
            hashCode.Add(Unsafe.ReadUnaligned<int>(ref pos));
            pos = ref Unsafe.Add(ref pos, sizeof(int));
        }

        // Add the remaining bytes a single byte at a time.
        while (Unsafe.IsAddressLessThan(ref pos, ref end))
        {
            hashCode.Add((int)pos);
            pos = ref Unsafe.Add(ref pos, 1);
        }
    }
#endif
}
