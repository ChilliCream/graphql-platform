using Nest;

namespace HotChocolate.Data.ElasticSearch;

public static class ElasticClientExtensions
{
    public static IExecutable<T> AsExecutable<T>(this IElasticClient client)
        where T : class
        => new NestExecutable<T>(client);
}
