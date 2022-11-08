using System.Linq.Expressions;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Data.Raven.Filtering;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Raven.Filtering;

public class RavenQueryableStringInHandler : QueryableStringOperationHandler
{
    public RavenQueryableStringInHandler(InputParser inputParser) : base(inputParser)
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

        return RavenExpressionHelper.In(
            property,
            context.RuntimeTypes.Peek().Source,
            parsedValue);
    }
}
