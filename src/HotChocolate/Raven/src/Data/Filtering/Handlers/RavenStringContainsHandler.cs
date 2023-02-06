using System.Linq.Expressions;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Raven.Filtering.Handlers;

public class RavenStringContainsHandler : QueryableStringOperationHandler
{
    public RavenStringContainsHandler(InputParser inputParser) : base(inputParser)
    {
        CanBeNull = false;
    }

    protected override int Operation => DefaultFilterOperations.Contains;

    public override Expression HandleOperation(
        QueryableFilterContext context,
        IFilterOperationField field,
        IValueNode value,
        object? parsedValue)
    {
        var property = context.GetInstance();

        if (parsedValue is not string parsedString)
        {
            throw ThrowHelper.Filtering_CouldNotParseValue(this, value, field.Type, field);
        }

        return RavenFilterExpressionBuilder.IsMatch(property, parsedString);
    }
}
