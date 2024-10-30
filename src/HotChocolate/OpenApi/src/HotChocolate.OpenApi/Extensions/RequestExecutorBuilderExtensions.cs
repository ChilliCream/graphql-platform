using HotChocolate.Execution.Configuration;
using HotChocolate.OpenApi.FieldMiddleware;
using HotChocolate.OpenApi.TypeInterceptors;
using HotChocolate.Skimmed.Serialization;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Readers;

namespace HotChocolate.OpenApi.Extensions;

public static class RequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddOpenApi(
        this IRequestExecutorBuilder builder,
        string httpClientName,
        string openApiDocumentText,
        bool enableMutationConventions = false,
        MutationConventionOptions mutationConventionOptions = default)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(nameof(httpClientName));
        ArgumentException.ThrowIfNullOrEmpty(nameof(openApiDocumentText));

        // Create OpenAPI document from the provided string.
        var openApiStringReader = new OpenApiStringReader();
        var openApiDocument = openApiStringReader.Read(openApiDocumentText, out _);

        var mutableSchemaBuilder = OpenApiMutableSchemaBuilder.New(
            openApiDocument,
            httpClientName);

        if (enableMutationConventions)
        {
            mutableSchemaBuilder.AddMutationConventions(mutationConventionOptions);
        }

        var mutableSchema = mutableSchemaBuilder.Build();

        builder.Services.AddSingleton(mutableSchema);

        builder
            .AddDocument(SchemaFormatter.FormatAsDocument(mutableSchema))
            .TryAddTypeInterceptor<AbstractResolverTypeInterceptor>()
            .TryAddTypeInterceptor<ContextDataTypeInterceptor>()
            .UseField<OpenApiFieldMiddleware>();

        return builder;
    }
}
