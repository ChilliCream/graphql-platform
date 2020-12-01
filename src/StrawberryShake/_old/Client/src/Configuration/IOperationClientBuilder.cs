using Microsoft.Extensions.DependencyInjection;

namespace StrawberryShake.Configuration
{
    /// <summary>
    /// A builder for configuring named GraphQL client.
    /// </summary>
    public interface IOperationClientBuilder
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
