using System.Collections.Generic;

namespace HotChocolate.Data.ElasticSearch;

public class BoolOperation : ISearchOperation
{
    public BoolOperation(
        IReadOnlyList<ISearchOperation> must,
        IReadOnlyList<ISearchOperation> should)
    {
        Must = must;
        Should = should;
    }

    public IReadOnlyList<ISearchOperation> Must { get; }

    public IReadOnlyList<ISearchOperation> Should { get; }

}
