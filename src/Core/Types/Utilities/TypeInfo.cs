using System.Collections.Generic;
using System;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Utilities
{
    public sealed class TypeInfo
    {
        public TypeInfo(
            Type clrType,
            IReadOnlyList<TypeComponent> components)
        {
            Type = clrType;
            Components = components;
        }

        public IReadOnlyList<TypeComponent> Components { get; }

        public Type Type { get; }

        public IType CreateSchemaType(INamedType namedType)
        {
            if (Components.Count == 1)
            {
                return namedType;
            }

            IType current = namedType;

            for (int i = Components.Count - 2; i >= 0; i--)
            {
                switch (Components[i].Kind)
                {
                    case TypeComponentKind.NonNull:
                        current = new NonNullType(current);
                        break;

                    case TypeComponentKind.List:
                        current = new ListType(current);
                        break;

                    default:
                        throw new InvalidOperationException(
                            "The type info components have an invalid structure.");
                }
            }

            return current;
        }
    }
}
