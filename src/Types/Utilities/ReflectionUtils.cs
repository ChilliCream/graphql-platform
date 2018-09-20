using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace HotChocolate.Utilities
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

            Expression unwrappedExpr = Unwrap(memberExpression);
            if (TryExtractMemberFromMemberExpression(
                    typeof(T), unwrappedExpr, out MemberInfo member)
                || TryExtractMemberFromMemberCallExpression(
                    typeof(T), unwrappedExpr, out member))
            {
                return member;
            }

            throw new ArgumentException(
                "The member expression must specify a property or method " +
                "that is public and that belongs to the " +
                $"type {typeof(T).FullName}",
                nameof(memberExpression));
        }

        private static bool TryExtractMemberFromMemberExpression(
            Type type,
            Expression memberExpression,
            out MemberInfo member)
        {
            if (memberExpression is MemberExpression m
                && m.Member.IsPublic())
            {
                if (m.Member is PropertyInfo pi
                    && pi.DeclaringType.IsAssignableFrom(type)
                    && !pi.IsSpecialName)
                {
                    member = GetBestMatchingProperty(type, pi);
                    return true;
                }
                else if (m.Member is MethodInfo mi
                    && mi.DeclaringType.IsAssignableFrom(type)
                    && !mi.IsSpecialName)
                {
                    member = GetBestMatchingMethod(type, mi);
                    return true;
                }
            }

            member = null;
            return false;
        }

        private static Expression Unwrap<T, TPropertyType>(
            Expression<Func<T, TPropertyType>> memberExpression)
        {
            if (memberExpression.Body is UnaryExpression u)
            {
                return u.Operand;
            }
            return memberExpression.Body;
        }

        private static bool TryExtractMemberFromMemberCallExpression(
            Type type,
            Expression memberExpression,
            out MemberInfo member)
        {
            if (memberExpression is MethodCallExpression mc
                && mc.Method.IsPublic()
                && mc.Method.DeclaringType.IsAssignableFrom(type)
                && !mc.Method.IsSpecialName)
            {
                member = GetBestMatchingMethod(type, mc.Method);
                return true;
            }

            member = null;
            return false;
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

            return type.IsGenericType
                ? CreateGenericTypeName(type)
                : CreateTypeName(type, type.Name);
        }

        private static string CreateGenericTypeName(Type type)
        {
            string name = type.Name.Substring(0, type.Name.Length - 2);
            IEnumerable<string> arguments = type.GetGenericArguments()
                .Select(GetTypeName);
            return CreateTypeName(type,
                $"{name}<{string.Join(", ", arguments)}>");
        }

        private static string CreateTypeName(Type type, string typeName)
        {
            string ns = GetNamespace(type);
            if (ns == null)
            {
                return typeName;
            }
            return $"{ns}.{typeName}";
        }

        private static string GetNamespace(Type type)
        {
            if (type.IsNested)
            {
                return $"{GetNamespace(type.DeclaringType)}.{type.DeclaringType.Name}";
            }
            return type.Namespace;
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
            var members = new Dictionary<string, PropertyInfo>(
                StringComparer.OrdinalIgnoreCase);

            foreach (PropertyInfo property in type.GetProperties())
            {
                members[property.GetGraphQLName()] = property;
            }

            return members;
        }

        public static Dictionary<string, MemberInfo> GetMembers(Type type)
        {
            var members = new Dictionary<string, MemberInfo>(
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

        private static MethodInfo GetBestMatchingMethod(
            Type type, MethodInfo method)
        {
            if (type.IsInterface || method.DeclaringType == type)
            {
                return method;
            }

            Type[] parameters = method.GetParameters()
                .Select(t => t.ParameterType).ToArray();
            Type current = type;

            while (current != typeof(object))
            {
                MethodInfo betterMatching = current
                    .GetMethod(method.Name, parameters);

                if (betterMatching != null)
                {
                    return betterMatching;
                }

                current = current.BaseType;
            }

            return method;
        }

        private static PropertyInfo GetBestMatchingProperty(
            Type type, PropertyInfo property)
        {
            if (type.IsInterface || property.DeclaringType == type)
            {
                return property;
            }

            Type current = type;

            while (current != typeof(object))
            {
                PropertyInfo betterMatching = current
                    .GetProperty(property.Name);

                if (betterMatching != null)
                {
                    return betterMatching;
                }

                current = current.BaseType;
            }

            return property;
        }
    }
}
