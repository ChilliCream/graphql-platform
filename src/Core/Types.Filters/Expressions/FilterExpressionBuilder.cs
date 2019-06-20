using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace HotChocolate.Types.Filters.Expressions
{
    internal static class FilterExpressionBuilder
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

        public static Expression CreateEqualExpression(MemberExpression property, object value)
        {
            return Expression.Equal(property, Expression.Constant(value));
        }

        public static Expression CreateInExpression(MemberExpression property, Type genericType, object parsedValue)
        {
            return Expression.Call(
                    typeof(Enumerable),
                    "Contains",
                    new Type[] { genericType },
                    Expression.Constant(parsedValue),
                    property
                );
        }

        public static Expression CreateEqualsExpression(MemberExpression property, object value)
        {
            return Expression.Equal(property, Expression.Constant(value));
        }

        public static Expression CreateGreaterThanExpression(MemberExpression property, object value)
        {
            return Expression.GreaterThan(property, Expression.Constant(value));
        }

        public static Expression CreateGreaterThanOrEqualExpression(MemberExpression property, object value)
        {
            return Expression.GreaterThanOrEqual(property, Expression.Constant(value));
        }

        public static Expression CreateLowerThanExpression(MemberExpression property, object value)
        {
            return Expression.LessThan(property, Expression.Constant(value));
        }

        public static Expression CreateLowerThanOrEqualExpression(MemberExpression property, object value)
        {
            return Expression.LessThanOrEqual(property, Expression.Constant(value));
        }

        public static Expression CreateStartsWithExpression(MemberExpression property, object value)
        {
            return Expression.Call(property, _startsWith, new[] { Expression.Constant(value) });
        }

        public static Expression CreateEndsWithExpression(MemberExpression property, object value)
        {
            return Expression.Call(property, _endsWith, new[] { Expression.Constant(value) });
        }
        public static Expression CreateContainsExpression(MemberExpression property, object value)
        {
            return Expression.Call(property, _contains, new[] { Expression.Constant(value) });
        }
    }
}
