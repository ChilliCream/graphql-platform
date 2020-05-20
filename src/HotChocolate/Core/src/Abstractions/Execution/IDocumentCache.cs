using System;
using HotChocolate.Language;

namespace HotChocolate.Execution
{
    public interface IDocumentCache
    {
        bool TryGetDocument(string queryId, out DocumentNode document);

        DocumentNode GetOrParseDocument(
            string queryId,
            IQuery query,
            Func<IQuery, DocumentNode> parseDocument);
    }
}
