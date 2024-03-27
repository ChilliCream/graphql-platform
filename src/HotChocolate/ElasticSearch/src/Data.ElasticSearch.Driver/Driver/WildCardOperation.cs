namespace HotChocolate.Data.ElasticSearch;

public class WildCardOperation : ILeafSearchOperation
{
    public WildCardOperation(
        string path,
        ElasticSearchOperationKind kind,
        WildCardOperationKind wildCardOperationKind,
        string value)
    {
        Path = path;
        Value = value;
        Kind = kind;
        WildCardOperationKind = wildCardOperationKind;
    }

    public string Path { get; }

    public string Value { get; }

    public ElasticSearchOperationKind Kind { get; }

    public WildCardOperationKind WildCardOperationKind { get; }
}
