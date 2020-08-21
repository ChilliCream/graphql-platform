using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace HotChocolate.Internal
{
    public sealed partial class TypeInfo2
    {
        private static class SchemaType
        {
            public static bool TryCreateTypeInfo(
                IExtendedType type,
                Type originalType,
                [NotNullWhen(true)]out TypeInfo2? typeInfo)
            {
                if (type.Kind == ExtendedTypeKind.Schema)
                {
                    IReadOnlyList<TypeComponentKind> components =
                        Decompose(type, out IExtendedType namedType);

                    if (IsStructureValid(components))
                    {
                        typeInfo = new TypeInfo2(
                            namedType.Type,
                            originalType,
                            components,
                            true,
                            type);
                        return true;
                    }
                }

                typeInfo = null;
                return false;
            }

            private static IReadOnlyList<TypeComponentKind> Decompose(
                IExtendedType type,
                out IExtendedType namedType)
            {
                var list = new List<TypeComponentKind>();
                IExtendedType? current = type;

                while (current is not null)
                {
                    if (!current.IsNullable)
                    {
                        list.Add(TypeComponentKind.NonNull);
                    }

                    if (current.IsNamedType)
                    {
                        list.Add(TypeComponentKind.Named);
                        namedType = current;
                        return list;
                    }

                    if (type.IsList)
                    {
                        list.Add(TypeComponentKind.List);
                        current = current.TypeArguments[0];
                    }
                    else
                    {
                        current = null;
                    }
                }

                throw new InvalidOperationException("No named type component found.");
            }
        }
    }
}
