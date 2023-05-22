namespace HotChocolate.Data.ElasticSearch;

public class WildCardOperation : ILeafSearchOperation
{
    public WildCardOperation(
        string path,
        ElasticSearchOperationKind kind,
        WildCardOperationKind wildCardOperationKind,
        object value)
    {
        Path = path;
        Value = value;
        Kind = kind;
        WildCardOperationKind = wildCardOperationKind;
    }

    public string Path { get; }

    public object Value { get; }

    public ElasticSearchOperationKind Kind { get; }

    public WildCardOperationKind WildCardOperationKind { get; }
}

public enum WildCardOperationKind
{
    StartsWith,
    EndsWith,
    Contains
}
