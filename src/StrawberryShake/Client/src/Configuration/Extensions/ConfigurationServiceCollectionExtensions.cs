using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StrawberryShake.Configuration;

namespace StrawberryShake
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
