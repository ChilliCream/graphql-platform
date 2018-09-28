namespace HotChocolate.Language
{
    public static class ParserExtensions
    {
        public static DocumentNode Parse(this Parser parser, string sourceText)
        {
            return parser.Parse(new Source(sourceText));
        }

        public static DocumentNode Parse(this Parser parser,
            string sourceText, ParserOptions options)
        {
            return parser.Parse(new Source(sourceText), options);
        }

        public static IValueNode ParseJson(
            this Parser parser, string sourceText)
        {
            return parser.ParseJson(new Source(sourceText));
        }
    }
}
