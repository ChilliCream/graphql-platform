using System.Collections.Generic;
using System;
using HotChocolate.Types;

namespace HotChocolate.Utilities
{
    public sealed class TypeInfo
    {
        public TypeInfo(Type clrType,
            IReadOnlyList<Type> components,
            Func<INamedType, IType> typeFactory)
        {
            ClrType = clrType;
            Components = components;
            TypeFactory = typeFactory;
        }

        public IReadOnlyList<Type> Components { get; }

        public Type ClrType { get; }

        public Func<INamedType, IType> TypeFactory { get; }
    }
}
