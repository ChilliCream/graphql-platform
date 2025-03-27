using Microsoft.Extensions.DependencyInjection;

namespace StrawberryShake;

/// <inheritdoc />
/// <summary>
/// Initializes a new instance of a <see cref="ScopedClientBuilder{T}"/>
/// </summary>
/// <param name="clientName">
/// The name of the client
/// </param>
/// <param name="services">
/// The service collection this client was registered to
/// </param>
public class ScopedClientBuilder<T>(
    string clientName,
    IServiceCollection services)
    : IScopedClientBuilder<T>
    where T : IStoreAccessor
{

    /// <inheritdoc />
    public string ClientName { get; } = clientName;

    /// <inheritdoc />
    public IServiceCollection Services { get; } = services;
}
