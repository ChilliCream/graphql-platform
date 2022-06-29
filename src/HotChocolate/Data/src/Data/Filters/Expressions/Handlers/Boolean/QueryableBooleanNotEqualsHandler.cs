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
        return FilterExpressionBuilder.NotEquals(property, parsedValue);
    }
}
