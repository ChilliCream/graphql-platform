using HotChocolate.Data.ElasticSearch.Filters;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.ElasticSearch.Sorting;

/// <summary>
/// Provides the capability to create a list of <see cref="ElasticSearchSortOperation"/>
/// based on the <see cref="IResolverContext"/>
/// </summary>
public interface IElasticSortFactory
{
    /// <summary>
    /// Creates a list of <see cref="ElasticSearchSortOperation"/> based on the <paramref name="context"/>.
    /// Uses the <paramref name="client"/> for client specific operations like
    /// <see cref="IAbstractElasticClient.GetName"/>.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="client"></param>
    /// <returns>A list of  <see cref="ElasticSearchSortOperation"/> or an empty list if no sorting was possible </returns>
    IReadOnlyList<ElasticSearchSortOperation> Create(IResolverContext context, IAbstractElasticClient client);
}
