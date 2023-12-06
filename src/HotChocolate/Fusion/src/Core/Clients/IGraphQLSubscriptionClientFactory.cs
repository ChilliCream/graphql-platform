using HotChocolate.Fusion.Metadata;

namespace HotChocolate.Fusion.Clients;

/// <summary>
/// Represents a factory for creating <see cref="IGraphQLSubscriptionClient"/> instances.
/// </summary>
public interface IGraphQLSubscriptionClientFactory
{
    /// <summary>
    /// Creates a new <see cref="IGraphQLSubscriptionClient"/> instance.
    /// </summary>
    /// <param name="configuration">
    /// The configuration of the client.
    /// </param>
    /// <returns>
    /// A new <see cref="IGraphQLSubscriptionClient"/> instance.
    /// </returns>
    IGraphQLSubscriptionClient CreateClient(IGraphQLClientConfiguration configuration);
}
