using Microsoft.Extensions.DependencyInjection;

namespace StrawberryShake.Transport.InMemory
{
    /// <summary>
    /// A builder for configuring named <see cref="IInMemoryClient"/> instances returned by
    /// <see cref="IInMemoryClientFactory"/>.
    /// </summary>
    public interface IInMemoryClientBuilder
    {
        /// <summary>
        /// Gets the name of the client configured by this builder.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the application service collection.
        /// </summary>
        IServiceCollection Services { get; }
    }
}
