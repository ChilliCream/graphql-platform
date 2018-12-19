using System;
using HotChocolate.Types;

namespace HotChocolate.Utilities
{
    internal sealed class TypeInfo
    {
        public TypeInfo(Type clrType,
            Func<IType, IType> typeFactory)
        {
            ClrType = clrType;
            TypeFactory = typeFactory;
        }

        public Type ClrType { get; }

        public Func<IType, IType> TypeFactory { get; }
    }
}
