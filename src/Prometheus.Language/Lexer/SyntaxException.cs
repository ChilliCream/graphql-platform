namespace Prometheus.Language
{
    [System.Serializable]
    public class SyntaxException : System.Exception
    {
        public SyntaxException(ILexerContext context, string message) { }
        public SyntaxException(IParserContext context, string message) { }
        public SyntaxException(IParserContext context, Token token, string message) { }
    }
}