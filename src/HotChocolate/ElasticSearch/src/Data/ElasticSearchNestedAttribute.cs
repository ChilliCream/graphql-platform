using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Data.ElasticSearch;

/// <summary>
/// Marks a list of objects that is mapped as <c>nested</c> in the Elasticsearch index.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
[Experimental(ElasticSearchExperiments.Provider)]
public sealed class ElasticSearchNestedAttribute : Attribute;
