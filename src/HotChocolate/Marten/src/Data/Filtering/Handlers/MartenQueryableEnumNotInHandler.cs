using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Marten.Filtering;

/// <summary>
/// Represents the enum in operation handler.
/// </summary>
public class MartenQueryableEnumNotInHandler : MartenQueryableComparableNotInHandler
{
    /// <summary>
    /// Initializes a new instance of <see cref="MartenQueryableEnumNotInHandler"/>.
    /// </summary>
    /// <param name="typeConverter">The type converter.</param>
    /// <param name="inputParser">The input parser.</param>
    public MartenQueryableEnumNotInHandler(
        ITypeConverter typeConverter,
        InputParser inputParser)
        : base(typeConverter, inputParser)
    {
    }

    /// <inheritdoc cref="QueryableComparableOperationHandler"/>
    public override bool CanHandle(
        ITypeCompletionContext context,
        IFilterInputTypeDefinition typeDefinition,
        IFilterFieldDefinition fieldDefinition)
    {
        return context.Type is IEnumOperationFilterInputType &&
            fieldDefinition is FilterOperationFieldDefinition operationField &&
            operationField.Id == Operation;
    }
}
