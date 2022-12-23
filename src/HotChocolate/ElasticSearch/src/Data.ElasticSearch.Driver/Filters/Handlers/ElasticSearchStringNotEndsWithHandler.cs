using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.ElasticSearch.Filters;

/// <summary>
/// This filter operation handler maps a NotEndsWith operation field to a
/// <see cref="ISearchOperation"/>
/// </summary>
public class ElasticSearchStringNotEndsWithHandler
    : ElasticSearchStringEndsWithHandler
{
    /// <summary>
    /// Initializes a new instance of
    /// <see cref="ElasticSearchStringNotEndsWithHandler"/>
    /// </summary>
    public ElasticSearchStringNotEndsWithHandler(InputParser inputParser)
        : base(inputParser)
    {
    }

    /// <inheritdoc />
    public override bool CanHandle(
        ITypeCompletionContext context,
        IFilterInputTypeDefinition typeDefinition,
        IFilterFieldDefinition fieldDefinition)
        => context.Type is StringOperationFilterInputType &&
           fieldDefinition is FilterOperationFieldDefinition
           {
               Id: DefaultFilterOperations.NotEndsWith
           };

    /// <inheritdoc />
    public override ISearchOperation HandleOperation(
        ElasticSearchFilterVisitorContext context,
        IFilterOperationField field,
        IValueNode value,
        object? parsedValue)
    {
        ISearchOperation operation =
            base.HandleOperation(context, field, value, parsedValue);
        return ElasticSearchOperationHelpers.Negate(operation);
    }
}
