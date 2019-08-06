using System;

namespace HotChocolate.Language
{
    public interface IDocumentHashProvider
    {
        string Name { get; }

        string ComputeHash(ReadOnlySpan<byte> document);
    }
}
