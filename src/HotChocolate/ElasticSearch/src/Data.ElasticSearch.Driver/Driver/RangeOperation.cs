namespace HotChocolate.Data.ElasticSearch;

public class RangeOperation<T> : ILeafSearchOperation
{
    public RangeOperation(
        string path,
        ElasticSearchOperationKind kind)
    {
        Path = path;
        Kind = kind;
    }

    public string Path { get; }

    public T? GreaterThan { get; init; }

    public T? LowerThan { get; init; }

    public T? GreaterThanOrEquals { get; init; }

    public T? LowerThanOrEquals { get; init; }

    public ElasticSearchOperationKind Kind { get; }
}
