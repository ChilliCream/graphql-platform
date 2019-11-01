using System;

namespace HotChocolate.Language
{
    public interface IHasSpan
    {
        ReadOnlySpan<byte> AsSpan();
    }
}
