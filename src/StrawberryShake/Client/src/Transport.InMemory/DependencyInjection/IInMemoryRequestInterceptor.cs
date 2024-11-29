using HotChocolate.Execution;

namespace StrawberryShake.Transport.InMemory;

/// <summary>
/// Represents a interceptor for <see cref="OperationRequest"/> of a
/// <see cref="IInMemoryClient"/>
/// </summary>
public interface IInMemoryRequestInterceptor
{
    /// <summary>
    /// Intercepts requests of an <see cref="IInMemoryClient"/> before they are send to the
    /// schema
    /// </summary>
    /// <param name="serviceProvider">
    /// The <see cref="IServiceProvider"/>
    /// </param>
    /// <param name="request">
    /// The <see cref="OperationRequest"/> that will be executed
    /// </param>
    /// <param name="requestBuilder">
    /// The query builder that builds the request that will be executed
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that is cancelled when the request is aborted
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask"/> that is completed when the configuration of the
    /// <paramref name="requestBuilder"/> is done
    /// </returns>
    ValueTask OnCreateAsync(
        IServiceProvider serviceProvider,
        OperationRequest request,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken);
}
