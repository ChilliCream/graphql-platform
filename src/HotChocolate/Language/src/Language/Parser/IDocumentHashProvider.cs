using System;

namespace HotChocolate.Language
{
    public interface IDocumentHashProvider
    {
        string Name { get; }

        HashFormat Format { get; }

        string ComputeHash(ReadOnlySpan<byte> document);
    }
}
