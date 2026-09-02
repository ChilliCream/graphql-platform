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
    /// Marks a resource as exposing a GraphQL endpoint over HTTP.
    /// </summary>
    /// <param name="builder">The resource builder</param>
    /// <param name="path">
    /// The path of the GraphQL endpoint that the resource serves. It must start with '/'.
    /// </param>
    /// <param name="schemaPath">
    /// The path the schema document is downloaded from. It must start with '/' and must not be
    /// <c>null</c> unless the source schema uses Apollo Federation, which serves its schema
    /// through the GraphQL endpoint at <paramref name="path"/> and ignores this path.
    /// </param>
    /// <param name="endpointName">The endpoint name to use (defaults to "http")</param>
    /// <param name="sourceSchemaName">
    /// An optional source schema name assertion. When specified, it must exactly match the
    /// <c>name</c> in <c>schema-settings.json</c>.
    /// </param>
    /// <returns>The resource builder for chaining</returns>
    /// <remarks>
    /// During Aspire publishing, the endpoint must already be reachable from the artifact runner
    /// and must use a fixed target port. The publishing pipeline does not start the source
    /// resource or allocate a dynamic port.
    /// </remarks>
    [AspireExport]
    public static IResourceBuilder<T> WithGraphQLHttpEndpoint<T>(
        this IResourceBuilder<T> builder,
        string path = "/graphql",
        string? schemaPath = "/graphql/schema.graphql",
        string endpointName = "http",
        string? sourceSchemaName = null)
        where T : IResourceWithEndpoints
    {
        if (!path.StartsWith('/'))
        {
            throw new ArgumentException(
                "The GraphQL endpoint path must start with '/'.",
                nameof(path));
        }

        if (schemaPath?.StartsWith('/') is false)
        {
            throw new ArgumentException(
                "The GraphQL schema endpoint path must start with '/'.",
                nameof(schemaPath));
        }

        builder.WithAnnotation(
            new GraphQLSourceSchemaAnnotation
            {
                SourceSchemaName = sourceSchemaName,
                EndpointName = endpointName,
                SchemaPath = schemaPath,
                GraphQLPath = path,
                Location = SourceSchemaLocationType.SchemaEndpoint
            },
            ResourceAnnotationMutationBehavior.Replace);

        return builder;
    }

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
    [Obsolete(
        "Use WithGraphQLHttpEndpoint instead, which declares the GraphQL route of the resource "
        + "in addition to the schema download path.")]
    [AspireExportIgnore(Reason = "Superseded by WithGraphQLHttpEndpoint.")]
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
            },
            ResourceAnnotationMutationBehavior.Replace);

        return builder;
    }

    /// <summary>
    /// Marks a resource as having a GraphQL schema file in its project directory.
    /// </summary>
    /// <param name="builder">The resource builder</param>
    /// <param name="fileName">The schema file name (defaults to "schema.graphqls")</param>
    /// <param name="sourceSchemaName">The source schema name (defaults to the resource name)</param>
    /// <returns>The resource builder for chaining</returns>
    [Obsolete(
        "File based source schemas are being retired. Use WithGraphQLHttpEndpoint instead, "
        + "which fetches the source schema from the endpoint of the resource.")]
    [AspireExportIgnore(Reason = "Superseded by WithGraphQLHttpEndpoint.")]
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
            },
            ResourceAnnotationMutationBehavior.Replace);

        return builder;
    }

    /// <summary>
    /// Marks a project resource as exporting a GraphQL schema through an Aspire-backed Hot
    /// Chocolate command whenever the integration acquires the source schema.
    /// </summary>
    /// <param name="builder">The project resource builder.</param>
    /// <param name="schemaName">
    /// The registered schema name to export. When omitted, Hot Chocolate's default schema is
    /// exported and its emitted name must match the Aspire resource name.
    /// </param>
    /// <returns>The resource builder for chaining.</returns>
    [AspireExportIgnore(Reason = "The schema export runs the .NET project of the resource.")]
    public static IResourceBuilder<ProjectResource> WithGraphQLSchemaExport(
        this IResourceBuilder<ProjectResource> builder,
        string? schemaName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (schemaName is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);
        }

        builder.WithAnnotation(
            new GraphQLSourceSchemaAnnotation
            {
                SourceSchemaName = schemaName,
                Location = SourceSchemaLocationType.CommandLineExport
            },
            ResourceAnnotationMutationBehavior.Replace);

        GraphQLSchemaExportCommand.Register(builder, schemaName);

        return builder;
    }

    /// <summary>
    /// Marks a resource as needing GraphQL schema composition from its referenced subgraphs.
    /// </summary>
    /// <param name="builder">The resource builder</param>
    /// <param name="disableValidation">
    /// A value indicating whether Nitro schema validation shall be disabled.
    /// </param>
    /// <param name="outputFileName">The output archive file name.</param>
    /// <returns>The resource builder for chaining</returns>
    [AspireExport]
    public static IResourceBuilder<T> WithNitroComposition<T>(
        this IResourceBuilder<T> builder,
        bool disableValidation = false,
        string outputFileName = "gateway.far")
        where T : IResourceWithEndpoints
        => builder.WithNitroComposition(
            new GraphQLCompositionSettings
            {
                DisableSchemaValidation = disableValidation
            },
            outputFileName);

    /// <summary>
    /// Marks a resource as needing GraphQL schema composition from its referenced subgraphs.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="settings">
    /// The composition settings. Composition settings normally come from Nitro and the settings
    /// given here override them locally.
    /// </param>
    /// <param name="outputFileName">The output archive file name.</param>
    /// <returns>The resource builder for chaining.</returns>
    [Obsolete("Use WithNitroComposition instead.")]
    [AspireExportIgnore(Reason = "Obsolete alias for WithNitroComposition.")]
    public static IResourceBuilder<T> WithGraphQLSchemaComposition<T>(
        this IResourceBuilder<T> builder,
        GraphQLCompositionSettings settings,
        string outputFileName = "gateway.far")
        where T : IResourceWithEndpoints
        => builder.WithNitroComposition(settings, outputFileName);

    /// <summary>
    /// Marks a resource as needing GraphQL schema composition from its referenced subgraphs.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="settings">
    /// The composition settings. Composition settings normally come from Nitro and the settings
    /// given here override them locally.
    /// </param>
    /// <param name="outputFileName">The output archive file name.</param>
    /// <returns>The resource builder for chaining.</returns>
    [AspireExportIgnore(Reason = "Composition settings are provided by Nitro.")]
    public static IResourceBuilder<T> WithNitroComposition<T>(
        this IResourceBuilder<T> builder,
        GraphQLCompositionSettings settings,
        string outputFileName = "gateway.far")
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

    /// <summary>
    /// Builds the URL that the source schema document of a resource is downloaded from.
    /// <paramref name="endpointName"/> overrides the endpoint that the resource declares, which
    /// the publishing pipeline uses. An endpoint that is not allocated resolves against its
    /// declared target port, because the publishing pipeline never starts the resource.
    /// </summary>
    internal static string? GetGraphQLSchemaUrl(
        this IResourceWithEndpoints resource,
        string path,
        string? endpointName = null)
    {
        var annotation = resource.Annotations.OfType<GraphQLSourceSchemaAnnotation>().FirstOrDefault();
        if (annotation is not { Location: SourceSchemaLocationType.SchemaEndpoint })
        {
            return null;
        }

        var targetEndpointName = endpointName ?? annotation.EndpointName;
        var endpoint = resource.GetEndpoints().FirstOrDefault(e => e.EndpointName == targetEndpointName);
        if (endpoint is null)
        {
            return null;
        }

        if (!endpoint.IsAllocated)
        {
            var endpointAnnotation = endpoint.EndpointAnnotation;
            var port = endpointAnnotation.TargetPort ?? endpointAnnotation.Port;
            if (port is null || string.IsNullOrWhiteSpace(endpointAnnotation.UriScheme))
            {
                return null;
            }

            var host = string.IsNullOrWhiteSpace(endpointAnnotation.TargetHost)
                ? "localhost"
                : endpointAnnotation.TargetHost;
            var uri = new UriBuilder(
                endpointAnnotation.UriScheme,
                host,
                port.Value);
            return uri.Uri.GetLeftPart(UriPartial.Authority) + path;
        }

        if (endpoint.Url is null)
        {
            return null;
        }

        return endpoint.Url.TrimEnd('/') + path;
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
