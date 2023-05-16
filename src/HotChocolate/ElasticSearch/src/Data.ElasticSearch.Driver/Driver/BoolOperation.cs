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
            must ?? Array.Empty<ISearchOperation>(),
            should ?? Array.Empty<ISearchOperation>(),
            mustNot ?? Array.Empty<ISearchOperation>(),
            filter ?? Array.Empty<ISearchOperation>());
}
