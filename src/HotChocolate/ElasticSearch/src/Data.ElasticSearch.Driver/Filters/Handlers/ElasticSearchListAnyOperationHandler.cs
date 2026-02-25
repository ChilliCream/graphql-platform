using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.ElasticSearch.Filters;

/// <summary>
/// This filter operation handler maps a Any operation field to a <see cref="ISearchOperation"/>
/// </summary>
public class ElasticSearchListAnyOperationHandler
    : ElasticSearchOperationHandlerBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="ElasticSearchListAnyOperationHandler"/>
    /// </summary>
    public ElasticSearchListAnyOperationHandler(InputParser inputParser)
        : base(inputParser)
    {
    }

    /// <inheritdoc />
    public override bool CanHandle(
        ITypeCompletionContext context,
        IFilterInputTypeConfiguration typeDefinition,
        IFilterFieldConfiguration fieldDefinition)
        => context.Type is IListFilterInputType
        && fieldDefinition is FilterOperationFieldConfiguration { Id: DefaultFilterOperations.Any };

    /// <inheritdoc />
    public override ISearchOperation HandleOperation(
        ElasticSearchFilterVisitorContext context,
        IFilterOperationField field,
        IValueNode value,
        object? parsedValue)
    {
        if (parsedValue is not bool val)
        {
            throw ThrowHelper.Filtering_WrongValueProvided(field);
        }

        var metadata = field.GetElasticMetadata();

        ExistsOperation operation = new(context.GetPath(), metadata.Kind);

        if (val)
        {
            return operation;
        }

        return BoolOperation.Create(mustNot: new[] { operation });
    }
}
