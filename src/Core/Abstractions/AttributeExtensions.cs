using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Utilities;

namespace HotChocolate
{
    internal static class AttributeExtensions
    {
        public static string GetGraphQLName(this MemberInfo member)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            if (member.IsDefined(typeof(GraphQLNameAttribute), false))
            {
                return NameUtils.RemoveInvalidCharacters(
                    member.GetCustomAttribute<GraphQLNameAttribute>().Name);
            }

            if (member is MethodInfo m)
            {
                return GetGraphQLName(m);
            }

            if (member is PropertyInfo p)
            {
                return GetGraphQLName(p);
            }

            if (member is Type t)
            {
                return NameUtils.RemoveInvalidCharacters(GetFromType(t));
            }

            return NameUtils.RemoveInvalidCharacters(member.Name);
        }

        public static string GetGraphQLName(this ParameterInfo parameter)
        {
            if (parameter.IsDefined(typeof(GraphQLNameAttribute), false))
            {
                return parameter.GetCustomAttribute<GraphQLNameAttribute>().Name;
            }
            return NormalizeName(parameter.Name);
        }

        public static string GetGraphQLName(this MethodInfo method)
        {
            string name;

            if (method.Name.StartsWith("Get", StringComparison.Ordinal)
                && method.Name.Length > 3)
            {
                name = NormalizeName(method.Name.Substring(3));
            }
            else
            {
                name = NormalizeName(method.Name);
            }

            return NameUtils.RemoveInvalidCharacters(name);
        }

        public static string GetGraphQLName(this PropertyInfo property)
        {
            return NameUtils.RemoveInvalidCharacters(
                NormalizeName(property.Name));
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

        private static string GetFromType(Type type)
        {
            if (type.IsGenericType)
            {
                string name = type.Name.Substring(0, type.Name.Length - 2);
                IEnumerable<string> arguments = type.GetGenericArguments()
                    .Select(GetFromType);
                return $"{name}Of{string.Join("And", arguments)}";
            }
            return type.Name;
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
