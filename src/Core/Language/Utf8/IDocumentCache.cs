namespace HotChocolate.Language
{
    public interface IDocumentCache
    {
        bool TryGet(string key, out DocumentNode document);
    }
}
