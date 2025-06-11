using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Utilities;
using static System.Reflection.BindingFlags;

namespace HotChocolate.Data.Filters.Expressions;

public static class FilterExpressionBuilder
{
    private static readonly ConcurrentDictionary<Type, Func<object?, Expression>> s_cachedDelegates = new();
    private static readonly ConcurrentDictionary<Type, Expression> s_cachedNullExpressions = new();
    private static readonly ConcurrentDictionary<Type, (MethodInfo, Func<object?, Expression>)> s_cachedEnumerableDelegates = new();

    private static readonly MethodInfo s_enumerableContains = typeof(Enumerable)
        .GetMethods(Public | Static)
        .Single(m => m.Name.Equals(nameof(Enumerable.Contains)) &&
            m.GetGenericArguments().Length == 1 &&
            m.GetParameters().Length is 2);

    private static readonly MethodInfo s_startsWith =
        ReflectionUtils.ExtractMethod<string>(x => x.StartsWith(null!));

    private static readonly MethodInfo s_endsWith =
        ReflectionUtils.ExtractMethod<string>(x => x.EndsWith(null!));

    private static readonly MethodInfo s_contains =
        ReflectionUtils.ExtractMethod<string>(x => x.Contains(null!));

    private static readonly MethodInfo s_createAndConvert =
        typeof(FilterExpressionBuilder)
            .GetMethod(nameof(CreateAndConvertParameter), NonPublic | Static)!;

    private static readonly MethodInfo s_anyMethod =
        typeof(Enumerable)
            .GetMethods()
            .Single(x => x.Name == nameof(Enumerable.Any) && x.GetParameters().Length == 1);

    private static readonly MethodInfo s_anyWithParameter =
        typeof(Enumerable)
            .GetMethods()
            .Single(x => x.Name == nameof(Enumerable.Any) && x.GetParameters().Length == 2);

    private static readonly MethodInfo s_allMethod =
        typeof(Enumerable)
            .GetMethods()
            .Single(x => x.Name == nameof(Enumerable.All) && x.GetParameters().Length == 2);

    private static readonly ConstantExpression s_null =
        Expression.Constant(null, typeof(object));

    private static readonly Expression s_true =
        CreateAndConvertParameter<bool>(true);

    private static readonly Expression s_false =
        CreateAndConvertParameter<bool>(false);

    private static readonly Expression s_nullableTrue =
        CreateAndConvertParameter<bool?>(true);

    private static readonly Expression s_nullableFalse =
        CreateAndConvertParameter<bool?>(false);

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
        var (methodInfo, expressionDelegate) = GetEnumerableDelegates(genericType);
        return Expression.Call(methodInfo, expressionDelegate(parsedValue), property);
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
            Expression.NotEqual(property, s_null),
            Expression.Call(
                property,
                s_startsWith,
                CreateParameter(value, property.Type)));

    public static Expression EndsWith(
        Expression property,
        object value)
        => Expression.AndAlso(
            Expression.NotEqual(property, s_null),
            Expression.Call(
                property,
                s_endsWith,
                CreateParameter(value, property.Type)));

    public static Expression Contains(
        Expression property,
        object value)
        => Expression.AndAlso(
            Expression.NotEqual(property, s_null),
            Expression.Call(
                property,
                s_contains,
                CreateParameter(value, property.Type)));

    public static Expression NotNull(Expression expression)
        => Expression.NotEqual(expression, s_null);

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
            s_anyWithParameter.MakeGenericMethod(type),
            property,
            lambda);

    public static Expression Any(
        Type type,
        Expression property)
    {
        return Expression.Call(
            s_anyMethod.MakeGenericMethod(type),
            property);
    }

    public static Expression All(
        Type type,
        Expression property,
        LambdaExpression lambda)
        => Expression.Call(
            s_allMethod.MakeGenericMethod(type),
            property,
            lambda);

    public static Expression NotContains(
        Expression property,
        object value)
        => Expression.OrElse(
            Expression.Equal(
                property,
                s_null),
            Expression.Not(Expression.Call(
                property,
                s_contains,
                CreateParameter(value, property.Type))));

    private static Expression CreateAndConvertParameter<T>(object value)
    {
        ExpressionParameter<T> parameter = new((T)value);
        return Expression.Property(Expression.Constant(parameter),
            nameof(parameter.p));
    }

    private static Expression CreateParameter(object? value, Type type)
    {
        if (value is null)
        {
            return CreateNullParameter(type);
        }

        if (value is bool boolean)
        {
            if (type == typeof(bool))
            {
                return boolean ? s_true : s_false;
            }

            return boolean ? s_nullableTrue : s_nullableFalse;
        }

        var expressionDelegate = s_cachedDelegates.GetOrAdd(type, static type =>
        {
            var methodInfo = s_createAndConvert.MakeGenericMethod(type);
            return methodInfo.CreateDelegate<Func<object?, Expression>>();
        });

        return expressionDelegate(value);
    }

    private static Expression CreateNullParameter(Type type)
    {
        return s_cachedNullExpressions.GetOrAdd(type, static type =>
        {
            var methodInfo = s_createAndConvert.MakeGenericMethod(type);
            return (Expression)methodInfo.Invoke(null, [null])!;
        });
    }

    private static (MethodInfo, Func<object?, Expression>) GetEnumerableDelegates(Type type)
    {
        return s_cachedEnumerableDelegates.GetOrAdd(type, static type =>
        {
            var methodInfo = s_enumerableContains.MakeGenericMethod(type);
            var expressionDelegate = s_createAndConvert
                .MakeGenericMethod(typeof(IEnumerable<>).MakeGenericType(type))
                .CreateDelegate<Func<object?, Expression>>();

            return (methodInfo, expressionDelegate);
        });
    }

    // ReSharper disable once InconsistentNaming
    // ReSharper disable once NotAccessedPositionalProperty.Local
    private readonly record struct ExpressionParameter<T>(T p);
}
