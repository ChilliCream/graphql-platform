using System.Runtime.CompilerServices;

namespace StrawberryShake.VisualStudio.Language
{
    /// <summary>
    /// This class provides internal char utilities
    /// that are used to tokenize a GraphQL source text.
    /// These utilities are used by the lexer default implementation.
    /// </summary>
    internal static partial class GraphQLConstants
    {
        public const byte Plus = (byte)'+';
        public const byte Minus = (byte)'-';
        public const byte Bang = (byte)'!';
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
        public const byte Tab = (byte)'\t';
        public const byte Zero = (byte)'0';
        public const byte E = (byte)'e';
        public const byte NewLine = (byte)'\n';
        public const byte Quote = (byte)'"';
    }
}
