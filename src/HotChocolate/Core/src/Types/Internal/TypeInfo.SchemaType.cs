using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace HotChocolate.Internal;

internal sealed partial class TypeInfo
{
    private static class SchemaType
    {
        internal static bool TryCreateTypeInfo(
            IExtendedType type,
            Type originalType,
            [NotNullWhen(true)] out TypeInfo? typeInfo)
        {
            if (type.Kind == ExtendedTypeKind.Schema)
            {
                var components =
                    Decompose(type, out var namedType);

                typeInfo = new TypeInfo(
                    namedType.Type,
                    originalType,
                    components,
                    true,
                    type,
                    IsStructureValid(components));
                return true;
            }

            typeInfo = null;
            return false;
        }

        private static IReadOnlyList<TypeComponent> Decompose(
            IExtendedType type,
            out IExtendedType namedType)
        {
            var list = new List<TypeComponent>();
            var current = type;

            while (current is not null)
            {
                if (!current.IsNullable)
                {
                    list.Add((TypeComponentKind.NonNull, current));
                }

                if (current.IsNamedType)
                {
                    list.Add((TypeComponentKind.Named, current));
                    namedType = current;
                    return list;
                }

                if (type.IsList)
                {
                    list.Add((TypeComponentKind.List, current));
                    current = current.ElementType;
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
