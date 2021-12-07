using System;
using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Filters.Expressions;

public class QueryableComparableNotGreaterThanHandler
    : QueryableComparableOperationHandler
{
    public QueryableComparableNotGreaterThanHandler(
        ITypeConverter typeConverter,
        InputParser inputParser)
        : base(typeConverter, inputParser)
    {
        CanBeNull = false;
    }

    protected override int Operation => DefaultFilterOperations.NotGreaterThan;

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

        return FilterExpressionBuilder.Not(
            FilterExpressionBuilder.GreaterThan(property, parsedValue));
    }
}
