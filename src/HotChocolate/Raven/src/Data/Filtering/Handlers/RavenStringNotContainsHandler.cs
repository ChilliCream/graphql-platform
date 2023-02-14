using System.Linq.Expressions;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Raven.Filtering.Handlers;

public class RavenStringNotContainsHandler : RavenStringContainsHandler
{
    public RavenStringNotContainsHandler(InputParser inputParser) : base(inputParser)
    {
        CanBeNull = false;
    }

    protected override int Operation => DefaultFilterOperations.NotContains;

    public override Expression HandleOperation(
        QueryableFilterContext context,
        IFilterOperationField field,
        IValueNode value,
        object? parsedValue)
    {
        return FilterExpressionBuilder.Not(
            base.HandleOperation(context, field, value, parsedValue));
    }
}
