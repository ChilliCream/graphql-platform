using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HotChocolate.Utilities
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
                return member.GetCustomAttribute<GraphQLNameAttribute>().Name;
            }

            if (member is MethodInfo)
            {
                if (member.Name.StartsWith("Get", StringComparison.Ordinal)
                    && member.Name.Length > 3)
                {
                    return NormalizeName(member.Name.Substring(3));
                }
                return NormalizeName(member.Name);
            }

            if (member is PropertyInfo)
            {
                return NormalizeName(member.Name);
            }

            if (member is Type t)
            {
                return GetFromType(t);
            }

            return member.Name;
        }

        public static string GetGraphQLName(this ParameterInfo parameter)
        {
            if (parameter.IsDefined(typeof(GraphQLNameAttribute), false))
            {
                return parameter.GetCustomAttribute<GraphQLNameAttribute>().Name;
            }
            return NormalizeName(parameter.Name);
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
