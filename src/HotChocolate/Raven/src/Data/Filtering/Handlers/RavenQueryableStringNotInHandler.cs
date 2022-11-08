using System.Linq.Expressions;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Raven.Filtering;

public class RavenQueryableStringNotInHandler : QueryableStringOperationHandler
{
    public RavenQueryableStringNotInHandler(InputParser inputParser) : base(inputParser)
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
            RavenExpressionHelper.In(
                property,
                context.RuntimeTypes.Peek().Source,
                parsedValue));
    }
}
