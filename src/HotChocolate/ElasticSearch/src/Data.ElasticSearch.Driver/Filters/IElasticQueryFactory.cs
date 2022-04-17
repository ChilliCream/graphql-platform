using HotChocolate.Resolvers;

namespace HotChocolate.Data.ElasticSearch.Filters;

/// <summary>
///
/// </summary>
public interface IElasticQueryFactory
{
    /// <summary>
    /// Creates a <see cref="BoolOperation"/> based on the <paramref name="context"/>.
    /// Uses the <paramref name="client"/> for client specific operations like
    /// <see cref="IAbstractElasticClient.GetName"/>.
    /// </summary>
    /// <returns>
    /// Either a <see cref="BoolOperation"/> or <c>null</c> if there was no filter or
    /// the translation was not possible
    /// </returns>
    BoolOperation? Create(IResolverContext context, IAbstractElasticClient client);
}
