using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Stitching;

namespace HotChocolate
{
    public static class StitchingServiceCollectionExtensions
    {
        public static IServiceCollection AddStitching(
            this IServiceCollection services)
        {
            return services.AddSingleton<IQueryBroker, QueryBroker>()
                .AddSingleton<IQueryParser, AnnotationQueryParser>();
        }
    }
}
