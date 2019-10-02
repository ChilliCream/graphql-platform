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

        private static readonly MethodInfo _anyMethod = typeof(Enumerable)
                    .GetMethods()
                    .Single(x => x.Name == "Any" && x.GetParameters().Length == 1);

        private static readonly MethodInfo _anyWithParameter = typeof(Enumerable)
                        .GetMethods()
                        .Single(x => x.Name == "Any" && x.GetParameters().Length == 2);

        private static readonly MethodInfo _allMethod = typeof(Enumerable)
                        .GetMethods()
                        .Single(x => x.Name == "All" && x.GetParameters().Length == 2);

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

        public static Expression Any(
            Type type,
            Expression property,
            Expression body,
            params ParameterExpression[] parameterExpression)
        {
            var lambda = Expression.Lambda(body, parameterExpression);
            return Any(type, property, lambda);
        }

        public static Expression Any(
            Type type,
            Expression property,
            LambdaExpression lambda)
        {
            return Expression.Call(_anyWithParameter.MakeGenericMethod(type), new Expression[] { property, lambda });
        }

        public static Expression Any(
            Type type,
            Expression property)
        {
            return Expression.Call(_anyMethod.MakeGenericMethod(type), new Expression[] { property });
        }

        public static Expression All(
            Type type,
            Expression property,
            LambdaExpression lambda)
        {
            return Expression.Call(_allMethod.MakeGenericMethod(type), new Expression[] { property, lambda });
        }


    }
}
