using System;
using HotChocolate.Types;

namespace HotChocolate.Internal
{
    internal readonly struct TypeInfo
    {
        public TypeInfo(Type nativeNamedType,
            Func<IType, IType> typeFactory)
        {
            NativeNamedType = nativeNamedType;
            TypeFactory = typeFactory;
        }

        public Type NativeNamedType { get; }

        public Func<IType, IType> TypeFactory { get; }
    }
}
