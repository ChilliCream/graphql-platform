using System;
using HotChocolate;
using HotChocolate.Language;

namespace StrawberryShake.CodeGeneration.Utilities
{
    public static class QueryDocumentRewriter
    {
        public static DocumentNode Rewrite(DocumentNode document, ISchema schema)
        {
            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (schema is null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            DocumentNode current = document;
            current = EntityIdRewriter.Rewrite(current, schema);
            current = TypeNameQueryRewriter.Rewrite(current);
            return RemoveClientDirectivesRewriter.Rewrite(current);
        }
    }
}
