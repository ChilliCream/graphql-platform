namespace HotChocolate.Data.ElasticSearch;

public class ElasticSearchSortOperation : ISearchOperation
{
    public ElasticSearchSortOperation(string path, ElasticSearchSortDirection direction)
    {
        Direction = direction;
        Path = path;
    }

    public ElasticSearchSortDirection Direction { get;}

    public string Path { get; }

}
