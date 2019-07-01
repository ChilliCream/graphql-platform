using System;

namespace HotChocolate.Execution
{
    public interface IQuery
    {
        ReadOnlySpan<byte> ToSource();

        string ToString();
    }
}
