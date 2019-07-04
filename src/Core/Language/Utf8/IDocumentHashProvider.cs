using System;

namespace HotChocolate.Language
{
    public interface IDocumentHashProvider
    {
        string ComputeHash(ReadOnlySpan<byte> document);
    }
}
