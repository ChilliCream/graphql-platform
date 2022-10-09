using System.Linq.Expressions;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Data.Marten.Filtering;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Marten.Filtering;

public class MartenQueryableComparableInHandler
    : QueryableComparableOperationHandler
{
    public MartenQueryableComparableInHandler(
        ITypeConverter typeConverter,
        InputParser inputParser)
        : base(typeConverter, inputParser)
    {
        CanBeNull = false;
    }

    protected override int Operation => DefaultFilterOperations.In;

    public override Expression HandleOperation(
        QueryableFilterContext context,
        IFilterOperationField field,
        IValueNode value,
        object? parsedValue)
    {
        var property = context.GetInstance();
        parsedValue = ParseValue(value, parsedValue, field.Type, context);

        if (parsedValue is null)
        {
            throw HotChocolate.Data.ThrowHelper
                .Filtering_CouldNotParseValue(this, value, field.Type, field);
        }

        return MartenExpressionHelper.In(
            property,
            context.RuntimeTypes.Peek().Source,
            parsedValue);
    }
}
