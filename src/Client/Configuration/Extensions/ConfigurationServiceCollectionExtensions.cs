using Microsoft.Extensions.DependencyInjection;
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
            return new DefaultOperationClientBuilder(services, clientName);
        }
    }
}
