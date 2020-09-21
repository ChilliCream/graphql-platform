using System;
using HotChocolate.RateLimit;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore.RateLimit
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRateLimit(
            this IServiceCollection services,
            Action<RateLimitOptions> options)
        {
            return services
                .Configure(options)
                .AddSingleton<IRateLimitContext, RateLimitContext>()
                .AddRateLimitCore();
        }
    }
}
