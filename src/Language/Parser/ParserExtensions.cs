namespace HotChocolate.Language
{
    public static class ParserExtensions
    {
        private static readonly Lexer _lexer = new Lexer();

        public static DocumentNode Parse(this IParser parser, ISource source)
        {
            return parser.Parse(_lexer, source);
        }

        public static DocumentNode Parse(this IParser parser,
            ISource source, ParserOptions options)
        {
            return parser.Parse(_lexer, source, options);
        }

        public static DocumentNode Parse(this IParser parser, string sourceText)
        {
            return parser.Parse(_lexer, new Source(sourceText));
        }

        public static DocumentNode Parse(this IParser parser,
            string sourceText, ParserOptions options)
        {
            return parser.Parse(_lexer, new Source(sourceText), options);
        }

        public static DocumentNode Parse(this IParser parser,
            ILexer lexer, string sourceText)
        {
            return parser.Parse(lexer, new Source(sourceText));
        }

        public static DocumentNode Parse(this IParser parser, ILexer lexer,
            string sourceText, ParserOptions options)
        {
            return parser.Parse(lexer, new Source(sourceText), options);
        }
    }
}
