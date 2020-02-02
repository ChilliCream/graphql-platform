using System;

namespace HotChocolate.Language
{
    [Obsolete("Use the Utf8GraphQLParser.")]
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
    }
}
