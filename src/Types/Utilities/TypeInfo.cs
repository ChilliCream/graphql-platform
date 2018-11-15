using System;
using HotChocolate.Types;

namespace HotChocolate.Utilities
{
    internal sealed class TypeInfo
    {
        public TypeInfo(Type nativeNamedType,
            Func<IType, IType> typeFactory)
        {
            NamedType = nativeNamedType;
            TypeFactory = typeFactory;
        }

        public Type NamedType { get; }

        public Func<IType, IType> TypeFactory { get; }
    }
}
