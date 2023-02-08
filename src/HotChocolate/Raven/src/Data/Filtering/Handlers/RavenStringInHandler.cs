using System.Linq.Expressions;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Raven.Filtering.Handlers;

public class RavenStringInHandler : QueryableStringOperationHandler
{
    public RavenStringInHandler(InputParser inputParser) : base(inputParser)
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

        return RavenFilterExpressionBuilder.In(
            property,
            context.RuntimeTypes.Peek().Source,
            parsedValue);
    }
}
