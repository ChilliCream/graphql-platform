using Microsoft.Extensions.DependencyInjection;
using MarshmallowPie.BackgroundServices;
using MarshmallowPie.Processing;

namespace MarshmallowPie
{
    public static class BackgroundServicesServiceCollectionExtensions
    {
        public static IServiceCollection AddPublishDocumentService(
            this IServiceCollection serviceCollection)
        {
            return serviceCollection
                .AddHostedService<PublishDocumentService>()
                .AddSingleton<IPublishDocumentHandler, PublishNewSchemaDocumentHandler>();
        }
    }
}
