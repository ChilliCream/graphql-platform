using System.Runtime.CompilerServices;

namespace HotChocolate.Language;

/// <summary>
/// This class provides internal char utilities
/// that are used to tokenize a GraphQL source text.
/// These utilities are used by the lexer default implementation.
/// </summary>
internal static class GraphQLConstants
{
    public const int StackallocThreshold = 256;

    public const byte Null = (byte)'\u0000';
    public const byte StartOfHeading = (byte)'\u0001';
    public const byte StartOfText = (byte)'\u0002';
    public const byte EndOfText = (byte)'\u0003';
    public const byte EndOfTransmission = (byte)'\u0004';
    public const byte Enquiry = (byte)'\u0005';
    public const byte Acknowledgement = (byte)'\u0006';
    public const byte Bell = (byte)'\u0007';
    public const byte Backspace = (byte)'\b';
    public const byte HorizontalTab = (byte)'\t';
    public const byte LineFeed = (byte)'\n';
    public const byte VerticalTab = (byte)'\v';
    public const byte FormFeed = 12;
    public const byte Return = (byte)'\r';
    public const byte ShiftOut = 14;
    public const byte ShiftIn = 15;
    public const byte DataLinkEscape = 16;
    public const byte DeviceControl1 = 17;
    public const byte DeviceControl2 = 18;
    public const byte DeviceControl3 = 19;
    public const byte DeviceControl4 = 20;
    public const byte NegativeAcknowledgement = 21;
    public const byte SynchronousIdle = 22;
    public const byte EndOfTransmissionBlock = 23;
    public const byte Cancel = 24;
    public const byte EndOfMedium = 25;
    public const byte Substitute = 26;
    public const byte Escape = 27;
    public const byte FileSeparator = 28;
    public const byte GroupSeparator = 29;
    public const byte RecordSeparator = 30;
    public const byte UnitSeparator = 31;
    public const byte Delete = 127;

    public const byte A = (byte)'a';
    public const byte B = (byte)'b';
    public const byte E = (byte)'e';
    public const byte F = (byte)'f';
    public const byte N = (byte)'n';
    public const byte R = (byte)'r';
    public const byte T = (byte)'t';
    public const byte U = (byte)'u';
    public const byte Z = (byte)'z';

    public const byte Hyphen = (byte)'-';
    public const byte Underscore = (byte)'_';
    public const byte Plus = (byte)'+';
    public const byte Minus = (byte)'-';
    public const byte Backslash = (byte)'\\';
    public const byte ForwardSlash = (byte)'/';

    public const byte Bang = (byte)'!';
    public const byte QuestionMark = (byte)'?';
    public const byte Dollar = (byte)'$';
    public const byte Ampersand = (byte)'&';
    public const byte LeftParenthesis = (byte)'(';
    public const byte RightParenthesis = (byte)')';
    public const byte Colon = (byte)':';
    public const byte Equal = (byte)'=';
    public const byte At = (byte)'@';
    public const byte LeftBracket = (byte)'[';
    public const byte RightBracket = (byte)']';
    public const byte LeftBrace = (byte)'{';
    public const byte RightBrace = (byte)'}';
    public const byte Pipe = (byte)'|';
    public const byte Dot = (byte)'.';
    public const byte Space = (byte)' ';
    public const byte Hash = (byte)'#';

    public const byte Zero = (byte)'0';

    public const byte Quote = (byte)'"';
    public const byte Comma = (byte)',';

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLetterOrDigitOrUnderscore(this byte c)
    {
        if (c > 96 && c < 123 || c > 64 && c < 91)
        {
            return true;
        }

        if (c > 47 && c < 58)
        {
            return true;
        }

        if (Underscore == c)
        {
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLetterOrDigitOrUnderscore(this char c)
        => IsLetterOrDigitOrUnderscore((byte)c);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLetterOrUnderscore(this byte c)
    {
        if (c > 96 && c < 123 || c > 64 && c < 91)
        {
            return true;
        }

        if (Underscore == c)
        {
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLetterOrUnderscore(this char c)
        => IsLetterOrUnderscore((byte)c);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsDigit(this byte c)
        => c > 47 && c < 58;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsDigitOrMinus(this byte c)
    {
        if (c > 47 && c < 58)
        {
            return true;
        }

        if (c is Hyphen)
        {
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPunctuator(this byte c)
    {
        switch (c)
        {
            case Bang:
            case Dollar:
            case Ampersand:
            case LeftParenthesis:
            case RightParenthesis:
            case Dot:
            case Colon:
            case Equal:
            case QuestionMark:
            case At:
            case LeftBracket:
            case RightBracket:
            case LeftBrace:
            case Pipe:
            case RightBrace:
                return true;

            default:
                return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidEscapeCharacter(this byte c)
    {
        switch (c)
        {
            case Quote:
            case ForwardSlash:
            case Backslash:
            case B:
            case F:
            case N:
            case R:
            case T:
            case U:
                return true;

            default:
                return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte EscapeCharacter(this byte c)
    {
        switch (c)
        {
            case B:
                return Backspace;
            case F:
                return FormFeed;
            case N:
                return LineFeed;
            case R:
                return Return;
            case T:
                return HorizontalTab;

            default:
                return c;
        }
    }
}
