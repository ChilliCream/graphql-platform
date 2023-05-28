namespace HotChocolate.Data.ElasticSearch;

public class ExistsOperation : ILeafSearchOperation
{
    public ExistsOperation(string field, int boost, ElasticSearchOperationKind kind)
    {
        Field = field;
        Boost = boost;
        Kind = kind;
    }

    public string Field { get; }

    public int Boost { get; }

    public ElasticSearchOperationKind Kind { get; }
}
