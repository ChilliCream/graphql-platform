using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.ElasticSearch.Filters.Comparable;

public class ElasticSearchComparableLowerThanOrEqualsHandler : ElasticSearchComparableOperationHandler
{
    /// <inheritdoc />
    public ElasticSearchComparableLowerThanOrEqualsHandler(InputParser inputParser) : base(inputParser)
    {
    }

    /// <inheritdoc />
    protected override int Operation => DefaultFilterOperations.LowerThanOrEquals;

    /// <inheritdoc />
    public override ISearchOperation HandleOperation(ElasticSearchFilterVisitorContext context, IFilterOperationField field,
        IValueNode value, object? parsedValue)
    {
        return parsedValue switch
        {
            double doubleVal => new RangeOperation<double>(context.GetPath(), ElasticSearchOperationKind.Filter)
            {
                LowerThanOrEquals = new RangeOperationValue<double>(doubleVal)
            },
            float floatValue => new RangeOperation<double>(context.GetPath(), ElasticSearchOperationKind.Filter)
            {
                LowerThanOrEquals = new RangeOperationValue<double>(floatValue)
            },
            DateTime dateTimeVal => new RangeOperation<DateTime>(context.GetPath(), ElasticSearchOperationKind.Filter)
            {
                LowerThanOrEquals = new RangeOperationValue<DateTime>(dateTimeVal)
            },
            _ => throw new InvalidOperationException()
        };
    }
}
