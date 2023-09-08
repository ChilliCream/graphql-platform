using HotChocolate.Fusion.Metadata;

namespace HotChocolate.Fusion.Clients;

/// <summary>
/// Represents a factory for creating <see cref="IGraphQLClient"/> instances.
/// </summary>
public interface IGraphQLClientFactory
{
    /// <summary>
    /// Creates a new <see cref="IGraphQLClient"/> instance.
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    IGraphQLClient CreateClient(HttpClientConfiguration configuration);
}
