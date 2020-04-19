using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Types.Filters.Expressions
{
    public static class FilterExpressionBuilder
    {
        private static readonly MethodInfo _startsWith =
            typeof(string).GetMethods().Single(m =>
                m.Name.Equals(nameof(string.StartsWith))
                && m.GetParameters().Length == 1
                && m.GetParameters().Single().ParameterType == typeof(string));

        private static readonly MethodInfo _endsWith =
            typeof(string).GetMethods().Single(m =>
                m.Name.Equals(nameof(string.EndsWith))
                && m.GetParameters().Length == 1
                && m.GetParameters().Single().ParameterType == typeof(string));

        private static readonly MethodInfo _contains =
            typeof(string).GetMethods().Single(m =>
                m.Name.Equals(nameof(string.Contains))
                && m.GetParameters().Length == 1
                && m.GetParameters().Single().ParameterType == typeof(string));

        private static readonly MethodInfo _anyMethod =
            typeof(Enumerable)
                .GetMethods()
                .Single(x => x.Name == nameof(Enumerable.Any) && x.GetParameters().Length == 1);

        private static readonly MethodInfo _anyWithParameter =
            typeof(Enumerable)
                .GetMethods()
                .Single(x => x.Name == nameof(Enumerable.Any) && x.GetParameters().Length == 2);

        private static readonly MethodInfo _allMethod =
            typeof(Enumerable)
                .GetMethods()
                .Single(x => x.Name == nameof(Enumerable.All) && x.GetParameters().Length == 2);

        private static readonly MethodInfo _createAndConvert =
            typeof(FilterExpressionBuilder)
                .GetMethod(
                    nameof(FilterExpressionBuilder.CreateAndConvertParameter),
                    BindingFlags.NonPublic | BindingFlags.Static)!;

        private static readonly ConstantExpression _null =
            Expression.Constant(null, typeof(object));

        public static Expression Not(Expression expression)
        {
            return Expression.Not(expression);
        }

        public static Expression Equals(
            Expression property,
            object? value)
        {
            return Expression.Equal(
                property,
                CreateParameter(value, property.Type));
        }

        public static Expression NotEquals(
            Expression property,
            object value)
        {
            return Expression.NotEqual(
                property,
                CreateParameter(value, property.Type));
        }

        public static Expression In(
            Expression property,
            Type genericType,
            object parsedValue)
        {
            Type type = typeof(List<>).MakeGenericType(property.Type);

            return Expression.Call(
                typeof(Enumerable),
                nameof(Enumerable.Contains),
                new Type[] { genericType },
                CreateParameter(parsedValue, type),
                property);
        }

        public static Expression GreaterThan(
            Expression property,
            object value)
        {
            (Expression left, Expression right)
                = GetComparableValueWithConversions(property, value);

            return Expression.GreaterThan(left, right);
        }

        public static Expression GreaterThanOrEqual(
            Expression property,
            object value)
        {
            (Expression left, Expression right)
                = GetComparableValueWithConversions(property, value);

            return Expression.GreaterThanOrEqual(left, right);
        }

        public static Expression LowerThan(
            Expression property,
            object value)
        {
            (Expression left, Expression right)
                = GetComparableValueWithConversions(property, value);

            return Expression.LessThan(left, right);
        }

        public static Expression LowerThanOrEqual(
            Expression property,
            object value)
        {
            (Expression left, Expression right)
                = GetComparableValueWithConversions(property, value);

            return Expression.LessThanOrEqual(left, right);
        }

        private static (Expression left, Expression right) GetComparableValueWithConversions(
            Expression property,
            object value)
        {
            Expression left;
            Expression right;
            Type nonNullType = Nullable.GetUnderlyingType(property.Type) is Type underlyingType
                ? underlyingType
                : property.Type;

            if (nonNullType.IsEnum &&
                Enum.GetUnderlyingType(nonNullType) is Type enumType)
            {
                left = Expression.Convert(property, enumType);
                right = CreateParameter(value, enumType);
            }
            else
            {
                left = property;
                right = CreateParameter(value, property.Type);
            }

            return (left, right);
        }

        public static Expression StartsWith(
            Expression property,
            object value)
        {
            return Expression.AndAlso(
                Expression.NotEqual(property, _null),
                Expression.Call(property, _startsWith, CreateParameter(value, property.Type)));
        }

        public static Expression EndsWith(
            Expression property,
            object value)
        {
            return Expression.AndAlso(
                Expression.NotEqual(property, _null),
                Expression.Call(property, _endsWith, CreateParameter(value, property.Type)));
        }

        public static Expression Contains(
            Expression property,
            object value)
        {
            return Expression.AndAlso(
                Expression.NotEqual(property, _null),
                Expression.Call(property, _contains, CreateParameter(value, property.Type)));
        }

        public static Expression NotNull(Expression expression)
        {
            return Expression.NotEqual(expression, _null);
        }

        public static Expression IsNull(Expression expression)
        {
            return Expression.Equal(expression, _null);
        }

        public static Expression NotNullAndAlso(Expression property, Expression condition)
        {
            return Expression.AndAlso(NotNull(property), condition);
        }

        public static Expression IsNullOrElse(Expression property, Expression condition)
        {
            return Expression.OrElse(IsNull(property), condition);
        }

        private static Expression CreateAndConvertParameter<T>(object value)
        {
            Expression<Func<T>> lambda = () => (T)value;
            return lambda.Body;
        }

        private static Expression CreateParameter(object? value, Type type)
        {
            return (Expression)_createAndConvert
                .MakeGenericMethod(type).Invoke(null, new[] { value })!;
        }

        public static Expression Any(
            Type type,
            Expression property,
            Expression body,
            params ParameterExpression[] parameterExpression)
        {
            LambdaExpression lambda = Expression.Lambda(body, parameterExpression);
            return Any(type, property, lambda);
        }

        public static Expression Any(
            Type type,
            Expression property,
            LambdaExpression lambda)
        {
            return Expression.Call(
                _anyWithParameter.MakeGenericMethod(type),
                new Expression[] { property, lambda });
        }

        public static Expression Any(
            Type type,
            Expression property)
        {
            return Expression.Call(
                _anyMethod.MakeGenericMethod(type),
                new Expression[] { property });
        }

        public static Expression All(
            Type type,
            Expression property,
            LambdaExpression lambda)
        {
            return Expression.Call(
                _allMethod.MakeGenericMethod(type),
                new Expression[] { property, lambda });
        }

        public static Expression NotContains(
            Expression property,
            object value)
        {
            return Expression.OrElse(
                Expression.Equal(
                    property,
                    _null),
                Expression.Not(Expression.Call(
                    property,
                    _contains,
                    CreateParameter(value, property.Type))));
        }
    }
}
