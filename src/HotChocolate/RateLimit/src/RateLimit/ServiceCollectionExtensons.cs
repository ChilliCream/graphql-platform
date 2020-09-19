using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.RateLimit
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRateLimitCore(
            this IServiceCollection services)
        {
            return services
                .AddSingleton<ILimitStore, LimitStore>()
                .AddSingleton<ILimitProcessor, LimitProcessor>();
        }
    }
}
