using System.Linq.Expressions;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Marten.Filtering;

/// <summary>
/// Represents the <see cref="string"/> in operation handler.
/// </summary>
public class MartenQueryableStringInHandler : QueryableStringOperationHandler
{
    /// <summary>
    /// Initializes a new instance of <see cref="MartenQueryableStringInHandler"/>.
    /// </summary>
    /// <param name="inputParser">The input parser.</param>
    public MartenQueryableStringInHandler(InputParser inputParser) : base(inputParser)
    {
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

        return MartenExpressionHelper.In(
            property,
            context.RuntimeTypes.Peek().Source,
            parsedValue);
    }
}
