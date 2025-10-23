﻿using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Data.Filters.DefaultFilterOperations;

namespace HotChocolate.Data.ElasticSearch.Filters.Comparable;

public class ElasticSearchComparableNotGreaterThanOrEqualsHandler
    : ElasticSearchComparableGreaterThanOrEqualsHandler
{
    /// <inheritdoc />
    public ElasticSearchComparableNotGreaterThanOrEqualsHandler(InputParser inputParser)
        : base(inputParser)
    {
    }

    /// <inheritdoc />
    protected override int Operation => NotGreaterThanOrEquals;

    /// <inheritdoc />
    public override ISearchOperation HandleOperation(
        ElasticSearchFilterVisitorContext context,
        IFilterOperationField field,
        IValueNode value,
        object? parsedValue)
    {
        var operation = base.HandleOperation(context, field, value, parsedValue);
        return ElasticSearchOperationHelpers.Negate(operation);
    }
}
