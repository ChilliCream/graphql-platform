using HotChocolate.Stitching;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate
{
    public static class StitchingServiceCollectionExtensions
    {
        public static IServiceCollection AddStitching(
            this IServiceCollection services)
        {
            return services.AddSingleton<IQueryBroker, QueryBroker>();
        }
    }
}
