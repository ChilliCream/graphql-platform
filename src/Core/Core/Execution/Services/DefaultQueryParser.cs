using System;
using HotChocolate.Language;

namespace HotChocolate.Execution
{
    public class DefaultQueryParser
        : IQueryParser
    {
        public DocumentNode Parse(ReadOnlySpan<byte> source)
        {
            return new Utf8GraphQLParser(
                source,
                ParserOptions.Default).Parse();
        }
    }
}
