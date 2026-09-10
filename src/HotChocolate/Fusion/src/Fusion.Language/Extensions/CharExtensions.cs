using System.Runtime.CompilerServices;
using static HotChocolate.Fusion.Language.CharConstants;

namespace HotChocolate.Fusion.Language;

internal static class CharExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLetterOrDigitOrUnderscore(this char c)
        => char.IsAsciiLetter(c) || char.IsAsciiDigit(c) || c == Underscore;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLetterOrUnderscore(this char c)
        => char.IsAsciiLetter(c) || c == Underscore;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPunctuator(this char c)
        => c is Colon or LeftAngleBracket or LeftBrace or LeftParenthesis or LeftSquareBracket or Period or Pipe or RightAngleBracket or RightBrace or RightParenthesis or RightSquareBracket;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsDigit(this char c)
        => char.IsAsciiDigit(c);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsDigitOrMinus(this char c)
        => char.IsAsciiDigit(c) || c == Minus;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsQuote(this char c)
        => c == Quote;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsControlCharacter(this char c)
        => (c < Space && c != HorizontalTab) || c == Delete;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidEscapeCharacter(this char c)
        => c is Quote or '/' or Backslash or 'b' or 'f' or 'n' or 'r' or 't' or 'u';
}
