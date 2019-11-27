using System;
using System.Collections.Generic;

#nullable enable

namespace HotChocolate.Utilities
{
    public class TypeNullability
    {
        public TypeNullability(Nullable state, Type type)
            : this(state, type, Array.Empty<TypeNullability>())
        {
        }

        public TypeNullability(
            Nullable state,
            Type type,
            IReadOnlyList<TypeNullability> genericArguments)
        {
            State = state;
            Type = type;
            GenericArguments = genericArguments;
        }

        public Nullable State { get; }

        public Type Type { get; }

        public IReadOnlyList<TypeNullability> GenericArguments { get; }
    }
}
