using System;
using HotChocolate.Language;

namespace HotChocolate.Execution
{
    public class QueryDocument
        : IQuery
    {
        public QueryDocument(DocumentNode document)
        {
            Document = document
                ?? throw new ArgumentNullException(nameof(document));
        }

        public DocumentNode Document { get; }
    }
}
