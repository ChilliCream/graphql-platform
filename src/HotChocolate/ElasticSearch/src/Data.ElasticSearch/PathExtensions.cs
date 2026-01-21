namespace HotChocolate.Data.ElasticSearch;

public static class PathExtensions
{
    public static string GetKeywordPath(this string path) => $"{path}.keyword";
}
