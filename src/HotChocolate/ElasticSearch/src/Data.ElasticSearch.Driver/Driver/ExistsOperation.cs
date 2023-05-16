namespace HotChocolate.Data.ElasticSearch;

public class ExistsOperation : ILeafSearchOperation
{
    public ExistsOperation(string path, ElasticSearchOperationKind kind)
    {
        Path = path;
        Kind = kind;
    }

    public string Path { get; }

    public ElasticSearchOperationKind Kind { get; }
}
