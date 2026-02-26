using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Data.Filters.DefaultFilterOperations;

namespace HotChocolate.Data.ElasticSearch.Filters;

/// <summary>
/// This filter operation handler maps a NotEquals operation field to a
/// <see cref="ISearchOperation"/>
/// </summary>
public class ElasticSearchStringNotEqualsOperationHandler
    : ElasticSearchStringEqualsOperationHandler
{
    /// <summary>
    /// Initializes a new instance of <see cref="ElasticSearchStringNotEqualsOperationHandler"/>
    /// </summary>
    public ElasticSearchStringNotEqualsOperationHandler(InputParser inputParser)
        : base(inputParser)
    {
    }

    /// <inheritdoc />
    public override bool CanHandle(
        ITypeCompletionContext context,
        IFilterInputTypeConfiguration typeDefinition,
        IFilterFieldConfiguration fieldDefinition)
        => context.Type is StringOperationFilterInputType
        && fieldDefinition is FilterOperationFieldConfiguration { Id: NotEquals };

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
