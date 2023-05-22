namespace HotChocolate.Data.ElasticSearch;

public class MatchOperation : ILeafSearchOperation
{
    public MatchOperation(
        string path,
        int boost,
        ElasticSearchOperationKind kind,
        string? value)
    {
        Field = path;
        Boost = boost;
        Value = value;
        Kind = kind;
    }

    public string Field { get; }

    public int Boost { get; }

    public string? Value { get; }

    public ElasticSearchOperationKind Kind { get; }
}
