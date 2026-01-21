using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Data.ElasticSearch.ElasticSearchOperationKind;
using static HotChocolate.Data.Filters.DefaultFilterOperations;

namespace HotChocolate.Data.ElasticSearch.Filters.Comparable;

public class ElasticSearchComparableLowerThanHandler : ElasticSearchComparableOperationHandler
{
    /// <inheritdoc />
    public ElasticSearchComparableLowerThanHandler(InputParser inputParser) : base(inputParser)
    {
    }

    /// <inheritdoc />
    protected override int Operation => LowerThan;

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
                LowerThan = doubleVal
            },
            float floatValue => new RangeOperation<double>(context.GetPath(), Filter)
            {
                LowerThan = floatValue
            },
            sbyte sbyteValue => new RangeOperation<long>(context.GetPath(), Filter)
            {
                LowerThan = sbyteValue
            },
            byte byteValue => new RangeOperation<long>(context.GetPath(), Filter)
            {
                LowerThan = byteValue
            },
            short shortValue => new RangeOperation<long>(context.GetPath(), Filter)
            {
                LowerThan = shortValue
            },
            ushort uShortValue => new RangeOperation<long>(context.GetPath(), Filter)
            {
                LowerThan = uShortValue
            },
            uint uIntValue => new RangeOperation<long>(context.GetPath(), Filter)
            {
                LowerThan = uIntValue
            },
            int intValue => new RangeOperation<long>(context.GetPath(), Filter)
            {
                LowerThan = intValue
            },
            long longValue => new RangeOperation<long>(context.GetPath(), Filter)
            {
                LowerThan = longValue
            },
            DateTime dateTimeVal => new RangeOperation<DateTime>(context.GetPath(), Filter)
            {
                LowerThan = dateTimeVal
            },
            _ => throw new InvalidOperationException()
        };
    }
}
