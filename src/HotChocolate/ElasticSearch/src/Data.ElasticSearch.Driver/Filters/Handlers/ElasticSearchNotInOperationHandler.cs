using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Data.Filters.DefaultFilterOperations;

namespace HotChocolate.Data.ElasticSearch.Filters;

/// <summary>
/// This filter operation handler maps a NotIn operation field to a
/// <see cref="ISearchOperation"/>
/// </summary>
public class ElasticSearchNotInOperationHandler : ElasticSearchInOperationHandler
{
    /// <summary>
    /// Initializes a new instance of
    /// <see cref="ElasticSearchNotInOperationHandler"/>
    /// </summary>
    public ElasticSearchNotInOperationHandler(InputParser inputParser)
        : base(inputParser)
    {
    }

    /// <inheritdoc />
    public override bool CanHandle(
        ITypeCompletionContext context,
        IFilterInputTypeDefinition typeDefinition,
        IFilterFieldDefinition fieldDefinition)
        => fieldDefinition is FilterOperationFieldDefinition { Id: NotIn };

    /// <inheritdoc />
    public override ISearchOperation HandleOperation(
        ElasticSearchFilterVisitorContext context,
        IFilterOperationField field,
        IValueNode value,
        object? parsedValue)
    {
        var operation = base.HandleOperation(context, field, value, parsedValue);

        return ElasticSearchOperationHelpers.Negate(operation);
    }
}
