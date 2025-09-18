using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Internal;

internal sealed partial class TypeInfo
{
    public static class RuntimeType
    {
        /// <summary>
        /// Removes non-essential parts from the type.
        /// </summary>
        public static IExtendedType Unwrap(IExtendedType type) =>
            RemoveNonEssentialParts(type);

        internal static bool TryCreateTypeInfo(
            IExtendedType type,
            Type originalType,
            TypeCache cache,
            [NotNullWhen(true)] out TypeInfo? typeInfo)
        {
            if (type.Kind != ExtendedTypeKind.Schema)
            {
                var components =
                    Decompose(type, cache, out var namedType);

                typeInfo = new TypeInfo(
                    namedType.Type,
                    originalType,
                    components,
                    false,
                    type,
                    IsStructureValid(components));
                return true;
            }

            typeInfo = null;
            return false;
        }

        private static IReadOnlyList<TypeComponent> Decompose(
            IExtendedType type,
            TypeCache cache,
            out IExtendedType namedType)
        {
            var list = new List<TypeComponent>();
            var current = type;

            while (current is not null)
            {
                current = RemoveNonEssentialParts(current);

                if (!current.IsNullable)
                {
                    list.Add((TypeComponentKind.NonNull, current));
                }

                if (current.IsArrayOrList)
                {
                    var rewritten = current.IsNullable
                        ? current
                        : ExtendedType.Tools.ChangeNullability(
                            current,
                            [true],
                            cache);

                    list.Add((TypeComponentKind.List, rewritten));
                    current = current.ElementType;
                }
                else
                {
                    var rewritten = current.IsNullable
                        ? current
                        : ExtendedType.Tools.ChangeNullability(
                            current,
                            [true],
                            cache);

                    list.Add((TypeComponentKind.Named, rewritten));
                    namedType = current;
                    return list;
                }
            }

            throw new InvalidOperationException("No named type component found.");
        }

        private static IExtendedType RemoveNonEssentialParts(IExtendedType type)
        {
            short i = 0;
            var current = type;

            while (
                type.IsGeneric
                && current.Definition is { } definition
                && (ExtendedType.NonEssentialWrapperTypes.Contains(definition)
                    || typeof(IFieldResult).IsAssignableFrom(type)))
            {
                current = type.TypeArguments[0];

                if (i++ > 64)
                {
                    throw new InvalidOperationException(
                        "Could not remove the non-essential parts of the type.");
                }
            }

            return current;
        }
    }
}
