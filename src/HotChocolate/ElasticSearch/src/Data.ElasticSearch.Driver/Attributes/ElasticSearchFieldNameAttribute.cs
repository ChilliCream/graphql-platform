namespace HotChocolate.Data.ElasticSearch.Attributes;

public sealed class ElasticSearchFieldNameAttribute : Attribute
{
    public string Path { get; }

    public ElasticSearchFieldNameAttribute(string path)
    {
        Path = path;
    }
}
