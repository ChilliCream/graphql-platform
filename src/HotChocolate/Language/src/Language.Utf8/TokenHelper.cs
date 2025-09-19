using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HotChocolate.Language;

internal static class TokenHelper
{
    private static readonly bool[] s_isString = new bool[22];
    private static readonly bool[] s_isScalar = new bool[22];

    static TokenHelper()
    {
        s_isString[(int)TokenKind.BlockString] = true;
        s_isString[(int)TokenKind.String] = true;

        s_isScalar[(int)TokenKind.BlockString] = true;
        s_isScalar[(int)TokenKind.String] = true;
        s_isScalar[(int)TokenKind.Integer] = true;
        s_isScalar[(int)TokenKind.Float] = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsDescription(ref Utf8GraphQLReader reader)
    {
        ref var searchSpace = ref MemoryMarshal.GetReference(s_isString.AsSpan());
        var index = (int)reader.Kind;
        return Unsafe.Add(ref searchSpace, index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsString(ref Utf8GraphQLReader reader)
    {
        ref var searchSpace = ref MemoryMarshal.GetReference(s_isString.AsSpan());
        var index = (int)reader.Kind;
        return Unsafe.Add(ref searchSpace, index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsScalarValue(ref Utf8GraphQLReader reader)
    {
        ref var searchSpace = ref MemoryMarshal.GetReference(s_isScalar.AsSpan());
        var index = (int)reader.Kind;
        return Unsafe.Add(ref searchSpace, index);
    }
}
