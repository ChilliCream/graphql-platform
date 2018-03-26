namespace Prometheus.Language
{
    public enum TokenKind2
    {
        StartOfFile,
        EndOfFile,
        Bang,
        Dollar,
        Ampersand,
        LeftParenthesis,
        RightParenthesis,
        Spread,
    }

    // TODO : use enum instead 
    public sealed class TokenKind
    {
        private string _value;

        private TokenKind(string value)
        {
            _value = value;
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