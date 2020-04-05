using StrawberryShake;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StrawberryShake.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConfigurationServiceCollectionExtensions
    {
        public static IOperationClientBuilder AddOperationClientOptions(
            this IServiceCollection services,
            string clientName)
        {
            services.AddOptions();
            services.TryAddSingleton<IClientOptions, ClientOptions>();
            return new DefaultOperationClientBuilder(services, clientName);
        }
    }
}
