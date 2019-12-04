using System;
using Microsoft.Extensions.DependencyInjection;

namespace StrawberryShake.Configuration
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
