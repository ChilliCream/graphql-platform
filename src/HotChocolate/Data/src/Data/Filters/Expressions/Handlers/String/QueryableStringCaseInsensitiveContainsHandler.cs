using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters.Expressions;

public class QueryableStringCaseInsensitiveContainsHandler : QueryableStringOperationHandler
{
    private static readonly ConstantExpression _sNull =
        Expression.Constant(null, typeof(object));

    private static readonly MethodInfo _sContainsInvariant =
        typeof(string).GetMethods()
            .Single(m =>
                m.Name.Equals(nameof(string.Contains), StringComparison.Ordinal) &&
                m.GetParameters().Length == 2 &&
                m.GetParameters().First().ParameterType == typeof(string) &&
                m.GetParameters().Skip(1).First().ParameterType == typeof(StringComparison));

    private static readonly MethodInfo _sCreateAndConvert =
        typeof(FilterExpressionBuilder)
            .GetMethod(
                nameof(CreateAndConvertParameter),
                BindingFlags.NonPublic | BindingFlags.Static)!;

    public QueryableStringCaseInsensitiveContainsHandler(InputParser inputParser)
        : base(inputParser)
    {
        CanBeNull = false;
    }

    protected override int Operation => DefaultFilterOperations.CaseInsensitiveContains;

    public override Expression HandleOperation(
        QueryableFilterContext context,
        IFilterOperationField field,
        IValueNode value,
        object? parsedValue)
    {
        var property = context.GetInstance();
        return ContainsInvariant(property, parsedValue);
    }

    public static Expression ContainsInvariant(
        Expression property,
        object? value)
    {
        return Expression.AndAlso(
            Expression.NotEqual(property, _sNull),
            Expression.Call(
                property,
                _sContainsInvariant,
                CreateParameter(value, property.Type),
                Expression.Constant(StringComparison.InvariantCultureIgnoreCase)));
    }

    private static Expression CreateParameter(object? value, Type type)
    {
        return (Expression)_sCreateAndConvert
            .MakeGenericMethod(type)
            .Invoke(null, new[] { value })!;
    }

    private static Expression CreateAndConvertParameter<T>(object value)
    {
        Expression<Func<T>> lambda = () => (T)value;
        return lambda.Body;
    }
}
