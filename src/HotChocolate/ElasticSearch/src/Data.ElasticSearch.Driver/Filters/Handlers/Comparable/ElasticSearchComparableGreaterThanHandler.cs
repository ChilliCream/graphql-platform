using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Data.ElasticSearch.ElasticSearchOperationKind;
using static HotChocolate.Data.Filters.DefaultFilterOperations;

namespace HotChocolate.Data.ElasticSearch.Filters.Comparable;

public class ElasticSearchComparableGreaterThanHandler : ElasticSearchComparableOperationHandler
{
    /// <inheritdoc />
    public ElasticSearchComparableGreaterThanHandler(InputParser inputParser) : base(inputParser)
    {
    }

    /// <inheritdoc />
    protected override int Operation => GreaterThan;

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
                GreaterThan = doubleVal
            },
            float floatValue => new RangeOperation<double>(context.GetPath(), Filter)
            {
                GreaterThan = floatValue
            },
            sbyte sbyteValue => new RangeOperation<long>(context.GetPath(), Filter)
            {
                GreaterThan = sbyteValue
            },
            byte byteValue => new RangeOperation<long>(context.GetPath(), Filter)
            {
                GreaterThan = byteValue
            },
            short shortValue => new RangeOperation<long>(context.GetPath(), Filter)
            {
                GreaterThan = shortValue
            },
            ushort uShortValue => new RangeOperation<long>(context.GetPath(), Filter)
            {
                GreaterThan = uShortValue
            },
            uint uIntValue => new RangeOperation<long>(context.GetPath(), Filter)
            {
                GreaterThan = uIntValue
            },
            int intValue => new RangeOperation<long>(context.GetPath(), Filter)
            {
                GreaterThan = intValue
            },
            long longValue => new RangeOperation<long>(context.GetPath(), Filter)
            {
                GreaterThan = longValue
            },
            DateTime dateTimeVal => new RangeOperation<DateTime>(context.GetPath(), Filter)
            {
                GreaterThan = dateTimeVal
            },
            _ => throw new InvalidOperationException()
        };
    }
}
