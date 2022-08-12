using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters.Expressions;

public class QueryableStringNotStartsWithHandler : QueryableStringOperationHandler
{
    public QueryableStringNotStartsWithHandler(InputParser inputParser) : base(inputParser)
    {
        CanBeNull = false;
    }

    protected override int Operation => DefaultFilterOperations.NotStartsWith;

    public override Expression HandleOperation(
        QueryableFilterContext context,
        IFilterOperationField field,
        IValueNode value,
        object? parsedValue)
    {
        if (parsedValue is null)
        {
            throw new GraphQLException(ErrorHelper.CreateNonNullError(field, value, context));
        }

        var property = context.GetInstance();
        return FilterExpressionBuilder.Not(
            FilterExpressionBuilder.StartsWith(property, parsedValue));
    }
}
