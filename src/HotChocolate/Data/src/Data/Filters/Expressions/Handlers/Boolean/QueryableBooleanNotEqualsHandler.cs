using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters.Expressions;

public class QueryableBooleanNotEqualsHandler
    : QueryableBooleanOperationHandler
{
    public QueryableBooleanNotEqualsHandler(InputParser inputParser)
        : base(inputParser)
    {
    }

    protected override int Operation => DefaultFilterOperations.NotEquals;

    public override Expression HandleOperation(
        QueryableFilterContext context,
        IFilterOperationField field,
        IValueNode value,
        object? parsedValue)
    {
        var property = context.GetInstance();

        if (parsedValue is bool boolValue && property.Type == typeof(bool))
        {
            return boolValue
                ? FilterExpressionBuilder.Not(property)
                : property;
        }

        return FilterExpressionBuilder.NotEquals(property, parsedValue);
    }

    public static QueryableBooleanNotEqualsHandler Create(FilterProviderContext context) => new(context.InputParser);
}
