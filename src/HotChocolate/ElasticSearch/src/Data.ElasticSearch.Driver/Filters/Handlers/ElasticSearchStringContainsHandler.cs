using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Data.Filters.DefaultFilterOperations;

namespace HotChocolate.Data.ElasticSearch.Filters;

/// <summary>
/// This filter operation handler maps a Contains operation field to a
/// <see cref="ISearchOperation"/>
/// </summary>
public class ElasticSearchStringContainsHandler
    : ElasticSearchOperationHandlerBase
{
    /// <summary>
    /// Initializes a new instance of
    /// <see cref="ElasticSearchStringContainsHandler"/>
    /// </summary>
    public ElasticSearchStringContainsHandler(InputParser inputParser)
        : base(inputParser)
    {
    }

    /// <inheritdoc />
    public override bool CanHandle(
        ITypeCompletionContext context,
        IFilterInputTypeConfiguration typeDefinition,
        IFilterFieldConfiguration fieldDefinition)
        => context.Type is StringOperationFilterInputType
        && fieldDefinition is FilterOperationFieldConfiguration { Id: Contains };

    /// <inheritdoc />
    public override ISearchOperation HandleOperation(
        ElasticSearchFilterVisitorContext context,
        IFilterOperationField field,
        IValueNode value,
        object? parsedValue)
    {
        if (parsedValue is not string val)
        {
            throw ThrowHelper.Filtering_WrongValueProvided(field);
        }

        var metadata = field.GetElasticMetadata();

        return new WildCardOperation(
            context.GetPath(),
            metadata.Kind,
            WildCardOperationKind.Contains,
            val);
    }
}
