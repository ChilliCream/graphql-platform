using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Aspire;

/// <summary>
/// Provides extension method to <see cref="IResourceBuilder{T}"/>
/// </summary>
public static class GraphQLResourceBuilderExtensions
{
    /// <summary>
    /// Marks a resource as having a GraphQL schema endpoint.
    /// </summary>
    /// <param name="builder">The resource builder</param>
    /// <param name="path">
    /// The GraphQL endpoint path. When omitted, the path defaults to
    /// <c>/graphql/schema.graphql</c> for a native schema download and <c>/graphql</c> for an
    /// Apollo Federation <c>_service.sdl</c> request.
    /// </param>
    /// <param name="endpointName">The endpoint name to use (defaults to "http")</param>
    /// <param name="sourceSchemaName">
    /// An optional source schema name assertion. When specified, it must exactly match the
    /// <c>name</c> in <c>schema-settings.json</c>.
    /// </param>
    /// <returns>The resource builder for chaining</returns>
    public static IResourceBuilder<T> WithGraphQLSchemaEndpoint<T>(
        this IResourceBuilder<T> builder,
        string? path = null,
        string endpointName = "http",
        string? sourceSchemaName = null)
        where T : IResourceWithEndpoints
    {
        if (path?.StartsWith('/') is false)
        {
            throw new ArgumentException(
                "The GraphQL schema endpoint path must start with '/'.",
                nameof(path));
        }

        builder.WithAnnotation(
            new GraphQLSourceSchemaAnnotation
            {
                SourceSchemaName = sourceSchemaName,
                EndpointName = endpointName,
                SchemaPath = path,
                Location = SourceSchemaLocationType.SchemaEndpoint
            });

        return builder;
    }

    /// <summary>
    /// Marks a resource as having a GraphQL schema file in its project directory.
    /// </summary>
    /// <param name="builder">The resource builder</param>
    /// <param name="fileName">The schema file name (defaults to "schema.graphql")</param>
    /// <param name="sourceSchemaName">The source schema name (defaults to the resource name)</param>
    /// <returns>The resource builder for chaining</returns>
    public static IResourceBuilder<T> WithGraphQLSchemaFile<T>(
        this IResourceBuilder<T> builder,
        string fileName = "schema.graphqls",
        string? sourceSchemaName = null)
        where T : IResourceWithEndpoints
    {
        builder.WithAnnotation(
            new GraphQLSourceSchemaAnnotation
            {
                SourceSchemaName = sourceSchemaName,
                SchemaPath = fileName,
                Location = SourceSchemaLocationType.ProjectDirectory
            });

        return builder;
    }

    /// <summary>
    /// Marks a project resource as exporting a GraphQL schema through the Hot Chocolate
    /// command-line schema exporter during publishing.
    /// </summary>
    /// <param name="builder">The project resource builder.</param>
    /// <param name="schemaName">The exact registered schema name to export.</param>
    /// <param name="configuration">The build configuration used by <c>dotnet run</c>.</param>
    /// <param name="targetFramework">The exact target framework used by <c>dotnet run</c>.</param>
    /// <param name="runtimeIdentifier">
    /// The runtime identifier used by <c>dotnet run</c>, or <see langword="null"/> for an
    /// explicitly portable export.
    /// </param>
    /// <param name="timeout">The maximum time allowed for the child process.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<ProjectResource> WithGraphQLSchemaExport(
        this IResourceBuilder<ProjectResource> builder,
        string schemaName,
        string configuration,
        string targetFramework,
        string? runtimeIdentifier,
        TimeSpan timeout)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);
        ArgumentException.ThrowIfNullOrWhiteSpace(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetFramework);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);

        if (runtimeIdentifier is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(runtimeIdentifier);
        }

        builder.WithAnnotation(
            new GraphQLSourceSchemaAnnotation
            {
                SourceSchemaName = schemaName,
                ExportSchemaName = schemaName,
                ExportConfiguration = configuration,
                ExportTargetFramework = targetFramework,
                ExportRuntimeIdentifier = runtimeIdentifier,
                ExportTimeout = timeout,
                Location = SourceSchemaLocationType.CommandLineExport
            });

        return builder;
    }

    /// <summary>
    /// Marks a resource as needing GraphQL schema composition from its referenced subgraphs.
    /// </summary>
    /// <param name="builder">The resource builder</param>
    /// <param name="outputFileName">The output schema file name (defaults to "gateway.fgp")</param>
    /// <param name="settings">The composition settings.</param>
    /// <returns>The resource builder for chaining</returns>
    public static IResourceBuilder<T> WithGraphQLSchemaComposition<T>(
        this IResourceBuilder<T> builder,
        string outputFileName = "gateway.far",
        GraphQLCompositionSettings settings = default)
        where T : IResourceWithEndpoints
    {
        builder.WithAnnotation(
            new GraphQLSchemaCompositionAnnotation
            {
                OutputFileName = outputFileName,
                Settings = settings
            });

        NitroExtensions.TryAddAutoUpdateCommands(builder);

        if (!builder.Resource.Annotations
            .OfType<ResourceCommandAnnotation>()
            .Any(command => command.Name == "recompose"))
        {
            var resourceName = builder.Resource.Name;

            builder.WithCommand(
                "recompose",
                "Recompose",
                context => context.ServiceProvider
                    .GetService<GatewayCompositionCommandCoordinator>()?
                    .ExecuteAsync(resourceName, context.CancellationToken)
                        ?? Task.FromResult(CommandResults.Failure("Schema composition is not ready.")),
                new CommandOptions
                {
                    Description = "Recompose and install the gateway schema.",
                    IconName = "ArrowSync",
                    UpdateState = context =>
                    {
                        var state = context.ResourceSnapshot.State?.Text;
                        return state == KnownResourceStates.Running
                            || state == KnownResourceStates.RuntimeUnhealthy
                                ? ResourceCommandState.Enabled
                                : ResourceCommandState.Disabled;
                    }
                });
        }

        return builder;
    }

    internal static string? GetGraphQLSourceSchemaName(this IResource resource)
    {
        var annotation = resource.Annotations.OfType<GraphQLSourceSchemaAnnotation>().FirstOrDefault();
        return annotation?.SourceSchemaName;
    }

    internal static string? GetGraphQLSchemaUrl(
        this IResourceWithEndpoints resource,
        string defaultPath,
        string? endpointName = null)
    {
        var annotation = resource.Annotations.OfType<GraphQLSourceSchemaAnnotation>().FirstOrDefault();
        if (annotation is not { Location: SourceSchemaLocationType.SchemaEndpoint })
        {
            return null;
        }

        var targetEndpointName = endpointName ?? annotation.EndpointName;
        var endpoint = resource.GetEndpoints().FirstOrDefault(e => e.EndpointName == targetEndpointName);
        if (endpoint?.Url == null)
        {
            return null;
        }

        var baseUrl = endpoint.Url.TrimEnd('/');
        return baseUrl + resource.GetGraphQLSchemaPath(defaultPath);
    }

    internal static string? GetAllocatedHttpEndpointUrl(this IResourceWithEndpoints resource)
    {
        var annotation = resource.Annotations.OfType<GraphQLSourceSchemaAnnotation>().FirstOrDefault();
        var endpointName = annotation?.EndpointName ?? "http";
        var endpoint = resource.GetEndpoints().FirstOrDefault(e => e.EndpointName == endpointName);

        if (endpoint is not { IsAllocated: true })
        {
            return null;
        }

        return endpoint.Url;
    }

    internal static string? GetGraphQLSchemaPath(
        this IResource resource,
        string? defaultPath = null)
    {
        var annotation = resource.Annotations.OfType<GraphQLSourceSchemaAnnotation>().FirstOrDefault();
        return annotation?.SchemaPath ?? defaultPath;
    }

    internal static bool HasGraphQLSchema(this IResource resource)
        => resource.Annotations.OfType<GraphQLSourceSchemaAnnotation>().Any();

    internal static bool NeedsGraphQLSchemaComposition(this IResource resource)
        => resource.Annotations.OfType<GraphQLSchemaCompositionAnnotation>().Any();

    internal static GraphQLSchemaCompositionAnnotation? GetCompositionSettings(this IResource resource)
        => resource.Annotations.OfType<GraphQLSchemaCompositionAnnotation>().FirstOrDefault();

    internal static IEnumerable<IResourceWithEndpoints> GetGraphQLSchemaResources(
        this DistributedApplicationModel appModel)
        => appModel.Resources.OfType<IResourceWithEndpoints>().Where(r => r.HasGraphQLSchema());

    internal static IEnumerable<IResourceWithEndpoints> GetGraphQLCompositionResources(
        this DistributedApplicationModel appModel)
        => appModel.Resources.OfType<IResourceWithEndpoints>().Where(r => r.NeedsGraphQLSchemaComposition());
}
