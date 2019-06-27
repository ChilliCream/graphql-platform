namespace HotChocolate.Language
{
    public interface IDocumentCache
    {
        bool TryGetDocument(string key, out DocumentNode document);
    }
}
