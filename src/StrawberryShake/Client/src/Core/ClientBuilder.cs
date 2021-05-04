using Microsoft.Extensions.DependencyInjection;

namespace StrawberryShake
{
    /// <inheritdoc />
    public class ClientBuilder<T> : IClientBuilder<T> where T : IStoreAccessor
    {
        /// <summary>
        /// Initializes a new instance of a <see cref="ClientBuilder{T}"/>
        /// </summary>
        /// <param name="clientName">
        /// The name of the client
        /// </param>
        /// <param name="services">
        /// The service collection this client was registered to
        /// </param>
        /// <param name="clientServices">
        /// The service collection of the client
        /// </param>
        public ClientBuilder(
            string clientName,
            IServiceCollection services,
            IServiceCollection clientServices)
        {
            ClientName = clientName;
            Services = services;
            ClientServices = clientServices;
        }

        /// <inheritdoc />
        public string ClientName { get; }

        /// <inheritdoc />
        public IServiceCollection Services { get; }

        /// <inheritdoc />
        public IServiceCollection ClientServices { get; }
    }
}
