using System;
using HotChocolate.Internal;

namespace HotChocolate.Internal
{
    internal interface ITypeInfoFactory
    {
        bool TryCreate(Type type, out TypeInfo typeInfo);
    }
}
