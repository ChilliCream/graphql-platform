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

        public static IServiceCollection AddResultParser(
            this IServiceCollection services,
            string clientName,
            Func<IServiceProvider, IValueSerializerCollection, IResultParser> factory)
        {
            services.AddOperationClientOptions(clientName)


        }

        public static IServiceCollection AddResultParser(
            this IServiceCollection services,
            string clientName,
            Func<IValueSerializerCollection, IResultParser> factory) =>
            services.AddResultParser(clientName, (sp, opt) => factory(opt));
    }
}
