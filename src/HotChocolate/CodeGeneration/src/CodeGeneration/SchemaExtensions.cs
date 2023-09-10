using System;
using System.Linq;
using HotChocolate.CodeGeneration.Types;
using HotChocolate.Types;
using static HotChocolate.CodeGeneration.TypeNames;

namespace HotChocolate.CodeGeneration;

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

        return directive.AsValue<T>();
    }

    public static string GetPropertyName(this IObjectField field)
    {
        if (field.Name.Length == 1)
        {
            return field.Name.ToUpperInvariant();
        }

        return field.Name.Substring(0, 1).ToUpperInvariant() +
            field.Name.Substring(1);
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
            var elementType = CreateTypeName(type.ElementType(), @namespace);
            var listType = Generics(Global(List), elementType);

            if (nullable)
            {
                return Nullable(listType);
            }

            return listType;
        }

        if (type.IsScalarType())
        {
            var runtimeType = type.ToRuntimeType();

            if (runtimeType.IsGenericType &&
                runtimeType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                runtimeType = runtimeType.GetGenericArguments()[0];
            }

            return Global(ToTypeName(runtimeType));
        }

        if (type is ObjectType objectType)
        {
            var typeNameDirective =
                objectType.GetFirstDirective<TypeNameDirective>("typeName");
            var typeName = typeNameDirective?.Name ?? objectType.Name;
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
        var name = type.Name.Substring(0, type.Name.Length - 2);
        var arguments = type.GetGenericArguments().Select(ToTypeName);
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
