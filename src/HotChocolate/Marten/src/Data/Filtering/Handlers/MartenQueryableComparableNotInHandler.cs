using System.Linq.Expressions;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Data.Marten.ThrowHelper;

namespace HotChocolate.Data.Marten.Filtering;

/// <summary>
/// Represents the comparable not in operation handler.
/// </summary>
public class MartenQueryableComparableNotInHandler : QueryableComparableOperationHandler
{
    /// <summary>
    /// Initializes a new instance of <see cref="MartenQueryableComparableNotInHandler"/>.
    /// </summary>
    /// <param name="typeConverter">The type converter.</param>
    /// <param name="inputParser">The input parser.</param>
    public MartenQueryableComparableNotInHandler(
        ITypeConverter typeConverter,
        InputParser inputParser)
        : base(typeConverter, inputParser)
    {
        CanBeNull = false;
    }

    /// <summary>
    /// Specifies the database operation.
    /// </summary>
    protected override int Operation => DefaultFilterOperations.NotIn;

    /// <inheritdoc cref="QueryableOperationHandlerBase"/>
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
            throw Filtering_CouldNotParseValue(this, value, field.Type, field);
        }

        return FilterExpressionBuilder.Not(
            MartenExpressionHelper.In(
                property,
                context.RuntimeTypes.Peek().Source,
                parsedValue));
    }
}
