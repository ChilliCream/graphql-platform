using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Data.ElasticSearch.ElasticSearchOperationKind;
using static HotChocolate.Data.Filters.DefaultFilterOperations;

namespace HotChocolate.Data.ElasticSearch.Filters.Comparable;

public class ElasticSearchComparableGreaterThanOrEqualsHandler
    : ElasticSearchComparableOperationHandler
{
    /// <inheritdoc />
    public ElasticSearchComparableGreaterThanOrEqualsHandler(InputParser inputParser)
        : base(inputParser)
    {
    }

    /// <inheritdoc />
    protected override int Operation => GreaterThanOrEquals;

    /// <inheritdoc />
    public override ISearchOperation HandleOperation(
        ElasticSearchFilterVisitorContext context,
        IFilterOperationField field,
        IValueNode value,
        object? parsedValue)
    {
        return parsedValue switch
        {
            double doubleVal => new RangeOperation<double>(context.GetPath(), Filter)
            {
                GreaterThanOrEquals = doubleVal
            },
            float floatValue => new RangeOperation<double>(context.GetPath(), Filter)
            {
                GreaterThanOrEquals = floatValue
            },
            sbyte sbyteValue => new RangeOperation<long>(context.GetPath(), Filter)
            {
                GreaterThanOrEquals = sbyteValue
            },
            byte byteValue => new RangeOperation<long>(context.GetPath(), Filter)
            {
                GreaterThanOrEquals = byteValue
            },
            short shortValue => new RangeOperation<long>(context.GetPath(), Filter)
            {
                GreaterThanOrEquals = shortValue
            },
            ushort uShortValue => new RangeOperation<long>(context.GetPath(), Filter)
            {
                GreaterThanOrEquals = uShortValue
            },
            uint uIntValue => new RangeOperation<long>(context.GetPath(), Filter)
            {
                GreaterThanOrEquals = uIntValue
            },
            int intValue => new RangeOperation<long>(context.GetPath(), Filter)
            {
                GreaterThanOrEquals = intValue
            },
            long longValue => new RangeOperation<long>(context.GetPath(), Filter)
            {
                GreaterThanOrEquals = longValue
            },
            DateTime dateTimeVal => new RangeOperation<DateTime>(context.GetPath(), Filter)
            {
                GreaterThanOrEquals = dateTimeVal
            },
            _ => throw new InvalidOperationException()
        };
    }
}
