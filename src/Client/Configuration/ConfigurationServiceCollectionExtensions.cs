using System;
using Microsoft.Extensions.DependencyInjection;

namespace StrawberryShake.Configuration
{
    public static class ConfigurationServiceCollectionExtensions
    {
        public static IOperationClientBuilder AddOperationClientOptions(
            this IServiceCollection services,
            string name)
        {
            services.AddOptions();
            return new DefaultOperationClientBuilder(services, name);
        }

        public static IServiceCollection AddResultParser(
            this IServiceCollection services,
            Func<IServiceProvider, IResultParser> factory)
        {

        }

        public static IServiceCollection AddResultParser(
            this IServiceCollection services,
            Func<IResultParser> factory)
        {
            return
        }
    }
}
