using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Data.ElasticSearch;

/// <summary>
/// Marks a string property of a list element, or a list of strings, that is mapped as <c>text</c>
/// with a <c>keyword</c> subfield in the Elasticsearch index. A filter on the list reads the subfield,
/// which holds the exact value, instead of the analyzed text.
/// </summary>
/// <param name="subfield">The name of the <c>keyword</c> subfield.</param>
[AttributeUsage(AttributeTargets.Property)]
[Experimental(ElasticSearchExperiments.Provider)]
public sealed class ElasticSearchKeywordAttribute(string subfield = "keyword") : Attribute
{
    /// <summary>
    /// Gets the name of the <c>keyword</c> subfield.
    /// </summary>
    public string Subfield { get; } = subfield;
}
