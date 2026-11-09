namespace HotChocolate.Data.ElasticSearch;

/// <summary>
/// Marks a list of objects that is mapped as <c>nested</c> in the Elasticsearch index.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ElasticSearchNestedAttribute : Attribute;
