using System.Collections;
using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Data.ElasticSearch.ElasticSearchOperationKind;
using static HotChocolate.Data.Filters.DefaultFilterOperations;

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
        => fieldDefinition is FilterOperationFieldDefinition { Id: In };

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

        var enumerable = ((IEnumerable)parsedValue).Cast<object>().ToList();
        var shouldOperations = enumerable
            .Select(val => new MatchOperation(context.GetPath(), Filter, val.ToString()));

        return new BoolOperation(
            Enumerable.Empty<ISearchOperation>(),
            shouldOperations,
            Enumerable.Empty<ISearchOperation>(),
            Enumerable.Empty<ISearchOperation>());
    }
}
