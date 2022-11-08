using System.Linq.Expressions;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Raven.Filtering;

public class RavenQueryableComparableNotInHandler
    : QueryableComparableOperationHandler
{
    public RavenQueryableComparableNotInHandler(
        ITypeConverter typeConverter,
        InputParser inputParser)
        : base(typeConverter, inputParser)
    {
        CanBeNull = false;
    }

    protected override int Operation => DefaultFilterOperations.NotIn;

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
            throw HotChocolate.Data.ThrowHelper.Filtering_CouldNotParseValue(this,
                value,
                field.Type,
                field);
        }

        return FilterExpressionBuilder.Not(
            RavenExpressionHelper.In(
                property,
                context.RuntimeTypes.Peek().Source,
                parsedValue));
    }
}
