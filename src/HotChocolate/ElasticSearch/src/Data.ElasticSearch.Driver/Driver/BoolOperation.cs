using System;
using System.Collections.Generic;

namespace HotChocolate.Data.ElasticSearch;

public class BoolOperation : ISearchOperation
{
    public BoolOperation(
        IEnumerable<ISearchOperation> must,
        IEnumerable<ISearchOperation> should,
        IEnumerable<ISearchOperation> mustNot,
        IEnumerable<ISearchOperation> filter)
    {
        Must = must;
        Should = should;
        MustNot = mustNot;
        Filter = filter;
    }

    public IEnumerable<ISearchOperation> Must { get; }

    public IEnumerable<ISearchOperation> Should { get; }

    public IEnumerable<ISearchOperation> MustNot { get; }

    public IEnumerable<ISearchOperation> Filter { get; }

    public static BoolOperation Create(
        IReadOnlyList<ISearchOperation>? must = null,
        IReadOnlyList<ISearchOperation>? should = null,
        IReadOnlyList<ISearchOperation>? mustNot = null,
        IReadOnlyList<ISearchOperation>? filter = null)
        => new(
            must ?? Enumerable.Empty<ISearchOperation>(),
            should ?? Enumerable.Empty<ISearchOperation>(),
            mustNot ?? Enumerable.Empty<ISearchOperation>(),
            filter ?? Enumerable.Empty<ISearchOperation>());
}
