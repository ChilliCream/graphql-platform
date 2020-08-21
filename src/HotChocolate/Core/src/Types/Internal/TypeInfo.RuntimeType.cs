using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Types;
using HotChocolate.Utilities;
using Nullable = HotChocolate.Utilities.Nullable;

#nullable enable

namespace HotChocolate.Internal
{
    public sealed partial class TypeInfo2
    {
        private static class RuntimeType
        {
            public static bool TryCreateTypeInfo(
                IExtendedType type,
                Type originalType,
                [NotNullWhen(true)]out TypeInfo2? typeInfo)
            {
                if (type.Kind != ExtendedTypeKind.Schema)
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
                    current = RemoveNonEssentialParts(current);

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

            private static bool IsNamedType(IExtendedType type)
            {
                return typeof(ScalarType).IsAssignableFrom(type) ||
                   typeof(ObjectType).IsAssignableFrom(type) ||
                   typeof(InterfaceType).IsAssignableFrom(type) ||
                   typeof(EnumType).IsAssignableFrom(type) ||
                   typeof(UnionType).IsAssignableFrom(type) ||
                   typeof(InputObjectType).IsAssignableFrom(type);
            }

            private static bool IsStructuralType(IExtendedType type)
            {
                if (type.Definition is not null)
                {
                    return IsNullableType(type) ||
                       IsNonNullType(type) ||
                       IsListType(type);
                }
                return false;
            }

            private static bool IsNonNullType(IExtendedType type) =>
                type.IsGeneric &&
                typeof(NonNullType<>) == type.Definition;

            private static bool IsNullableType(IExtendedType type) =>
                type.IsGeneric &&
                typeof(Nullable) == type.Definition;

            private static bool IsOptional(IExtendedType type) =>
                type.IsGeneric &&
                typeof(Optional<>) == type.Definition;

            private static bool IsTaskType(IExtendedType type) =>
                type.IsGeneric &&
                (typeof(Task<>) == type.Definition ||
                 typeof(ValueTask<>) == type.Definition);

            private static bool IsListType(IExtendedType type) =>
                type.IsArray
                || (type.IsGeneric && type.Definition == typeof(ListType<>))
                || ImplementsListInterface(type);

            private static bool ImplementsListInterface(IExtendedType type)
            {
                return GetInnerListType(type) != null;
            }

            private static IExtendedType? GetInnerListType(IExtendedType type)
            {
                if (type.IsGeneric && type.Definition == typeof(ListType<>))
                {
                    return type.TypeArguments[0];
                }

                if (type.IsInterface && IsSupportedCollectionInterface(type, true))
                {
                    return type.TypeArguments[0];
                }

                foreach (IExtendedType interfaceType in type.GetInterfaces())
                {
                    if (IsSupportedCollectionInterface(interfaceType))
                    {
                        return interfaceType.TypeArguments[0];
                    }
                }

                return null;
            }

            private static bool IsSupportedCollectionInterface(IExtendedType type) =>
                IsSupportedCollectionInterface(type, false);

            private static bool IsSupportedCollectionInterface(
                IExtendedType type,
                bool allowEnumerable)
            {
                if (type.IsGeneric)
                {
                    Type? typeDefinition = type.Definition;
                    if (typeDefinition == typeof(IReadOnlyCollection<>)
                        || typeDefinition == typeof(IReadOnlyList<>)
                        || typeDefinition == typeof(ICollection<>)
                        || typeDefinition == typeof(IList<>)
                        || typeDefinition == typeof(IQueryable<>)
                        || typeDefinition == typeof(IAsyncEnumerable<>)
                        || typeDefinition == typeof(IObservable<>))
                    {
                        return true;
                    }

                    if (allowEnumerable && typeDefinition == typeof(IEnumerable<>))
                    {
                        return true;
                    }
                }
                return false;
            }

            private static IExtendedType? GetInnerType(IExtendedType type)
            {
                if (type.IsArray)
                {
                    return ExtendedType.FromType(type.Type.GetElementType()!);
                }

                if (IsTaskType(type)
                    || IsNonNullType(type)
                    || IsNullableType(type)
                    || IsWrapperType(type)
                    || IsOptional(type))
                {
                    return type.TypeArguments[0];
                }

                if (ImplementsListInterface(type))
                {
                    return GetInnerListType(type);
                }

                return null;
            }

            private static bool IsWrapperType(IExtendedType type) =>
                type.IsGeneric &&
                typeof(NativeType<>) == type.Definition;

            private static IExtendedType RemoveNonEssentialParts(IExtendedType type)
            {
                IExtendedType current = type;

                if (IsWrapperType(current))
                {
                    current = GetInnerType(current)!;
                }

                if (IsTaskType(current))
                {
                    current = GetInnerType(current)!;
                }

                if (IsOptional(type))
                {
                    current = GetInnerType(current)!;
                }

                return current;
            }

            private static bool IsTypeStackValid(List<IExtendedType> components)
            {
                if (components.Count == 0)
                {
                    return false;
                }

                foreach (IExtendedType type in components)
                {
                    if (typeof(Task).IsAssignableFrom(type))
                    {
                        return false;
                    }
                }

                if (typeof(IType).IsAssignableFrom(components[components.Count - 1]))
                {
                    return false;
                }

                return true;
            }
        }
    }
}
