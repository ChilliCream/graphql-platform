using Microsoft.Extensions.DependencyInjection;

namespace StrawberryShake
{
    /// <inheritdoc />
    public class ClientBuilder : IClientBuilder
    {
        /// <summary>
        /// Initializes a new instance of a <see cref="ClientBuilder"/>
        /// </summary>
        /// <param name="clientName">
        /// The name of the client
        /// </param>
        /// <param name="services">
        /// The service collection this client was registered to
        /// </param>
        public ClientBuilder(
            string clientName,
            IServiceCollection services)
        {
            ClientName = clientName;
            Services = services;
        }

        /// <inheritdoc />
        public string ClientName { get; }

        /// <inheritdoc />
        public IServiceCollection Services { get; }
    }
}
