﻿using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.ElasticSearch.Filters.Comparable;

public class ElasticSearchComparableGreaterThanOrEqualsHandler : ElasticSearchComparableOperationHandler
{
    /// <inheritdoc />
    public ElasticSearchComparableGreaterThanOrEqualsHandler(InputParser inputParser) : base(inputParser)
    {
    }

    /// <inheritdoc />
    protected override int Operation => DefaultFilterOperations.GreaterThanOrEquals;

    /// <inheritdoc />
    public override ISearchOperation HandleOperation(ElasticSearchFilterVisitorContext context, IFilterOperationField field,
        IValueNode value, object? parsedValue)
    {
        return parsedValue switch
        {
            double doubleVal => new RangeOperation<double>(context.GetPath(), context.Boost, ElasticSearchOperationKind.Filter)
            {
                GreaterThanOrEquals = new RangeOperationValue<double>(doubleVal)
            },
            float floatValue => new RangeOperation<double>(context.GetPath(), context.Boost, ElasticSearchOperationKind.Filter)
            {
                GreaterThanOrEquals = new RangeOperationValue<double>(floatValue)
            },
            string stringValue => new RangeOperation<string>(context.GetPath(), context.Boost, ElasticSearchOperationKind.Filter)
            {
                GreaterThanOrEquals = new RangeOperationValue<string>(stringValue)
            },
            DateTime dateTimeVal => new RangeOperation<DateTime>(context.GetPath(), context.Boost, ElasticSearchOperationKind.Filter)
            {
                GreaterThanOrEquals = new RangeOperationValue<DateTime>(dateTimeVal)
            },
            _ => throw new InvalidOperationException()
        };
    }
}
