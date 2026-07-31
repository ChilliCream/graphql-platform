using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using HotChocolate.Fusion.Aspire.Nitro;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Aspire;

/// <summary>
/// Provides the extension methods that connect a distributed application to Nitro.
/// </summary>
public static class NitroExtensions
{
    /// <summary>
    /// Adds GraphQL schema composition orchestration to the distributed application. Every gateway
    /// composes the source schemas of the distributed application.
    /// </summary>
    /// <param name="builder">
    /// The distributed application builder.
    /// </param>
    /// <returns>
    /// The distributed application builder for chaining.
    /// </returns>
    public static IDistributedApplicationBuilder AddNitro(
        this IDistributedApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        SchemaCompositionRegistration.Ensure(builder);

        return builder;
    }

    /// <summary>
    /// Adds GraphQL schema composition orchestration that composes against the fusion
    /// configurations of Nitro and configures the Nitro portal URL shown on each Nitro-composed
    /// gateway. A gateway configured with <see cref="WithNitroApiId{T}"/> composes the source
    /// schemas of the distributed application on top of the fusion configuration that Nitro
    /// serves for <paramref name="stage"/>, so it also serves source schemas that run outside of
    /// the distributed application.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="stage">
    /// The Nitro stage whose fusion configuration is used. The settings of the source schemas
    /// carried by that configuration resolve against this stage environment.
    /// </param>
    /// <param name="portalUrl">
    /// An optional Nitro portal URL. When omitted, the URL is derived from the effective Nitro API
    /// URL.
    /// </param>
    /// <returns>The distributed application builder for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Nitro is already added for another stage or portal URL.
    /// </exception>
    /// <remarks>
    /// A source schema of the distributed application replaces the source schema of the same name
    /// in the fusion configuration. The name of a source schema declared with
    /// <see cref="GraphQLResourceBuilderExtensions.WithGraphQLSchemaEndpoint{T}"/> is the
    /// <c>name</c> in its settings file. The name of a source schema declared with
    /// <see cref="GraphQLResourceBuilderExtensions.WithGraphQLSchemaFile{T}"/> is the configured
    /// source schema name or the resource name, and is not checked against its settings file. A
    /// source schema that ends up with another name is added to the composition instead of
    /// replacing the one in the fusion configuration.
    /// </remarks>
    public static IDistributedApplicationBuilder AddNitro(
        this IDistributedApplicationBuilder builder,
        string stage,
        Uri? portalUrl = null)
        => AddNitroCore(builder, stage, portalUrl, configureSeedUpdates: null);

    /// <summary>
    /// Adds GraphQL schema composition orchestration that composes against Nitro and configures
    /// how Fusion Aspire follows changes to the selected stage during the AppHost run.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="stage">The Nitro stage whose Fusion configuration is used.</param>
    /// <param name="portalUrl">
    /// An optional Nitro portal URL. When omitted, the URL is derived from the effective Nitro API
    /// URL.
    /// </param>
    /// <param name="configureSeedUpdates">
    /// Configures background stage-change subscriptions, current-version queries, Fusion
    /// configuration downloads, and automatic adoption.
    /// </param>
    /// <returns>The distributed application builder for chaining.</returns>
    /// <remarks>
    /// Stage update detection receives stage-change metadata and downloads the same Fusion archive
    /// that startup seed acquisition downloads. It sends no schema or configuration data to Nitro.
    /// </remarks>
    public static IDistributedApplicationBuilder AddNitro(
        this IDistributedApplicationBuilder builder,
        string stage,
        Uri? portalUrl,
        Action<NitroSeedUpdateOptions> configureSeedUpdates)
    {
        ArgumentNullException.ThrowIfNull(configureSeedUpdates);

        return AddNitroCore(builder, stage, portalUrl, configureSeedUpdates);
    }

    private static IDistributedApplicationBuilder AddNitroCore(
        IDistributedApplicationBuilder builder,
        string stage,
        Uri? portalUrl,
        Action<NitroSeedUpdateOptions>? configureSeedUpdates)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(stage);

        if (portalUrl?.IsAbsoluteUri is false
            || portalUrl is not null
                && !string.Equals(
                    portalUrl.Scheme,
                    Uri.UriSchemeHttp,
                    StringComparison.OrdinalIgnoreCase)
                && !string.Equals(
                    portalUrl.Scheme,
                    Uri.UriSchemeHttps,
                    StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrEmpty(portalUrl?.UserInfo))
        {
            throw new ArgumentException(
                "The Nitro portal URL must be an absolute HTTP URL without user information.",
                nameof(portalUrl));
        }

        var options = SchemaCompositionRegistration.Ensure(builder);

        if (options.Coordinator is { } coordinator)
        {
            if (!string.Equals(coordinator.Stage, stage, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Nitro is already added for the stage '{coordinator.Stage}'. A distributed "
                    + "application composes against a single stage, so AddNitro cannot be called "
                    + $"again for the stage '{stage}'.");
            }

            if (portalUrl is not null
                && options.PortalUrl is not null
                && options.PortalUrl != portalUrl)
            {
                throw new InvalidOperationException(
                    $"Nitro is already added with the portal URL '{options.PortalUrl}'.");
            }

            configureSeedUpdates?.Invoke(options.SeedUpdates);
            coordinator.SetInitialAutoUpdate(options.SeedUpdates.AutoUpdate);
            options.PortalUrl ??= portalUrl;
            AddAutoUpdateCommandsToConfiguredGateways(builder);
            return builder;
        }

        configureSeedUpdates?.Invoke(options.SeedUpdates);
        options.Coordinator = NitroSeedCoordinator.CreateProduction(
            stage,
            options.SeedUpdates.AutoUpdate);
        options.PortalUrl = portalUrl;
        AddAutoUpdateCommandsToConfiguredGateways(builder);

        return builder;
    }

    /// <summary>
    /// Selects the Nitro api that carries the fusion configuration of a gateway. The api id is the
    /// id that the Nitro dashboard and the Nitro CLI report for the api. Calling this method again
    /// replaces the previously configured api id.
    /// </summary>
    /// <param name="builder">
    /// The resource builder of a gateway.
    /// </param>
    /// <param name="apiId">
    /// The id of the Nitro api.
    /// </param>
    /// <returns>
    /// The resource builder for chaining.
    /// </returns>
    /// <remarks>
    /// The api id only takes effect on a resource whose schema is composed and only when the
    /// distributed application calls
    /// <see cref="AddNitro(IDistributedApplicationBuilder, string, Uri)"/>.
    /// On any other resource it is metadata
    /// without an effect.
    /// </remarks>
    public static IResourceBuilder<T> WithNitroApiId<T>(
        this IResourceBuilder<T> builder,
        string apiId)
        where T : IResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiId);

        builder.WithAnnotation(
            new NitroApiIdAnnotation { ApiId = apiId },
            ResourceAnnotationMutationBehavior.Replace);
        TryAddAutoUpdateCommands(builder);

        return builder;
    }

    internal static string? GetNitroApiId(this IResource resource)
        => resource.Annotations.OfType<NitroApiIdAnnotation>().SingleOrDefault()?.ApiId;

    internal static void TryAddAutoUpdateCommands<T>(IResourceBuilder<T> builder)
        where T : IResource
    {
        if (!builder.Resource.NeedsGraphQLSchemaComposition()
            || builder.Resource.GetNitroApiId() is null
            || SchemaCompositionRegistration.GetOptions(builder.ApplicationBuilder)?.Coordinator
                is null)
        {
            return;
        }

        AddAutoUpdateCommand(
            builder,
            "disable-nitro-auto-update",
            "Disable auto-update",
            enabled: false);
        AddAutoUpdateCommand(
            builder,
            "enable-nitro-auto-update",
            "Enable auto-update",
            enabled: true);
    }

    private static void AddAutoUpdateCommandsToConfiguredGateways(
        IDistributedApplicationBuilder builder)
    {
        foreach (var resource in builder.Resources.OfType<IResourceWithEndpoints>())
        {
            if (resource.NeedsGraphQLSchemaComposition()
                && resource.GetNitroApiId() is not null)
            {
                TryAddAutoUpdateCommands(builder.CreateResourceBuilder(resource));
            }
        }
    }

    private static void AddAutoUpdateCommand<T>(
        IResourceBuilder<T> builder,
        string name,
        string displayName,
        bool enabled)
        where T : IResource
    {
        if (builder.Resource.Annotations
            .OfType<ResourceCommandAnnotation>()
            .Any(command => command.Name == name))
        {
            return;
        }

        var resourceName = builder.Resource.Name;

        builder.WithCommand(
            name,
            displayName,
            context => context.ServiceProvider
                .GetService<NitroSeedUpdateService>()?
                .SetAutoUpdateAsync(
                    resourceName,
                    enabled,
                    context.CancellationToken)
                ?? Task.FromResult(
                    CommandResults.Failure("Nitro stage update monitoring is not ready.")),
            new CommandOptions
            {
                Description = enabled
                    ? "Apply staged Nitro updates and resume automatic updates."
                    : "Stage Nitro updates until the next local recomposition.",
                IconName = enabled ? "ArrowSyncCheckmark" : "ArrowSyncOff",
                UpdateState = context =>
                {
                    var service = context.ServiceProvider.GetService<NitroSeedUpdateService>();
                    if (service is null
                        || !service.IsEnabled
                        || !service.IsReady(resourceName))
                    {
                        return ResourceCommandState.Hidden;
                    }

                    return service.IsAutoUpdateEnabled(resourceName) != enabled
                        ? ResourceCommandState.Enabled
                        : ResourceCommandState.Hidden;
                }
            });
    }
}
