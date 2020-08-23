using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using HotChocolate.Types;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Internal
{
    public sealed partial class TypeInfo
    {
        public static class RuntimeType
        {
            /// <summary>
            /// Removes non-essential parts from the type.
            /// </summary>
            public static IExtendedType Unwrap(Type type) =>
                Unwrap(ExtendedType.FromType(type));

            /// <summary>
            /// Removes non-essential parts from the type.
            /// </summary>
            public static IExtendedType Unwrap(IExtendedType type) =>
                RemoveNonEssentialParts(type);

            internal static bool TryCreateTypeInfo(
                IExtendedType type,
                Type originalType,
                [NotNullWhen(true)]out TypeInfo? typeInfo)
            {
                if (type.Kind != ExtendedTypeKind.Schema)
                {
                    IReadOnlyList<TypeComponent> components =
                        Decompose(type, out IExtendedType namedType);

                    if (IsStructureValid(components))
                    {
                        typeInfo = new TypeInfo(
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

            private static IReadOnlyList<TypeComponent> Decompose(
                IExtendedType type,
                out IExtendedType namedType)
            {
                var list = new List<TypeComponent>();
                IExtendedType? current = type;

                while (current is not null)
                {
                    current = RemoveNonEssentialParts(current);

                    if (!current.IsNullable)
                    {
                        list.Add((TypeComponentKind.NonNull, current));
                    }

                    if (current.IsArrayOrList)
                    {
                        list.Add((TypeComponentKind.List, current));
                        current = current.GetElementType();
                    }
                    else
                    {
                        list.Add((TypeComponentKind.Named, current));
                        namedType = current;
                        return list;
                    }
                }

                throw new InvalidOperationException("No named type component found.");
            }

            private static IExtendedType RemoveNonEssentialParts(IExtendedType type)
            {
                IExtendedType current = type;

                while (IsWrapperType(current) || IsTaskType(current) || IsOptional(current))
                {
                    current = type.TypeArguments[0];
                }

                return current;
            }

            private static bool IsWrapperType(IExtendedType type) =>
                type.IsGeneric &&
                typeof(NativeType<>) == type.Definition;

            private static bool IsTaskType(IExtendedType type) =>
                type.IsGeneric &&
                (typeof(Task<>) == type.Definition ||
                 typeof(ValueTask<>) == type.Definition);

            private static bool IsOptional(IExtendedType type) =>
                type.IsGeneric &&
                typeof(Optional<>) == type.Definition;
        }
    }
}
