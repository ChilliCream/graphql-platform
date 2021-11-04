using HotChocolate.AspNetCore;
using HotChocolate.AzureFunctions;
using HotChocolate.Execution.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides DI extension methods to configure a GraphQL server.
/// </summary>
public static class HotChocolateAzureFunctionServiceCollectionExtensions
{
    /// <summary>
    /// Adds a GraphQL server and Azure Functions integration services to the DI.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/>.
    /// </param>
    /// <param name="maxAllowedRequestSize">
    /// The max allowed GraphQL request size.
    /// </param>
    /// <param name="apiRoute">
    /// The API route that was used in the GraphQL Azure Function.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> so that configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <see cref="IServiceCollection"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddGraphQLFunction(
        this IServiceCollection services,
        int maxAllowedRequestSize = 20 * 1000 * 1000,
        string apiRoute = "/api/graphql")
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        IRequestExecutorBuilder executorBuilder =
            services.AddGraphQLServer(maxAllowedRequestSize: maxAllowedRequestSize);

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IExtensionConfigProvider, GraphQLExtensions>());

        services.AddSingleton<IGraphQLRequestExecutor>(sp =>
        {
            PathString path = apiRoute.TrimEnd('/');
            IFileProvider fileProvider = CreateFileProvider();

            var pipelineBuilder = new PipelineBuilder();

            pipelineBuilder
                .UseMiddleware<WebSocketSubscriptionMiddleware>()
                .UseMiddleware<HttpPostMiddleware>()
                .UseMiddleware<HttpMultipartMiddleware>()
                .UseMiddleware<HttpGetSchemaMiddleware>()
                .UseMiddleware<ToolDefaultFileMiddleware>(fileProvider, path)
                .UseMiddleware<ToolOptionsFileMiddleware>(path)
                .UseMiddleware<ToolStaticFileMiddleware>(fileProvider, path)
                .UseMiddleware<HttpGetMiddleware>();

            RequestDelegate pipeline = pipelineBuilder.Compile(sp);
            return new DefaultGraphQLRequestExecutor(pipeline);
        });

        return executorBuilder;
    }

    private static IFileProvider CreateFileProvider()
    {
        Type type = typeof(HttpMultipartMiddleware);
        var resourceNamespace = typeof(MiddlewareBase).Namespace + ".Resources";
        return new EmbeddedFileProvider(type.Assembly, resourceNamespace);
    }
}
