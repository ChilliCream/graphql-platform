using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Properties;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Utilities
{
    public static class ReflectionUtils
    {
        public static MemberInfo ExtractMember<T, TPropertyType>(
            this Expression<Func<T, TPropertyType>> memberExpression)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException(nameof(memberExpression));
            }

            return ExtractMemberInternal<T>(UnwrapFunc(memberExpression));
        }

        private static MemberInfo ExtractMemberInternal<T>(
            Expression expression)
        {
            MemberInfo member = ExtractMember(typeof(T), expression);

            if (member == null)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        TypeResources.Reflection_MemberMust_BeMethodOrProperty,
                        typeof(T).FullName),
                    nameof(expression));
            }

            return member;
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

        private static Expression UnwrapFunc<T, TPropertyType>(
            Expression<Func<T, TPropertyType>> memberExpression)
        {
            if (memberExpression.Body is UnaryExpression u)
            {
                return u.Operand;
            }
            return memberExpression.Body;
        }

        private static MemberInfo ExtractMember(
            Type type, Expression unwrappedExpr)
        {
            if (TryExtractMemberFromMemberExpression(
                    type, unwrappedExpr, out MemberInfo member)
                || TryExtractMemberFromMemberCallExpression(
                    type, unwrappedExpr, out member))
            {
                return member;
            }

            return null;
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

        public static string GetTypeName(this Type type)
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

        public static ITypeReference GetOutputType(this MemberInfo member) =>
            member.GetTypeReference(TypeContext.Output);

        public static ITypeReference GetInputType(this MemberInfo member) =>
            member.GetTypeReference(TypeContext.Input);

        private static ITypeReference GetTypeReference(
            this MemberInfo member,
            TypeContext context)
        {
            Type type = GetReturnType(member);

            if (type != null)
            {
                return new ClrTypeReference(type, context);
            }

            return null;
        }

        public static Type GetReturnType(this MemberInfo member)
        {
            if (member.IsDefined(typeof(GraphQLTypeAttribute)))
            {
                return member.GetCustomAttribute<GraphQLTypeAttribute>().Type;
            }

            if (member is Type t)
            {
                return t;
            }

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

            AddProperties(
                members.ContainsKey,
                (n, p) => members[n] = p,
                type);

            return members;
        }

        private static void AddProperties(
            Func<string, bool> exists,
            Action<string, PropertyInfo> add,
            Type type)
        {
            foreach (PropertyInfo property in type.GetProperties(
                BindingFlags.Instance | BindingFlags.Public)
                .Where(p => !IsIgnored(p)
                    && p.CanRead
                    && p.DeclaringType != typeof(object)))
            {
                string name = property.GetGraphQLName();
                if (!exists(name))
                {
                    add(name, property);
                }
            }
        }

        private static bool IsIgnored(MemberInfo member)
        {
            return member.IsDefined(typeof(GraphQLIgnoreAttribute));
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

        public static ILookup<string, PropertyInfo> CreatePropertyLookup(
            this Type type)
        {
            return type.GetProperties().ToLookup(
                t => t.Name,
                StringComparer.OrdinalIgnoreCase);
        }
    }
}
