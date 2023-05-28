namespace HotChocolate.Data.ElasticSearch;

public class WildCardOperation : ILeafSearchOperation
{
    public WildCardOperation(
        string path,
        int boost,
        ElasticSearchOperationKind kind,
        WildCardOperationKind wildCardOperationKind,
        object value)
    {
        Field = path;
        Boost = boost;
        Value = value;
        Kind = kind;
        WildCardOperationKind = wildCardOperationKind;
    }

    public string Field { get; }

    public int Boost { get; }

    public object Value { get; }

    public ElasticSearchOperationKind Kind { get; }

    public WildCardOperationKind WildCardOperationKind { get; }
}
