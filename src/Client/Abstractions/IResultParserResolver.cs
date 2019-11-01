using System;

namespace StrawberryShake
{
    public interface IResultParserResolver
    {
        IResultParser GetResultParser(Type resultType);
    }
}
