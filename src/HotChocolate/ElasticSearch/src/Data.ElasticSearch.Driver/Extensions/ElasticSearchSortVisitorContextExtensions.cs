using HotChocolate.Data.ElasticSearch.Sorting;

namespace HotChocolate.Data.ElasticSearch;

public static class ElasticSearchSortVisitorContextExtensions
{
    /// <summary>
    /// Returns the currently selected path of this context
    /// </summary>
    public static string GetPath(this ElasticSearchSortVisitorContext context) =>
        string.Join(".", context.Path.Reverse());
}
