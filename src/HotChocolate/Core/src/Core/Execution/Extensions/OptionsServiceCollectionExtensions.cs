using System;
using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OptionsServiceCollectionExtensions
    {
        public static IServiceCollection AddOptions(
            this IServiceCollection services,
            IQueryExecutionOptionsAccessor options)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return services
                .AddSingleton(options)
                .AddSingleton<IErrorHandlerOptionsAccessor>(options)
                .AddSingleton<IInstrumentationOptionsAccessor>(options)
                .AddSingleton<IQueryCacheSizeOptionsAccessor>(options)
                .AddSingleton<IRequestTimeoutOptionsAccessor>(options)
                .AddSingleton<IValidateQueryOptionsAccessor>(options);
        }
    }
}
