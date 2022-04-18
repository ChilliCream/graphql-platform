using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters.Expressions;

public class QueryableStringCaseInsensitiveContainsHandler : QueryableStringOperationHandler
{
    private static readonly ConstantExpression s_null =
        Expression.Constant(null, typeof(object));

    public QueryableStringCaseInsensitiveContainsHandler(InputParser inputParser) : base(inputParser)
    {
        CanBeNull = false;
    }

    protected override int Operation => 2103;

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
                Expression.NotEqual(property, s_null),
                Expression.Call(
                    property,
                    s_containsInvariant,
                    CreateParameter(value, property.Type),
                    Expression.Constant(StringComparison.InvariantCultureIgnoreCase)));
    }

    private static readonly MethodInfo s_createAndConvert =
                typeof(FilterExpressionBuilder)
                    .GetMethod(
                        nameof(CreateAndConvertParameter),
                        BindingFlags.NonPublic | BindingFlags.Static)!;
    private static Expression CreateParameter(object? value, Type type)
    {
        return (Expression)s_createAndConvert
            .MakeGenericMethod(type).Invoke(null, new[] { value })!;
    }

    private static Expression CreateAndConvertParameter<T>(object value)
    {
        Expression<Func<T>> lambda = () => (T)value;
        return lambda.Body;
    }

    private static readonly MethodInfo s_containsInvariant =
        typeof(string).GetMethods().Single(m =>
            m.Name.Equals(nameof(string.Contains), StringComparison.Ordinal)
            && m.GetParameters().Length == 2
            && m.GetParameters().First().ParameterType == typeof(string)
            && m.GetParameters().Skip(1).First().ParameterType == typeof(StringComparison));
}
