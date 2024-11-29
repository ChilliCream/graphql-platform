using ChilliCream.Nitro.App;
using HotChocolate;
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
    /// <param name="schemaName">
    /// The name of the schema that shall be used by this Azure Function.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> so that configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <see cref="IServiceCollection"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddGraphQLFunction(
        this IServiceCollection services,
        int maxAllowedRequestSize = GraphQLAzureFunctionsConstants.DefaultMaxRequests,
        string apiRoute = GraphQLAzureFunctionsConstants.DefaultGraphQLRoute,
        string? schemaName = default)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        var executorBuilder =
            services.AddGraphQLServer(maxAllowedRequestSize: maxAllowedRequestSize);

        // Register AzFunc Custom Binding Extensions for In-Process Functions.
        // NOTE: This does not work for Isolated Process due to (but is not harmful at all of
        // isolated process; it just remains dormant):
        // 1) Bindings always execute in-process and values must be marshaled between
        // the Host Process & the Isolated Process Worker!
        // 2) Currently only String values are supported (obviously due to above complexities).
        // More Info. here (using Blob binding docs):
        // https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-blob-input#usage
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IExtensionConfigProvider, GraphQLExtensions>());

        // Add the Request Executor Dependency...
        services.AddAzureFunctionsGraphQLRequestExecutor(apiRoute, schemaName);

        return executorBuilder;
    }

    /// <summary>
    /// Internal method to adds the Request Executor dependency for Azure Functions both
    /// in-process and isolate-process. Normal configuration should use AddGraphQLFunction()
    /// extension instead which correctly call this internally.
    /// </summary>
    private static IServiceCollection AddAzureFunctionsGraphQLRequestExecutor(
        this IServiceCollection services,
        string apiRoute = GraphQLAzureFunctionsConstants.DefaultGraphQLRoute,
        string? schemaName = default)
    {
        services.AddSingleton<IGraphQLRequestExecutor>(sp =>
        {
            PathString path = apiRoute.TrimEnd('/');
            var options = new GraphQLServerOptions();

            foreach (var configure in sp.GetServices<Action<GraphQLServerOptions>>())
            {
                configure(options);
            }

            // We need to set the ServeMode to Embedded to ensure that the GraphQL IDE is
            // working since the isolation mode does not allow us to take control over the response
            // object.
            options.Tool.ServeMode = GraphQLToolServeMode.Embedded;

            var schemaNameOrDefault = schemaName ?? Schema.DefaultName;

            var pipeline = new PipelineBuilder()
                    .UseMiddleware<WebSocketSubscriptionMiddleware>(schemaNameOrDefault)
                    .UseMiddleware<HttpPostMiddleware>(schemaNameOrDefault)
                    .UseMiddleware<HttpMultipartMiddleware>(schemaNameOrDefault)
                    .UseMiddleware<HttpGetMiddleware>(schemaNameOrDefault)
                    .UseNitroApp(path)
                    .UseMiddleware<HttpGetSchemaMiddleware>(
                        schemaNameOrDefault,
                        path,
                        MiddlewareRoutingType.Integrated)
                    .Compile(sp);

            return new DefaultGraphQLRequestExecutor(pipeline, options);
        });

        return services;
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

    private static PipelineBuilder UseNitroApp(
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
            .UseMiddleware<NitroAppOptionsFileMiddleware>(path)
            .UseMiddleware<NitroAppCdnMiddleware>(path, forwarderAccessor)
            .UseMiddleware<NitroAppDefaultFileMiddleware>(fileProvider, path)
            .UseMiddleware<NitroAppStaticFileMiddleware>(fileProvider, path);
    }

    private static IFileProvider CreateFileProvider()
    {
        var type = typeof(NitroAppStaticFileMiddleware);
        var resourceNamespace = type.Namespace + ".Resources";

        return new EmbeddedFileProvider(type.Assembly, resourceNamespace);
    }
}
