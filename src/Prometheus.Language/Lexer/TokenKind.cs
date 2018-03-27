using System;

namespace Prometheus.Language
{
    public sealed class TokenKind
        : IEquatable<TokenKind>
    {
        private string _value;

        private TokenKind(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            _value = value;
        }

        public bool Equals(TokenKind other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return other._value.Equals(_value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return Equals(obj as TokenKind);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return 199 * base.GetHashCode();
            }
        }

        public override string ToString()
        {
            return _value;
        }

        public static TokenKind StartOfFile { get; } = new TokenKind("<SOF>");
        public static TokenKind EndOfFile { get; } = new TokenKind("<EOF>");
        public static TokenKind Bang { get; } = new TokenKind("!");
        public static TokenKind Dollar { get; } = new TokenKind("$");
        public static TokenKind Ampersand { get; } = new TokenKind("&");
        public static TokenKind LeftParenthesis { get; } = new TokenKind("(");
        public static TokenKind RightParenthesis { get; } = new TokenKind(")");
        public static TokenKind Spread { get; } = new TokenKind("...");
        public static TokenKind Colon { get; } = new TokenKind(":");
        public static TokenKind Equal { get; } = new TokenKind("=");
        public static TokenKind At { get; } = new TokenKind("@");
        public static TokenKind LeftBracket { get; } = new TokenKind("[");
        public static TokenKind RightBracket { get; } = new TokenKind("]");
        public static TokenKind LeftBrace { get; } = new TokenKind("{");
        public static TokenKind RightBrace { get; } = new TokenKind("}");
        public static TokenKind Pipe { get; } = new TokenKind("|");
        public static TokenKind Name { get; } = new TokenKind("Name");
        public static TokenKind Integer { get; } = new TokenKind("Int");
        public static TokenKind Float { get; } = new TokenKind("Float");
        public static TokenKind String { get; } = new TokenKind("String");
        public static TokenKind BlockString { get; } = new TokenKind("BlockString");
        public static TokenKind Comment { get; } = new TokenKind("Comment");
    }
}