using System.Collections.Generic;

namespace HotChocolate.Data.ElasticSearch;

public class BoolOperation : ISearchOperation
{
    public BoolOperation(
        IReadOnlyList<ISearchOperation> must,
        IReadOnlyList<ISearchOperation> should,
        IReadOnlyList<ISearchOperation> mustNot)
    {
        Must = must;
        Should = should;
        MustNot = mustNot;
    }

    public IReadOnlyList<ISearchOperation> Must { get; }

    public IReadOnlyList<ISearchOperation> Should { get; }

    public IReadOnlyList<ISearchOperation> MustNot { get; }

    public bool IsEmpty => Must.Count == 0 && Should.Count == 0 && MustNot.Count == 0;
}
