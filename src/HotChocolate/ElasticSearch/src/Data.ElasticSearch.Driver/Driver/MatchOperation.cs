namespace HotChocolate.Data.ElasticSearch;

public class MatchOperation : ILeafSearchOperation
{
    public MatchOperation(
        string path,
        ElasticSearchOperationKind kind,
        string? value)
    {
        Path = path;
        Value = value;
        Kind = kind;
    }

    public string Path { get; }

    public string? Value { get; }

    public ElasticSearchOperationKind Kind { get; }
}
