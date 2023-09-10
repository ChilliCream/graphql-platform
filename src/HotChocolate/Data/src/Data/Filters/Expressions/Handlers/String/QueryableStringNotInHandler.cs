using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters.Expressions;

public class QueryableStringNotInHandler : QueryableStringOperationHandler
{
    public QueryableStringNotInHandler(InputParser inputParser) : base(inputParser)
    {
    }

    protected override int Operation => DefaultFilterOperations.NotIn;

    public override Expression HandleOperation(
        QueryableFilterContext context,
        IFilterOperationField field,
        IValueNode value,
        object? parsedValue)
    {
        var property = context.GetInstance();

        return FilterExpressionBuilder.Not(
            FilterExpressionBuilder.In(
                property,
                context.RuntimeTypes.Peek().Source,
                parsedValue));
    }
}
