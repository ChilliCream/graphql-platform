using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Data.ElasticSearch.ElasticSearchOperationKind;
using static HotChocolate.Data.Filters.DefaultFilterOperations;

namespace HotChocolate.Data.ElasticSearch.Filters.Comparable;

public class ElasticSearchComparableGreaterThanOrEqualsHandler : ElasticSearchComparableOperationHandler
{
    /// <inheritdoc />
    public ElasticSearchComparableGreaterThanOrEqualsHandler(InputParser inputParser) : base(inputParser)
    {
    }

    /// <inheritdoc />
    protected override int Operation => GreaterThanOrEquals;

    /// <inheritdoc />
    public override ISearchOperation HandleOperation(ElasticSearchFilterVisitorContext context, IFilterOperationField field,
        IValueNode value, object? parsedValue)
    {
        return parsedValue switch
        {
            double doubleVal => new RangeOperation<double>(context.GetPath(), Filter)
            {
                GreaterThanOrEquals = new RangeOperationValue<double>(doubleVal)
            },
            float floatValue => new RangeOperation<double>(context.GetPath(), Filter)
            {
                GreaterThanOrEquals = new RangeOperationValue<double>(floatValue)
            },
            DateTime dateTimeVal => new RangeOperation<DateTime>(context.GetPath(), Filter)
            {
                GreaterThanOrEquals = new RangeOperationValue<DateTime>(dateTimeVal)
            },
            _ => throw new InvalidOperationException()
        };
    }
}
