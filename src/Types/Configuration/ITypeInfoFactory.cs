using System;

namespace HotChocolate.Configuration
{
    internal interface ITypeInfoFactory
    {
        bool TryCreate(Type type, out TypeInfo typeInfo);
    }
}
