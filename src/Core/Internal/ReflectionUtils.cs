using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Types;

namespace HotChocolate.Internal
{
    internal static class ReflectionUtils
    {
        public static MemberInfo ExtractMember<T, TPropertyType>(
            this Expression<Func<T, TPropertyType>> memberExpression)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException(nameof(memberExpression));
            }

            Type type = typeof(T);

            if (memberExpression.Body is MemberExpression m && m.Member.IsPublic())
            {
                if (m.Member is PropertyInfo pi
                    && pi.DeclaringType.IsAssignableFrom(type)
                    && !pi.IsSpecialName)
                {
                    return pi;
                }
                else if (m.Member is MethodInfo mi
                    && mi.DeclaringType.IsAssignableFrom(type)
                    && !mi.IsSpecialName)
                {
                    return mi;
                }
            }

            if (memberExpression.Body is MethodCallExpression mc
                && mc.Method.IsPublic()
                && mc.Method.DeclaringType.IsAssignableFrom(type)
                && !mc.Method.IsSpecialName)
            {
                return mc.Method;
            }

            throw new ArgumentException(
                "The member expression must specify a property or method " +
                "that is public and that belongs to the " +
                $"type {typeof(T).FullName}",
                nameof(memberExpression));
        }

        private static bool IsPublic(this MemberInfo member)
        {
            if (member is PropertyInfo p)
            {
                return p.GetGetMethod()?.IsPublic ?? false;
            }

            if (member is MethodInfo m)
            {
                return m.IsPublic;
            }

            return false;
        }


        internal static string GetTypeName(this Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            string name = type.FullName ?? type.Name;

            if (type.IsGenericType)
            {
                name = CreateGenericName(type);
            }

            return name.Replace("+", ".");
        }

        private static string CreateGenericName(Type type)
        {
            string name = type.Name.Substring(0, type.Name.Length - 2);
            IEnumerable<string> arguments = type.GetGenericArguments()
                .Select(GetTypeName);
            return $"{name}<{string.Join(", ", arguments)}>";
        }

        public static bool IsNativeTypeWrapper<T>()
        {
            Type type = typeof(T);
            if (type.IsGenericType)
            {
                return (typeof(NativeType<>) == typeof(T).GetGenericTypeDefinition());
            }
            return false;
        }

        public static bool IsNativeTypeWrapper(Type type)
        {
            return (typeof(NativeType<>) == type.GetGenericTypeDefinition());
        }

        public static Type GetReturnType(this MemberInfo member)
        {
            if (member is PropertyInfo p)
            {
                return p.PropertyType;
            }

            if (member is MethodInfo m
                && (m.ReturnType != typeof(void)
                    || m.ReturnType != typeof(Task)))
            {
                return m.ReturnType;
            }

            return null;
        }

        public static Dictionary<string, PropertyInfo> GetProperties(Type type)
        {
            Dictionary<string, PropertyInfo> members =
                new Dictionary<string, PropertyInfo>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (PropertyInfo property in type.GetProperties())
            {
                members[property.GetGraphQLName()] = property;
            }

            return members;
        }

        public static Dictionary<string, MemberInfo> GetMembers(Type type)
        {
            Dictionary<string, MemberInfo> members =
                new Dictionary<string, MemberInfo>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (PropertyInfo property in type.GetProperties())
            {
                members[property.GetGraphQLName()] = property;
            }

            foreach (MethodInfo method in type.GetMethods())
            {
                members[method.GetGraphQLName()] = method;
                if (method.Name.Length > 3 && method.Name
                    .StartsWith("Get", StringComparison.OrdinalIgnoreCase))
                {
                    members[method.Name.Substring(3)] = method;
                }
            }

            return members;
        }
    }
}
