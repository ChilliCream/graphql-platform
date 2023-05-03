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
    public override ISearchOperation HandleOperation(ElasticSearchFilterVisitorContext context, IFilterOperationField field,
        IValueNode value, object? parsedValue)
    {
        return parsedValue switch
        {
            double doubleVal => new RangeOperation<double>(context.GetPath(), Filter)
            {
                LowerThan = new RangeOperationValue<double>(doubleVal)
            },
            float floatValue => new RangeOperation<double>(context.GetPath(), Filter)
            {
                LowerThan = new RangeOperationValue<double>(floatValue)
            },
            sbyte sbyteValue => new RangeOperation<long>(context.GetPath(), Filter)
            {
                LowerThan = new RangeOperationValue<long>(sbyteValue)
            },
            byte byteValue => new RangeOperation<long>(context.GetPath(), Filter)
            {
                LowerThan = new RangeOperationValue<long>(byteValue)
            },
            short shortValue => new RangeOperation<long>(context.GetPath(), Filter)
            {
                LowerThan = new RangeOperationValue<long>(shortValue)
            },
            ushort uShortValue => new RangeOperation<long>(context.GetPath(), Filter)
            {
                LowerThan = new RangeOperationValue<long>(uShortValue)
            },
            uint uIntValue => new RangeOperation<long>(context.GetPath(), Filter)
            {
                LowerThan = new RangeOperationValue<long>(uIntValue)
            },
            int intValue => new RangeOperation<long>(context.GetPath(), Filter)
            {
                LowerThan = new RangeOperationValue<long>(intValue)
            },
            long longValue => new RangeOperation<long>(context.GetPath(), Filter)
            {
                LowerThan = new RangeOperationValue<long>(longValue)
            },
            DateTime dateTimeVal => new RangeOperation<DateTime>(context.GetPath(), Filter)
            {
                LowerThan = new RangeOperationValue<DateTime>(dateTimeVal)
            },
            _ => throw new InvalidOperationException()
        };
    }
}
