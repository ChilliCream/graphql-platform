namespace HotChocolate.Data.ElasticSearch.Attributes;

[AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
public sealed class ElasticSearchFieldNameAttribute : Attribute
{
    public string Path { get; }

    public ElasticSearchFieldNameAttribute(string path)
    {
        Path = path;
    }
}
