using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.CodeGeneration.Types;
using HotChocolate.Types;
using static HotChocolate.CodeGeneration.TypeNames;

namespace HotChocolate.CodeGeneration
{
    public static class SchemaExtensions
    {
        public static T? GetFirstDirective<T>(
            this IHasDirectives hasDirectives,
            string name,
            T? defaultValue = default)
        {
            var directive = hasDirectives.Directives[name].FirstOrDefault();

            if (directive is null)
            {
                return defaultValue;
            }

            return directive.ToObject<T>();
        }

        public static string GetPropertyName(this IObjectField field)
        {
            if (field.Name.Value.Length == 1)
            {
                return field.Name.Value.ToUpperInvariant();
            }

            return field.Name.Value.Substring(0, 1).ToUpperInvariant() +
                field.Name.Value.Substring(1);
        }

        public static string GetTypeName(this IObjectField field, string @namespace)
        {
            return CreateTypeName(field.Type, @namespace);
        }

        public static string CreateTypeName(IType type, string @namespace, bool nullable = true)
        {
            if (type.IsNonNullType())
            {
                return CreateTypeName(type.InnerType(), @namespace, false);
            }

            if (type.IsListType())
            {
                string elementType = CreateTypeName(type.ElementType(), @namespace);
                string listType = Generics(Global(List), elementType);

                if (nullable)
                {
                    return Nullable(listType);
                }

                return listType;
            }

            if (type.IsScalarType())
            {
                Type runtimeType = type.ToRuntimeType();

                if (runtimeType.IsGenericType &&
                    runtimeType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    runtimeType = runtimeType.GetGenericArguments()[0];
                }

                return Global(ToTypeName(runtimeType));
            }

            if (type is ObjectType objectType)
            {
                TypeNameDirective? typeNameDirective =
                    objectType.GetFirstDirective<TypeNameDirective>("typeName");
                string typeName = typeNameDirective?.Name ?? objectType.Name.Value;
                return Global(@namespace + "." + typeName);
            }

            throw new NotSupportedException();
        }

        private static string ToTypeName(this Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return type.IsGenericType
                ? CreateGenericTypeName(type)
                : CreateTypeName(type, type.Name);
        }

        private static string CreateGenericTypeName(Type type)
        {
            string name = type.Name.Substring(0, type.Name.Length - 2);
            IEnumerable<string> arguments = type.GetGenericArguments().Select(ToTypeName);
            return CreateTypeName(type, $"{name}<{string.Join(", ", arguments)}>");
        }

        private static string CreateTypeName(Type type, string typeName)
        {
            var ns = GetNamespace(type);

            if (ns is null)
            {
                return typeName;
            }

            return $"{ns}.{typeName}";
        }

        private static string? GetNamespace(Type type)
        {
            if (type.IsNested)
            {
                return $"{GetNamespace(type.DeclaringType!)}.{type.DeclaringType!.Name}";
            }
            return type.Namespace;
        }
    }
}
