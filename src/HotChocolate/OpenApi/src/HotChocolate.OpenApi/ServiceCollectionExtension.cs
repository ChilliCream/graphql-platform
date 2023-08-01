using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;

namespace HotChocolate.OpenApi;

public static class ServiceCollectionExtension
{
    public static IRequestExecutorBuilder AddOpenApi(this IRequestExecutorBuilder requestExecutorBuilder, string openApi)
    {
        var document = new OpenApiStringReader().Read(openApi, out var diag);
        requestExecutorBuilder.ParseAndAddTypes(document, diag.SpecificationVersion);
        return requestExecutorBuilder;
    }

    public static IRequestExecutorBuilder AddOpenApi(this IRequestExecutorBuilder requestExecutorBuilder, Stream openApiStream)
    {
        var document = new OpenApiStreamReader().Read(openApiStream, out var diag);
        requestExecutorBuilder.ParseAndAddTypes(document, diag.SpecificationVersion);
        return requestExecutorBuilder;
    }

    private static void ParseAndAddTypes(this IRequestExecutorBuilder requestExecutorBuilder,
        OpenApiDocument apiDocument,
        OpenApiSpecVersion specVersion)
    {
        requestExecutorBuilder.AddJsonSupport();

        var wrapper = new OpenApiWrapper();
        wrapper.Wrap(apiDocument);
    }
}
