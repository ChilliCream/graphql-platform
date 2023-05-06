using System;
using System.Collections.Generic;

namespace HotChocolate.Data.ElasticSearch;

public class BoolOperation : ISearchOperation
{
    public BoolOperation(
        IReadOnlyList<ISearchOperation> must,
        IReadOnlyList<ISearchOperation> should,
        IReadOnlyList<ISearchOperation> mustNot,
        IReadOnlyList<ISearchOperation> filter)
    {
        Must = must;
        Should = should;
        MustNot = mustNot;
        Filter = filter;
    }

    public IReadOnlyList<ISearchOperation> Must { get; }

    public IReadOnlyList<ISearchOperation> Should { get; }

    public IReadOnlyList<ISearchOperation> MustNot { get; }

    public IReadOnlyList<ISearchOperation> Filter { get; }

    public static BoolOperation Create(
        IReadOnlyList<ISearchOperation>? must = null,
        IReadOnlyList<ISearchOperation>? should = null,
        IReadOnlyList<ISearchOperation>? mustNot = null,
        IReadOnlyList<ISearchOperation>? filter = null)
        => new(
            must ?? new List<ISearchOperation>(),
            should ?? new List<ISearchOperation>(),
            mustNot ?? new List<ISearchOperation>(),
            filter ?? new List<ISearchOperation>());
}
