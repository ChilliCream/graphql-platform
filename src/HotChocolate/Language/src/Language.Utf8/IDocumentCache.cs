using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Language
{
    public interface IDocumentCache
    {
        bool TryGetDocument(
            string documentId,
            [NotNullWhen(true)] out DocumentNode document);

        void TryAddDocument(
            string documentId,
            DocumentNode document);

        void Clear();
    }
}
