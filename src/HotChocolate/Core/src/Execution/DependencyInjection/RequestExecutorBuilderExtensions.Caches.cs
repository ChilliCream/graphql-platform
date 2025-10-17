using HotChocolate;
using HotChocolate.Execution.Caching;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class RequestExecutorBuilderExtensions
{
    internal static IRequestExecutorBuilder AddDocumentCache(this IRequestExecutorBuilder builder)
    {
        builder.Services.TryAddKeyedSingleton<IDocumentCache>(
            builder.Name,
            static (sp, schemaName) =>
            {
                var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<RequestExecutorSetup>>();
                var setup = optionsMonitor.Get((string)schemaName!);
                var options = setup.CreateSchemaOptions();

                return new DefaultDocumentCache(options.OperationDocumentCacheSize);
            });

        return builder.ConfigureSchemaServices(
            static (applicationServices, s) =>
                s.AddSingleton(schemaServices =>
                {
                    var schemaName = schemaServices.GetRequiredService<ISchemaDefinition>().Name;
                    return applicationServices.GetRequiredKeyedService<IDocumentCache>(schemaName);
                }));
    }
}
