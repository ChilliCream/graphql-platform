namespace HotChocolate.Data.ElasticSearch.Attributes;

public sealed class ElasticSearchAttribute : Attribute
{
    public string Field { get; }

    public int Boost { get; }

    public ElasticSearchAttribute(string field, int boost = 1)
    {
        Field = field;
        Boost = boost;
    }
}
