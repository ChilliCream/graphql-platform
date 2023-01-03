namespace HotChocolate.Data.ElasticSearch;

public class WildcardOperation : ILeafSearchOperation
{
    public WildcardOperation(
        string path,
        ElasticSearchOperationKind kind,
        object value)
    {
        Path = path;
        Value = value;
        Kind = kind;
    }

    public string Path { get; }

    public object Value { get; }

    public ElasticSearchOperationKind Kind { get; }
}
