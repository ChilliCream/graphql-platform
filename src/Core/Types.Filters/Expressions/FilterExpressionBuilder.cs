using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Types.Filters.Expressions
{
    public static class FilterExpressionBuilder
    {

        private static readonly MethodInfo _startsWith =
            typeof(string).GetMethods().Single(m =>
                m.Name.Equals("StartsWith")
                && m.GetParameters().Length == 1
                && m.GetParameters().Single().ParameterType == typeof(string));

        private static readonly MethodInfo _endsWith =
            typeof(string).GetMethods().Single(m =>
                m.Name.Equals("EndsWith")
                && m.GetParameters().Length == 1
                && m.GetParameters().Single().ParameterType == typeof(string));


        private static readonly MethodInfo _contains =
            typeof(string).GetMethods().Single(m =>
                m.Name.Equals("Contains")
                && m.GetParameters().Length == 1
                && m.GetParameters().Single().ParameterType == typeof(string));

        public static Expression Not(Expression expression)
        {
            return Expression.Equal(expression, Expression.Constant(false));
        }

        public static Expression Equals(
            MemberExpression property,
            object value)
        {
            return Expression.Equal(property, Expression.Constant(value));
        }

        public static Expression NotEquals(
            MemberExpression property,
            object value)
        {
            return Expression.NotEqual(property, Expression.Constant(value));
        }

        public static Expression In(
            MemberExpression property,
            Type genericType,
            object parsedValue)
        {
            return Expression.Call(
                    typeof(Enumerable),
                    "Contains",
                    new Type[] { genericType },
                    Expression.Constant(parsedValue),
                    property
                );
        }

        public static Expression GreaterThan(
            MemberExpression property,
            object value)
        {
            return Expression.GreaterThan(
                property,
                Expression.Constant(value));
        }

        public static Expression GreaterThanOrEqual(
            MemberExpression property,
            object value)
        {
            return Expression.GreaterThanOrEqual(
                property,
                Expression.Constant(value));
        }

        public static Expression LowerThan(
            MemberExpression property,
            object value)
        {
            return Expression.LessThan(
                property,
                Expression.Constant(value));
        }

        public static Expression LowerThanOrEqual(
            MemberExpression property,
            object value)
        {
            return Expression.LessThanOrEqual(
                property,
                Expression.Constant(value));
        }

        public static Expression StartsWith(
            MemberExpression property,
            object value)
        {
            return Expression.AndAlso(
                Expression.NotEqual(property, Expression.Constant(null)),
                Expression.Call(property, _startsWith,
                    new[] { Expression.Constant(value) }));
        }

        public static Expression EndsWith(
            MemberExpression property,
            object value)
        {
            return Expression.AndAlso(
                Expression.NotEqual(property, Expression.Constant(null)),
                Expression.Call(property, _endsWith,
                    new[] { Expression.Constant(value) }));
        }
        public static Expression Contains(
            MemberExpression property,
            object value)
        {
            return Expression.AndAlso(
                Expression.NotEqual(property, Expression.Constant(null)),
                Expression.Call(property, _contains,
                    new[] { Expression.Constant(value) }));
        }
    }
}
