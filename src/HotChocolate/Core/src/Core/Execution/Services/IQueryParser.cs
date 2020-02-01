using System;
using HotChocolate.Language;

namespace HotChocolate.Execution
{
    public interface IQueryParser
    {
        DocumentNode Parse(ReadOnlySpan<byte> source);
    }
}
