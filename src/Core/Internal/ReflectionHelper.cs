using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Resolvers;

namespace HotChocolate
{
    internal static class ReflectionHelper
    {
        public static string GetTypeName(Type type)
        {
            if (type.IsDefined(typeof(GraphQLNameAttribute)))
            {
                GraphQLNameAttribute name = type.GetCustomAttribute<GraphQLNameAttribute>();
                return name.Name;
            }
            return type.Name;
        }

        public static IEnumerable<FieldResolverMember> GetMemberResolverInfos(Type type)
        {
            return GetProperties(type).Concat(GetMethods(type));
        }

        private static IEnumerable<FieldResolverMember> GetProperties(Type type)
        {
            PropertyInfo[] properties = type.GetProperties(
                BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo property in properties)
            {
                if (property.IsDefined(typeof(GraphQLNameAttribute)))
                {
                    GraphQLNameAttribute name = property.GetCustomAttribute<GraphQLNameAttribute>();
                    yield return new MemberResolverInfo(property, name.Name);
                }
                else
                {
                    yield return new MemberResolverInfo(property,
                        AdjustCasing(property.Name));
                }
            }
        }

        private static IEnumerable<FieldResolverMember> GetMethods(Type type)
        {
            MethodInfo[] methods = type.GetMethods(
                BindingFlags.Public | BindingFlags.Instance);

            foreach (MethodInfo method in methods)
            {
                if (method.IsDefined(typeof(GraphQLNameAttribute)))
                {
                    GraphQLNameAttribute name = method.GetCustomAttribute<GraphQLNameAttribute>();
                    yield return new MemberResolverInfo(method, name.Name);
                }
                else
                {
                    if (method.Name.StartsWith("Get"))
                    {
                        yield return new MemberResolverInfo(method,
                            AdjustCasing(method.Name.Substring(3)));
                    }
                    yield return new MemberResolverInfo(method,
                        AdjustCasing(method.Name));
                }
            }

        }

        public static string AdjustCasing(string name)
        {
            return name.Substring(0, 1).ToLowerInvariant() + name.Substring(1);
        }
    }
}
