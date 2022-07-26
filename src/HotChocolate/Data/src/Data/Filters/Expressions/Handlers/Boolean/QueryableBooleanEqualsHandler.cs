using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters.Expressions;

public class QueryableBooleanEqualsHandler
    : QueryableBooleanOperationHandler
{
    public QueryableBooleanEqualsHandler(InputParser inputParser)
        : base(inputParser)
    {
    }

    protected override int Operation => DefaultFilterOperations.Equals;

    public override Expression HandleOperation(
        QueryableFilterContext context,
        IFilterOperationField field,
        IValueNode value,
        object? parsedValue)
    {
        var property = context.GetInstance();
        return FilterExpressionBuilder.Equals(property, parsedValue);
    }
}
