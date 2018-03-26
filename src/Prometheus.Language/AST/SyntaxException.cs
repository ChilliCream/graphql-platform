namespace Prometheus.Language
{
    [System.Serializable]
    public class SyntaxException : System.Exception
    {
        public SyntaxException(ILexerContext context, string message) { }
    }
}