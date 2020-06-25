using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Types;

namespace HotChocolate.Utilities
{
    internal class NamedTypeInfoFactory
        : ITypeInfoFactory
    {
        public bool TryCreate(Type type, out TypeInfo typeInfo)
        {
            if (CanHandle(type))
            {
                List<Type> components = DecomposeType(type);

                if (components.Count > 0 &&
                    TryCreateBluePrint(components, 0, out IType bluePrint) &&
                    TypeFactoryHelper.IsTypeStructureValid(bluePrint))
                {
                    typeInfo = new TypeInfo(
                        components.Last(),
                        components,
                        n => bluePrint.Rewrite(n));
                    return true;
                }
            }

            typeInfo = default;
            return false;
        }

        public bool TryExtractName(Type type, out NameString name)
        {
            if (TryCreate(type, out TypeInfo typeInfo))
            {
                ConstructorInfo constructor = typeInfo.ClrType.GetTypeInfo()
                    .DeclaredConstructors
                    .FirstOrDefault(t => !t.GetParameters().Any());

                if (constructor?.Invoke(Array.Empty<object>()) is IHasName nt)
                {
                    name = nt.Name;
                    return true;
                }
            }

            name = default;
            return false;
        }

        public bool TryExtractClrType(Type type, out Type clrType)
        {
            if (TryCreate(type, out TypeInfo typeInfo))
            {
                ConstructorInfo constructor = typeInfo.ClrType.GetTypeInfo()
                    .DeclaredConstructors
                    .FirstOrDefault(c => !c.GetParameters().Any());

                if (constructor?.Invoke(Array.Empty<object>()) is IHasRuntimeType t)
                {
                    clrType = t.RuntimeType;
                    return true;
                }
            }

            clrType = default;
            return false;
        }

        private static List<Type> DecomposeType(Type type)
        {
            var components = new List<Type>();
            Type current = type;

            do
            {
                components.Add(current);
                current = GetInnerType(current);
            } while (current != null && components.Count < 4);

            if (IsTypeStackValid(components))
            {
                return components;
            }
            return new List<Type>();
        }

        private static bool IsTypeStackValid(List<Type> components)
        {
            foreach (Type type in components)
            {
                if (!CanHandle(type))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool TryCreateBluePrint(
            List<Type> components,
            int index,
            out IType bluePrint)
        {
            if (index < components.Count)
            {
                Type component = components[index];

                if (index == components.Count - 1)
                {
                    if (IsNamedType(component))
                    {
                        bluePrint = TypeFactoryHelper.PlaceHolder;
                        return true;
                    }
                }
                else if (IsNonNullType(component))
                {
                    if (TryCreateBluePrint(components, index + 1, out IType innerType) &&
                        innerType.Kind != TypeKind.NonNull)
                    {
                        bluePrint = new NonNullType(innerType);
                        return true;
                    }
                }
                else if (IsListType(component))
                {
                    if (TryCreateBluePrint(components, index + 1, out IType innerType))
                    {
                        bluePrint = new ListType(innerType);
                        return true;
                    }
                }
            }

            bluePrint = null;
            return false;
        }

        private static Type GetInnerType(Type type)
        {
            if (typeof(INamedType).IsAssignableFrom(type))
            {
                return null;
            }

            if (type.IsGenericType)
            {
                return type.GetGenericArguments().First();
            }

            return null;
        }

        private static bool IsListType(Type type)
        {
            return type.IsGenericType
                && type.GetGenericTypeDefinition() == typeof(ListType<>);
        }

        private static bool IsNonNullType(Type type)
        {
            return type.IsGenericType
                && type.GetGenericTypeDefinition() == typeof(NonNullType<>);
        }

        private static bool IsNamedType(Type type)
        {
            return typeof(INamedType).IsAssignableFrom(type);
        }

        private static bool CanHandle(Type type)
        {
            return typeof(ScalarType).IsAssignableFrom(type)
                || typeof(ObjectType).IsAssignableFrom(type)
                || typeof(InterfaceType).IsAssignableFrom(type)
                || typeof(EnumType).IsAssignableFrom(type)
                || typeof(UnionType).IsAssignableFrom(type)
                || typeof(InputObjectType).IsAssignableFrom(type)
                || type.IsGenericType
                && (typeof(ListType<>) == type.GetGenericTypeDefinition()
                || typeof(NonNullType<>) == type.GetGenericTypeDefinition());
        }

        public static NamedTypeInfoFactory Default { get; } =
            new NamedTypeInfoFactory();
    }
}
