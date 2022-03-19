using System.Linq.Expressions;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Filters.Expressions;
public class QueryableComparableInHandler
    : QueryableComparableOperationHandler
{
    public QueryableComparableInHandler(
        ITypeConverter typeConverter,
        InputParser inputParser)
        : base(typeConverter, inputParser)
    {
        CanBeNull = false;
    }

    protected override int Operation => DefaultFilterOperations.In;

    protected override bool IsValueNull(
        QueryableFilterContext context,
        IFilterOperationField field,
        IExtendedType runtimeType,
        IValueNode node,
        object? parsedValue)
        => ComparableInOperationHelpers.IsValueNull(runtimeType, node, parsedValue);

    public override Expression HandleOperation(
        QueryableFilterContext context,
        IFilterOperationField field,
        IValueNode value,
        object? parsedValue)
    {
        Expression property = context.GetInstance();
        parsedValue = ParseValue(value, parsedValue, field.Type, context);

        if (parsedValue is null)
        {
            throw ThrowHelper.Filtering_CouldNotParseValue(this, value, field.Type, field);
        }

        return FilterExpressionBuilder.In(
            property,
            context.RuntimeTypes.Peek().Source,
            parsedValue);
    }
}
