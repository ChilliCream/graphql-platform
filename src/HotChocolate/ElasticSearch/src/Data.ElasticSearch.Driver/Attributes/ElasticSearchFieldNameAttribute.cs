namespace HotChocolate.Data.ElasticSearch.Attributes;

public sealed class ElasticSearchFieldNameAttribute : Attribute
{
    public string FieldName { get; }

    public ElasticSearchFieldNameAttribute(string fieldName)
    {
        FieldName = fieldName;
    }
}
