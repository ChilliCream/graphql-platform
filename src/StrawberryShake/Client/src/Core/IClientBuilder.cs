using Microsoft.Extensions.DependencyInjection;

namespace StrawberryShake;

/// <summary>
/// Represents a builder that can be used to configure a client
/// </summary>
public interface IClientBuilder<T> : IScopedClientBuilder<T> where T : IStoreAccessor
{
    /// <summary>
    /// The <see cref="IServiceCollection"/> of the client
    /// </summary>
    public IServiceCollection ClientServices { get; }
}
