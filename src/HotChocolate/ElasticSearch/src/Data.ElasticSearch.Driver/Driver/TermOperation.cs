namespace HotChocolate.Data.ElasticSearch;

public class TermOperation : ILeafSearchOperation
{
    public TermOperation(
        string path,
        int boost,
        ElasticSearchOperationKind kind,
        object value)
    {
        Field = path;
        Boost = boost;
        Value = value;
        Kind = kind;
    }

    public string Field { get; }

    public int Boost { get; }

    public object Value { get; }

    public ElasticSearchOperationKind Kind { get; }
}
