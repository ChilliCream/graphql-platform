using System;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Configuration
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

            if (memberExpression.Body is MemberExpression m)
            {
                if (m.Member is PropertyInfo pi
                    && type == pi.ReflectedType)
                {
                    return pi;
                }
                else if (m.Member is MethodInfo mi
                    && type == mi.ReflectedType)
                {
                    return mi;
                }
            }

            if (memberExpression.Body is MethodCallExpression mc
                && type == mc.Method.ReflectedType)
            {
                return mc.Method;
            }

            throw new ArgumentException(
                "The specied expression does not refer to a property.",
                nameof(memberExpression));
        }
    }
}
