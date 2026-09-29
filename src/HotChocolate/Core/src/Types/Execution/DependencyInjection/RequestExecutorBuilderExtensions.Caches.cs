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

        var schemaName = builder.Name;

        // The document cache is registered as an instance so that the schema service provider never disposes it.
        return builder.ConfigureSchemaServices(
            (applicationServices, s) =>
                s.AddSingleton(applicationServices.GetRequiredKeyedService<IDocumentCache>(schemaName)));
    }
}
