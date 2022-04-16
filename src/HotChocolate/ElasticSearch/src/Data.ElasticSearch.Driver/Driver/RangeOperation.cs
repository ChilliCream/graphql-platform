namespace HotChocolate.Data.ElasticSearch;

public class RangeOperation : ILeafSearchOperation
{
    public RangeOperation(
        string path,
        ElasticSearchOperationKind kind,
        double? greaterThan,
        double? lowerThan,
        double? greaterThanOrEquals,
        double? lowerThanOrEquals)
    {
        Path = path;
        GreaterThan = greaterThan;
        LowerThan = lowerThan;
        GreaterThanOrEquals = greaterThanOrEquals;
        LowerThanOrEquals = lowerThanOrEquals;
        Kind = kind;
    }

    public string Path { get; }

    public double? GreaterThan { get; }

    public double? LowerThan { get; }

    public double? GreaterThanOrEquals { get; }

    public double? LowerThanOrEquals { get; }

    public ElasticSearchOperationKind Kind { get; }
}


