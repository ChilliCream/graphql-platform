using MarshmallowPie;
using MarshmallowPie.BackgroundServices;
using MarshmallowPie.Processing;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class BackgroundServicesServiceCollectionExtensions
    {
        public static IServiceCollection AddPublishDocumentService(
            this IServiceCollection serviceCollection)
        {
            return serviceCollection
                .AddHostedService<PublishDocumentService>()
                .AddSingleton<IPublishDocumentHandler, PublishNewSchemaDocumentHandler>()
                .AddSingleton<IPublishDocumentHandler, PublishSchemaHandler>()
                .AddSingleton<IPublishDocumentHandler, PublishNewQueryDocumentHandler>()
                .AddSingleton<IPublishDocumentHandler, PublishNewRelayDocumentHandler>()
                .AddSingleton<IPublishDocumentHandler, PublishQueryDocumentHandler>();
        }
    }
}
