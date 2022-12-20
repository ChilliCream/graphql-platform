namespace HotChocolate.Data.ElasticSearch;

public class RangeOperation<T> : ILeafSearchOperation
{
    public RangeOperation(
        string path,
        ElasticSearchOperationKind kind,
        T? greaterThan,
        T? lowerThan,
        T? greaterThanOrEquals,
        T? lowerThanOrEquals)
    {
        Path = path;
        GreaterThan = greaterThan;
        LowerThan = lowerThan;
        GreaterThanOrEquals = greaterThanOrEquals;
        LowerThanOrEquals = lowerThanOrEquals;
        Kind = kind;
    }

    public string Path { get; }

    public T? GreaterThan { get; }

    public T? LowerThan { get; }

    public T? GreaterThanOrEquals { get; }

    public T? LowerThanOrEquals { get; }

    public ElasticSearchOperationKind Kind { get; }
}


