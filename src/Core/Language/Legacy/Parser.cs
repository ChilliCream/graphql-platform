using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    [Obsolete("Use the Utf8GraphQLParser.")]
    public sealed partial class Parser
    {
        public DocumentNode Parse(ISource source)
        {
            return Parse(source, ParserOptions.Default);
        }

        public DocumentNode Parse(ISource source, ParserOptions options)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return Utf8GraphQLParser.Parse(source.Text, options);
        }


        public static Parser Default { get; } = new Parser();
    }
}
