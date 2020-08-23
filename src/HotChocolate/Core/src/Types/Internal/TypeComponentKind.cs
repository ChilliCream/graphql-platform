using System;
using System.Collections.Generic;

namespace HotChocolate.Internal
{
    public enum TypeComponentKind
    {
        NonNull,
        List,
        Named
    }

    public readonly struct TypeComponent
    {
        public TypeComponent(TypeComponentKind kind, IExtendedType type)
        {
            Kind = kind;
            Type = type;
        }

        public TypeComponentKind Kind { get; }

        public IExtendedType Type { get; }

        public override bool Equals(object obj)
        {
            return obj is TypeComponent component &&
                   Kind == component.Kind &&
                   Type == component.Type;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return Kind.GetHashCode() * 397 ^ Type.GetHashCode() * 397;
            }
        }

        public static implicit operator TypeComponent(
            (TypeComponentKind, IExtendedType) component) =>
            new TypeComponent(component.Item1, component.Item2);
    }
}
