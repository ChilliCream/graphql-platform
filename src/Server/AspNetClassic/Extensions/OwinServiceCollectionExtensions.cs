using System;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetClassic
{
    public static class OwinServiceCollectionExtensions
    {
        public static IServiceCollection AddOwinContextAccessor(
            this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton<IOwinContextAccessor, OwinContextAccessor>();
            return services;
        }
    }
}
