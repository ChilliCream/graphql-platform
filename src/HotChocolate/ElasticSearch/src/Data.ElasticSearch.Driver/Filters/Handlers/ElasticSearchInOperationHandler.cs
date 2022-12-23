using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.ElasticSearch.Filters;

/// <summary>
/// This filter operation handler maps a In operation field to a
/// <see cref="ISearchOperation"/>
/// </summary>
public class ElasticSearchInOperationHandler
    : ElasticSearchOperationHandlerBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="ElasticSearchInOperationHandler"/>
    /// </summary>
    public ElasticSearchInOperationHandler(InputParser inputParser)
        : base(inputParser)
    {
    }

    /// <inheritdoc />
    public override bool CanHandle(
        ITypeCompletionContext context,
        IFilterInputTypeDefinition typeDefinition,
        IFilterFieldDefinition fieldDefinition)
        => context.Type is IListFilterInputType &&
           fieldDefinition is FilterOperationFieldDefinition { Id: DefaultFilterOperations.In };

    /// <inheritdoc />
    public override ISearchOperation HandleOperation(
        ElasticSearchFilterVisitorContext context,
        IFilterOperationField field,
        IValueNode value,
        object? parsedValue)
    {
        if (parsedValue is null)
        {
            throw new InvalidOperationException();
        }

        return new TermOperation(context.GetPath(), ElasticSearchOperationKind.Filter, parsedValue);
    }
}
