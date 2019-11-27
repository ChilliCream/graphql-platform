using System;

namespace StrawberryShake
{
    public interface IDocument
    {
        ReadOnlySpan<byte> HashName { get; }

        ReadOnlySpan<byte> Hash { get; }

        ReadOnlySpan<byte> Content { get; }
    }
}
