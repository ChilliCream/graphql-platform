namespace HotChocolate.Language
{
    public static class ParserExtensions
    {
        private static readonly Lexer _lexer = new Lexer();

        public static DocumentNode Parse(this IParser parser, ISource source)
        {
            return parser.Parse(_lexer, source);
        }

        public static DocumentNode Parse(this IParser parser, ISource source, ParserOptions options)
        {
            return parser.Parse(_lexer, source, options);
        }
    }
}
