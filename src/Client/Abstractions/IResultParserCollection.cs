using System;
using System.Collections.Generic;

namespace StrawberryShake
{
    public interface IResultParserCollection
        : IReadOnlyCollection<IResultParser>
    {
        IResultParser GetByResult(Type resultType);
    }
}
