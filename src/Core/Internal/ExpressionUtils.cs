using System;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Internal
{
    internal static class ExpressionUtils
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
    }
}
