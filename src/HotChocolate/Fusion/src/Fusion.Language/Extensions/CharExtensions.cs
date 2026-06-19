using System.Runtime.CompilerServices;
using static HotChocolate.Fusion.Language.CharConstants;

namespace HotChocolate.Fusion.Language;

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
            case LeftParenthesis:
            case LeftSquareBracket:
            case Period:
            case Pipe:
            case RightAngleBracket:
            case RightBrace:
            case RightParenthesis:
            case RightSquareBracket:
                return true;

            default:
                return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsDigit(this char c)
    {
        return char.IsAsciiDigit(c);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsDigitOrMinus(this char c)
    {
        return char.IsAsciiDigit(c) || c == Minus;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsQuote(this char c)
    {
        return c == Quote;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsControlCharacter(this char c)
    {
        return (c < Space && c != HorizontalTab) || c == Delete;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidEscapeCharacter(this char c)
    {
        switch (c)
        {
            case Quote:
            case '/':
            case Backslash:
            case 'b':
            case 'f':
            case 'n':
            case 'r':
            case 't':
            case 'u':
                return true;

            default:
                return false;
        }
    }
}
