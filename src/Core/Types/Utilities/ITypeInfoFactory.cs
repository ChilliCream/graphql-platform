using System;

namespace HotChocolate.Utilities
{
    internal interface ITypeInfoFactory
    {
        bool TryCreate(Type type, out TypeInfo typeInfo);
    }
}
