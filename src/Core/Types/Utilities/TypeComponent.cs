using System;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Utilities
{
    public sealed class TypeComponent
    {
        public TypeComponent(TypeComponentKind kind, Type type)
        {
            Kind = kind;
            Type = type;
        }

        public TypeComponentKind Kind { get; }

        public Type Type { get; }

        public static TypeComponent NonNull { get; } = new TypeComponent(
            TypeComponentKind.NonNull,
            typeof(NonNullType<>));

        public static TypeComponent List { get; } = new TypeComponent(
            TypeComponentKind.List,
            typeof(ListType<>));
    }
}
