using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.ElasticSearch.Filters.Comparable;

public class ElasticSearchComparableNotLowerThanOrEqualsHandler : ElasticSearchComparableGreaterThanHandler
{
    /// <inheritdoc />
    public ElasticSearchComparableNotLowerThanOrEqualsHandler(InputParser inputParser) : base(inputParser)
    {
    }

    /// <inheritdoc />
    protected override int Operation => DefaultFilterOperations.NotLowerThanOrEquals;

    /// <inheritdoc />
    public override ISearchOperation HandleOperation(ElasticSearchFilterVisitorContext context, IFilterOperationField field,
        IValueNode value, object? parsedValue)
    {
        ISearchOperation operation = base.HandleOperation(context, field, value, parsedValue);
        return ElasticSearchOperationHelpers.Negate(operation);
    }
}
