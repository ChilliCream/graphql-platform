using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Utilities
{
    internal class SchemaTypeInfoFactory
        : ITypeInfoFactory
    {
        public bool TryCreate(IExtendedType type, out TypeInfo? typeInfo)
        {
            if (type.Kind == ExtendedTypeKind.Schema)
            {
                List<TypeComponent> components = DecomposeType(type);

                if (components.Count > 0)
                {
                    typeInfo = new TypeInfo(
                        components[components.Count - 1].Type,
                        components);
                    return true;
                }
            }

            typeInfo = default;
            return false;
        }

        public bool TryExtractName(IExtendedType type, out NameString? name)
        {
            if (TryCreate(type, out TypeInfo? typeInfo))
            {
                ConstructorInfo constructor = typeInfo!.Type.GetTypeInfo()
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

        public bool TryExtractClrType(IExtendedType type, out Type? clrType)
        {
            if (TryCreate(type, out TypeInfo? typeInfo))
            {
                ConstructorInfo constructor = typeInfo!.Type.GetTypeInfo()
                    .DeclaredConstructors
                    .FirstOrDefault(c => !c.GetParameters().Any());

                if (constructor?.Invoke(Array.Empty<object>()) is IHasClrType t)
                {
                    clrType = t.ClrType;
                    return true;
                }
            }

            clrType = default;
            return false;
        }

        private static List<TypeComponent> DecomposeType(IExtendedType type)
        {
            var components = new List<TypeComponent>();
            IExtendedType? current = type;

            do
            {
                if (!current.IsNullable)
                {
                    components.Add(TypeComponent.NonNull);
                }

                if (current.IsGeneric && current.Definition == typeof(ListType<>))
                {
                    components.Add(TypeComponent.List);
                }
                else
                {
                    components.Add(new TypeComponent(
                        TypeComponentKind.Named,
                        current.Type));
                    current = null;
                }


            } while (current != null && components.Count < 7);

            if (IsTypeStackValid(components))
            {
                return components;
            }
            return new List<TypeComponent>();
        }

        private static bool IsTypeStackValid(List<TypeComponent> components)
        {
            if (components.Count > 0)
            {
                TypeComponent namedType = components[components.Count - 1];
                if (typeof(INamedType).IsAssignableFrom(namedType.Type))
                {
                    return true;
                }
            }
            return false;
        }

        public static SchemaTypeInfoFactory Default { get; } =
            new SchemaTypeInfoFactory();
    }
}
