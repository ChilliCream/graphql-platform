using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Utilities;

namespace HotChocolate
{
    internal static class AttributeExtensions
    {
        public static string GetGraphQLName(this Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            TypeInfo typeInfo = type.GetTypeInfo();
            string name = typeInfo.IsDefined(
                typeof(GraphQLNameAttribute), false)
                ? typeInfo.GetCustomAttribute<GraphQLNameAttribute>().Name
                : GetFromType(typeInfo);
            return NameUtils.RemoveInvalidCharacters(name);
        }

        public static string GetGraphQLName(this PropertyInfo property)
        {
            string name = property.IsDefined(
                typeof(GraphQLNameAttribute), false)
                ? property.GetCustomAttribute<GraphQLNameAttribute>().Name
                : NormalizeName(property.Name);
            return NameUtils.RemoveInvalidCharacters(name);
        }

        public static string GetGraphQLName(this MethodInfo method)
        {
            string name = method.IsDefined(
                typeof(GraphQLNameAttribute), false)
                ? method.GetCustomAttribute<GraphQLNameAttribute>().Name
                : NormalizeMethodName(method);
            return NameUtils.RemoveInvalidCharacters(name);
        }

        public static string GetGraphQLName(this ParameterInfo parameter)
        {
            string name = parameter.IsDefined(
                typeof(GraphQLNameAttribute), false)
                ? parameter.GetCustomAttribute<GraphQLNameAttribute>().Name
                : NormalizeName(parameter.Name);
            return NameUtils.RemoveInvalidCharacters(name);
        }

        public static string GetGraphQLName(this MemberInfo member)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            if (member is MethodInfo m)
            {
                return GetGraphQLName(m);
            }

            if (member is PropertyInfo p)
            {
                return GetGraphQLName(p);
            }

            throw new NotSupportedException(
                "Only properties and methods are accepted as members.");
        }

        private static string NormalizeMethodName(MethodInfo method)
        {
            string name = method.Name;

            if (name.StartsWith("Get", StringComparison.Ordinal)
                && name.Length > 3)
            {
                name = name.Substring(3);
            }

            if (typeof(Task).IsAssignableFrom(method.ReturnType)
                && name.Length > 5
                && name.EndsWith("Async", StringComparison.Ordinal))
            {
                name = name.Substring(0, name.Length - 5);
            }

            return NormalizeName(name);
        }

        public static string GetGraphQLDescription(
            this ICustomAttributeProvider attributeProvider)
        {
            if (attributeProvider.IsDefined(
                typeof(GraphQLDescriptionAttribute),
                false))
            {
                var attribute = attributeProvider.GetCustomAttributes(
                    typeof(GraphQLDescriptionAttribute),
                    false)
                    .OfType<GraphQLDescriptionAttribute>()
                    .FirstOrDefault();
                return attribute.Description;
            }

            return null;
        }

        private static string GetFromType(TypeInfo typeInfo)
        {
            if (typeInfo.IsGenericType)
            {
                string name = typeInfo.Name
                    .Substring(0, typeInfo.Name.Length - 2);
                IEnumerable<string> arguments = typeInfo.GenericTypeArguments
                    .Select(t => GetFromType(t.GetTypeInfo()));
                return $"{name}Of{string.Join("And", arguments)}";
            }
            return typeInfo.Name;
        }

        private static string NormalizeName(string name)
        {
            if (name.Length > 1)
            {
                return name.Substring(0, 1).ToLowerInvariant() +
                    name.Substring(1);
            }
            return name.ToLowerInvariant();
        }
    }
}
