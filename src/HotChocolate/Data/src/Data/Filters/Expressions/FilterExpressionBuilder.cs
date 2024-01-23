using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Utilities;
using static System.Reflection.BindingFlags;

namespace HotChocolate.Data.Filters.Expressions;

public static class FilterExpressionBuilder
{
#pragma warning disable CA1307
    private static readonly MethodInfo _startsWith =
        ReflectionUtils.ExtractMethod<string>(x => x.StartsWith(default(string)!));

    private static readonly MethodInfo _endsWith =
        ReflectionUtils.ExtractMethod<string>(x => x.EndsWith(default(string)!));

    private static readonly MethodInfo _contains =
        ReflectionUtils.ExtractMethod<string>(x => x.Contains(default(string)!));
#pragma warning restore CA1307

    private static readonly MethodInfo _createAndConvert =
        typeof(FilterExpressionBuilder)
            .GetMethod(nameof(CreateAndConvertParameter), NonPublic | Static)!;

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

    private static readonly ConstantExpression _null =
        Expression.Constant(null, typeof(object));

    public static Expression Not(Expression expression)
        => Expression.Not(expression);

    public static Expression Equals(
        Expression property,
        object? value)
        => Expression.Equal(property, CreateParameter(value, property.Type));

    public static Expression NotEquals(
        Expression property,
        object? value)
        => Expression.NotEqual(property, CreateParameter(value, property.Type));

    public static Expression In(
        Expression property,
        Type genericType,
        object? parsedValue)
    {
        var enumerableType = typeof(IEnumerable<>);
        var enumerableGenericType = enumerableType.MakeGenericType(genericType);

        return Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Contains),
            [genericType,],
            CreateParameter(parsedValue, enumerableGenericType),
            property);
    }

    public static Expression GreaterThan(
        Expression property,
        object value)
        => Expression.GreaterThan(property, CreateParameter(value, property.Type));

    public static Expression GreaterThanOrEqual(
        Expression property,
        object value)
        => Expression.GreaterThanOrEqual(property, CreateParameter(value, property.Type));

    public static Expression LowerThan(
        Expression property,
        object value)
        => Expression.LessThan(property, CreateParameter(value, property.Type));

    public static Expression LowerThanOrEqual(
        Expression property,
        object value)
        => Expression.LessThanOrEqual(property, CreateParameter(value, property.Type));

    public static Expression StartsWith(
        Expression property,
        object value)
        => Expression.AndAlso(
            Expression.NotEqual(property, _null),
            Expression.Call(
                property,
                _startsWith,
                CreateParameter(value, property.Type)));

    public static Expression EndsWith(
        Expression property,
        object value)
        => Expression.AndAlso(
            Expression.NotEqual(property, _null),
            Expression.Call(
                property,
                _endsWith,
                CreateParameter(value, property.Type)));

    public static Expression Contains(
        Expression property,
        object value)
        => Expression.AndAlso(
            Expression.NotEqual(property, _null),
            Expression.Call(
                property,
                _contains,
                CreateParameter(value, property.Type)));

    public static Expression NotNull(Expression expression)
        => Expression.NotEqual(expression, _null);

    public static Expression HasValue(Expression expression)
        => Expression.IsTrue(
            Expression.Property(
                expression,
                expression.Type.GetProperty(nameof(Nullable<int>.HasValue))!));

    public static Expression NotNullAndAlso(Expression property, Expression condition)
        => Expression.AndAlso(NotNull(property), condition);

    public static Expression HasValueAndAlso(Expression property, Expression condition)
        => Expression.AndAlso(HasValue(property), condition);

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
        => Expression.Call(
            _anyWithParameter.MakeGenericMethod(type),
            [property, lambda,]);

    public static Expression Any(
        Type type,
        Expression property)
    {
        return Expression.Call(
            _anyMethod.MakeGenericMethod(type),
            [property,]);
    }

    public static Expression All(
        Type type,
        Expression property,
        LambdaExpression lambda)
        => Expression.Call(
            _allMethod.MakeGenericMethod(type),
            [property, lambda,]);

    public static Expression NotContains(
        Expression property,
        object value)
        => Expression.OrElse(
            Expression.Equal(
                property,
                _null),
            Expression.Not(Expression.Call(
                property,
                _contains,
                CreateParameter(value, property.Type))));

    private static Expression CreateAndConvertParameter<T>(object value)
    {
        Expression<Func<T>> lambda = () => (T)value;

        return lambda.Body;
    }

    private static Expression CreateParameter(object? value, Type type)
        => (Expression)_createAndConvert
            .MakeGenericMethod(type)
            .Invoke(null,
                [value,])!;
}
