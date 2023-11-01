using BananaCakePop.Middleware;
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
            var options = new GraphQLServerOptions();

            foreach (Action<GraphQLServerOptions> configure in
                sp.GetServices<Action<GraphQLServerOptions>>())
            {
                configure(options);
            }
            
            // We need to set the ServeMode to Embedded to ensure that the GraphQL IDE is
            // working since the isolation mode does not allow us to take control over the response
            // object.
            options.Tool.ServeMode = GraphQLToolServeMode.Embedded;

            RequestDelegate pipeline =
                new PipelineBuilder()
                    .UseMiddleware<WebSocketSubscriptionMiddleware>()
                    .UseMiddleware<HttpPostMiddleware>()
                    .UseMiddleware<HttpMultipartMiddleware>()
                    .UseMiddleware<HttpGetMiddleware>()
                    .UseBananaCakePop(path)
                    .UseMiddleware<HttpGetSchemaMiddleware>()
                    .Compile(sp);

            return new DefaultGraphQLRequestExecutor(pipeline, options);
        });

        return executorBuilder;
    }

    /// <summary>
    /// Modifies the GraphQL functions options.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="configure">
    /// A delegate to modify the options.
    /// </param>
    /// <returns>
    /// Returns <see cref="IRequestExecutorBuilder"/> so that configuration can be chained.
    /// </returns>
    public static IRequestExecutorBuilder ModifyFunctionOptions(
        this IRequestExecutorBuilder builder,
        Action<GraphQLServerOptions> configure)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        builder.Services.AddSingleton(configure);
        return builder;
    }
    
    private static PipelineBuilder UseBananaCakePop(
        this PipelineBuilder requestPipeline,
        PathString path)
    {
        if (requestPipeline is null)
        {
            throw new ArgumentNullException(nameof(requestPipeline));
        }

        path = path.ToString().TrimEnd('/');
        var fileProvider = CreateFileProvider();
        var forwarderAccessor = new HttpForwarderAccessor();

        return requestPipeline
            .UseMiddleware<BananaCakePopOptionsFileMiddleware>(path)
            .UseMiddleware<BananaCakePopCdnMiddleware>(path, forwarderAccessor)
            .UseMiddleware<BananaCakePopDefaultFileMiddleware>(fileProvider, path)
            .UseMiddleware<BananaCakePopStaticFileMiddleware>(fileProvider, path);
    }

    private static IFileProvider CreateFileProvider()
    {
        var type = typeof(BananaCakePopStaticFileMiddleware);
        var resourceNamespace = type.Namespace + ".Resources";
        return new EmbeddedFileProvider(type.Assembly, resourceNamespace);
    }
}
