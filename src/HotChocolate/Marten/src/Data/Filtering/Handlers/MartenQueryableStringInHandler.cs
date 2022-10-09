using System.Linq.Expressions;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Data.Marten.Filtering;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Marten.Filtering;

public class MartenQueryableStringInHandler : QueryableStringOperationHandler
{
    public MartenQueryableStringInHandler(InputParser inputParser) : base(inputParser)
    {
    }

    protected override int Operation => DefaultFilterOperations.In;

    public override Expression HandleOperation(
        QueryableFilterContext context,
        IFilterOperationField field,
        IValueNode value,
        object? parsedValue)
    {
        var property = context.GetInstance();

        return MartenExpressionHelper.In(
            property,
            context.RuntimeTypes.Peek().Source,
            parsedValue);
    }
}
