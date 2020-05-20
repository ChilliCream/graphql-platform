namespace HotChocolate.Language
{
    public interface IDocumentCache
    {
        bool TryGetDocument(string queryId, out DocumentNode document);

        void TryAddDocument(string queryId, DocumentNode document);
    }
}
