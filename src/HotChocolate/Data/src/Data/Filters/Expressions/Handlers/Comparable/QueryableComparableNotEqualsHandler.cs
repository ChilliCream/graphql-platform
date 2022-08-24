using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Filters.Expressions;

public class QueryableComparableNotEqualsHandler
    : QueryableComparableOperationHandler
{
    public QueryableComparableNotEqualsHandler(
        ITypeConverter typeConverter,
        InputParser inputParser)
        : base(typeConverter, inputParser)
    {
    }

    protected override int Operation => DefaultFilterOperations.NotEquals;

    public override Expression HandleOperation(
        QueryableFilterContext context,
        IFilterOperationField field,
        IValueNode value,
        object? parsedValue)
    {
        var property = context.GetInstance();
        parsedValue = ParseValue(value, parsedValue, field.Type, context);
        return FilterExpressionBuilder.NotEquals(property, parsedValue);
    }
}
