namespace HotChocolate.Data.ElasticSearch;

public class ExistsOperation : ILeafSearchOperation
{
    public ExistsOperation(string field, ElasticSearchOperationKind kind)
    {
        Field = field;
        Kind = kind;
    }

    public string Field { get; }

    public ElasticSearchOperationKind Kind { get; }
}
