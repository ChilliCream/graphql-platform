using System;
using Microsoft.Extensions.DependencyInjection;

namespace StrawberryShake
{
    /// <summary>
    /// Represents a builder that can be used to configure a client
    /// </summary>
    public interface IClientBuilder
    {
        /// <summary>
        /// The name of the client
        /// </summary>
        public string ClientName { get; }

        /// <summary>
        /// The <see cref="IServiceCollection"/> this client was registered to
        /// </summary>
        public IServiceCollection Services { get; }

        /// <summary>
        /// The <see cref="IServiceCollection"/> of the client
        /// </summary>
        public IServiceCollection ClientServices { get; }

        /// <summary>
        /// The <see cref="IStoreAccessor"/> <see cref="Type"/>.
        /// </summary>
        public Type StoreAccessorType { get; }
    }
}
