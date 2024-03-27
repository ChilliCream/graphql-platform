namespace HotChocolate.Data.ElasticSearch;

public class ElasticSearchSortOperation
{
    public ElasticSearchSortOperation(string path, ElasticSearchSortDirection direction)
    {
        Direction = direction;
        Path = path;
    }

    public ElasticSearchSortDirection Direction { get;}

    public string Path { get; }
}
