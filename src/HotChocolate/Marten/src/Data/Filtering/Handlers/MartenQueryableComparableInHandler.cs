using System.Linq.Expressions;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Data.Marten.ThrowHelper;

namespace HotChocolate.Data.Marten.Filtering;

/// <summary>
/// Represents the comparable in operation handler.
/// </summary>
public class MartenQueryableComparableInHandler : QueryableComparableOperationHandler
{
    /// <summary>
    /// Initializes a new instance of <see cref="MartenQueryableComparableInHandler"/>.
    /// </summary>
    /// <param name="typeConverter">The type converter.</param>
    /// <param name="inputParser">The input parser.</param>
    public MartenQueryableComparableInHandler(
        ITypeConverter typeConverter,
        InputParser inputParser)
        : base(typeConverter, inputParser)
    {
        CanBeNull = false;
    }

    /// <summary>
    /// Specifies the database operation.
    /// </summary>
    protected override int Operation => DefaultFilterOperations.In;

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

        return MartenExpressionHelper.In(
            property,
            context.RuntimeTypes.Peek().Source,
            parsedValue);
    }
}
