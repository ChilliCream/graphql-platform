using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Properties;

namespace HotChocolate.Utilities;

public static class ReflectionUtils
{
    public static MemberInfo TryExtractMember<T, TPropertyType>(
        this Expression<Func<T, TPropertyType>> memberExpression,
        bool allowStatic = false)
    {
        if (memberExpression == null)
        {
            throw new ArgumentNullException(nameof(memberExpression));
        }

        return TryExtractMemberInternal<T>(UnwrapFunc(memberExpression), allowStatic);
    }

    internal static MemberInfo TryExtractCallMember(
        this Expression expression)
    {
        if (expression is LambdaExpression lambda)
        {
            if (lambda.Body is MethodCallExpression m)
            {
                return m.Method;
            }

            if (lambda.Body is MemberExpression p)
            {
                return p.Member;
            }
        }

        return null;
    }

    private static MemberInfo TryExtractMemberInternal<T>(
        Expression expression,
        bool allowStatic)
        => ExtractMember(typeof(T), expression, allowStatic);

    public static MethodInfo ExtractMethod<T>(
        this Expression<Action<T>> memberExpression,
        bool allowStatic = false)
        => ExtractMember(memberExpression, allowStatic) as MethodInfo ??
           throw new ArgumentException(
               TypeResources.ReflectionUtils_ExtractMethod_MethodExpected,
               nameof(memberExpression));

    public static MemberInfo ExtractMember<T>(
        this Expression<Action<T>> memberExpression,
        bool allowStatic = false)
    {
        if (memberExpression is null)
        {
            throw new ArgumentNullException(nameof(memberExpression));
        }

        return ExtractMemberInternal<T>(UnwrapAction(memberExpression), allowStatic);
    }

    public static MemberInfo ExtractMember<T, TPropertyType>(
        this Expression<Func<T, TPropertyType>> memberExpression,
        bool allowStatic = false)
    {
        if (memberExpression is null)
        {
            throw new ArgumentNullException(nameof(memberExpression));
        }

        return ExtractMemberInternal<T>(UnwrapFunc(memberExpression), allowStatic);
    }

    private static MemberInfo ExtractMemberInternal<T>(
        Expression expression,
        bool allowStatic)
    {
        var member = ExtractMember(typeof(T), expression, allowStatic);

        if (member is null)
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

    private static Expression UnwrapAction<T>(
        Expression<Action<T>> memberExpression)
    {
        if (memberExpression.Body is UnaryExpression u)
        {
            return u.Operand;
        }
        return memberExpression.Body;
    }

    private static bool TryExtractMemberFromMemberExpression(
        Type type,
        Expression memberExpression,
        bool allowStatic,
        out MemberInfo member)
    {
        if (memberExpression is MemberExpression m)
        {
            if (m.Member is PropertyInfo pi
                && (pi.DeclaringType?.IsAssignableFrom(type) ?? false)
                && !pi.IsSpecialName)
            {
                member = GetBestMatchingProperty(type, pi);
                return true;
            }

            if (m.Member is MethodInfo mi &&
                (IsInstanceMethod(type, mi) || allowStatic && IsStaticMethod(mi)))
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
        Type type,
        Expression unwrappedExpr,
        bool allowStatic)
    {
        if (TryExtractMemberFromMemberExpression(
                type,
                unwrappedExpr,
                allowStatic,
                out var member) ||
            TryExtractMemberFromMemberCallExpression(
                type,
                unwrappedExpr,
                allowStatic,
                out member))
        {
            return member;
        }

        return null;
    }

    private static bool TryExtractMemberFromMemberCallExpression(
        Type type,
        Expression memberExpression,
        bool allowStatic,
        out MemberInfo member)
    {
        if (memberExpression is MethodCallExpression mc &&
            (IsInstanceMethod(type, mc.Method) || allowStatic && IsStaticMethod(mc.Method)))
        {
            member = GetBestMatchingMethod(type, mc.Method);
            return true;
        }

        member = null;
        return false;
    }

    private static bool IsInstanceMethod(Type type, MethodInfo method)
        => (method.DeclaringType?.IsAssignableFrom(type) ?? false) && !method.IsSpecialName;

    private static bool IsStaticMethod(MethodInfo method)
        => method.IsStatic;

    public static string GetTypeName(this Type type)
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
        var arguments = type.GetGenericArguments().Select(GetTypeName);
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

    private static string GetNamespace(Type type)
    {
        if (type.IsNested)
        {
            return $"{GetNamespace(type.DeclaringType)}.{type.DeclaringType!.Name}";
        }
        return type.Namespace;
    }

    public static Type GetReturnType(this MemberInfo member)
    {
        if (member.IsDefined(typeof(GraphQLTypeAttribute)))
        {
            return member.GetCustomAttribute<GraphQLTypeAttribute>()!.Type;
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
        foreach (var property in type.GetProperties(
            BindingFlags.Instance | BindingFlags.Public)
            .Where(p => !IsIgnored(p)
                && p.CanRead
                && p.DeclaringType != typeof(object)))
        {
            var name = property.GetGraphQLName();
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

        var parameters = method.GetParameters()
            .Select(t => t.ParameterType).ToArray();
        var current = type;

        while (current is not null && current != typeof(object))
        {
            var betterMatching = current.GetMethod(method.Name, parameters);

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

        var current = type;

        while (current is not null && current != typeof(object))
        {
            var betterMatching = current.GetProperty(property.Name);

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
