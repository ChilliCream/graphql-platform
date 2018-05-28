using System;

namespace HotChocolate.Types
{
    internal readonly struct TypeInfo
    {
        public TypeInfo(Type nativeNamedType)
        {
            NativeNamedType = nativeNamedType;
            TypeFactory = r => r.GetType<IType>(nativeNamedType);
        }

        public TypeInfo(Type nativeNamedType,
            Func<ITypeRegistry, IType> typeFactory)
        {
            NativeNamedType = nativeNamedType;
            TypeFactory = typeFactory;
        }

        public Type NativeNamedType { get; }
        public Func<ITypeRegistry, IType> TypeFactory { get; }
    }
}
