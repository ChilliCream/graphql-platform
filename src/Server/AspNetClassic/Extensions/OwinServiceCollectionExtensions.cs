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

            services.AddSingleton<OwinContextAccessor>();
            services.AddSingleton<IOwinContextAccessor>(sp =>
                sp.GetService<OwinContextAccessor>());
            return services;
        }
    }
}
