using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters.Expressions;

public class QueryableStringEndsWithHandler : QueryableStringOperationHandler
{
    public QueryableStringEndsWithHandler(InputParser inputParser) : base(inputParser)
    {
        CanBeNull = false;
    }

    protected override int Operation => DefaultFilterOperations.EndsWith;

    public override Expression HandleOperation(
        QueryableFilterContext context,
        IFilterOperationField field,
        IValueNode value,
        object? parsedValue)
    {
        var property = context.GetInstance();

        if (parsedValue is null)
        {
            throw new GraphQLException(ErrorHelper.CreateNonNullError(field, value, context));
        }

        return FilterExpressionBuilder.EndsWith(property, parsedValue);
    }
}
