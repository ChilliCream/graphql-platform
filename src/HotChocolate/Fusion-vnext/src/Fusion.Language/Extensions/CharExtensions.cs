using System.Runtime.CompilerServices;
using static HotChocolate.Fusion.CharConstants;

namespace HotChocolate.Fusion;

internal static class CharExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLetterOrDigitOrUnderscore(this char c)
    {
        return char.IsAsciiLetter(c) || char.IsAsciiDigit(c) || c == Underscore;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLetterOrUnderscore(this char c)
    {
        return char.IsAsciiLetter(c) || c == Underscore;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPunctuator(this char c)
    {
        switch (c)
        {
            case Colon:
            case LeftAngleBracket:
            case LeftBrace:
            case LeftSquareBracket:
            case Period:
            case Pipe:
            case RightAngleBracket:
            case RightBrace:
            case RightSquareBracket:
                return true;

            default:
                return false;
        }
    }
}
