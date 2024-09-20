using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Filters.Expressions;

public class QueryableComparableNotLowerThanOrEqualsHandler
    : QueryableComparableOperationHandler
{
    public QueryableComparableNotLowerThanOrEqualsHandler(
        ITypeConverter typeConverter,
        InputParser inputParser)
        : base(typeConverter, inputParser)
    {
        CanBeNull = false;
    }

    protected override int Operation => DefaultFilterOperations.NotLowerThanOrEquals;

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
            throw ThrowHelper.Filtering_CouldNotParseValue(this, value, field.Type, field);
        }

        return FilterExpressionBuilder.Not(
            FilterExpressionBuilder.LowerThanOrEqual(property, parsedValue));
    }
}
