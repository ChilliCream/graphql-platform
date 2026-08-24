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
    /// composes the source schemas of the distributed application, and a gateway configured with
    /// <see cref="WithNitroApiId{T}"/> composes them on top of the fusion configuration that Nitro
    /// serves for <paramref name="stage"/>, so it also serves source schemas that run outside of
    /// the distributed application.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="stage">
    /// The Nitro stage whose fusion configuration is used. The settings of the source schemas
    /// carried by that configuration resolve against this stage environment. When it is
    /// <c>null</c>, the distributed application composes only its own source schemas and the
    /// remaining arguments must be omitted.
    /// </param>
    /// <param name="portalUrl">
    /// An optional Nitro portal URL. When omitted, the URL is derived from the effective Nitro API
    /// URL.
    /// </param>
    /// <param name="seedUpdates">
    /// The settings for background stage-change subscriptions, current-version queries, fusion
    /// configuration downloads, and automatic adoption. When omitted, the previously configured
    /// settings stay in effect.
    /// </param>
    /// <returns>The distributed application builder for chaining.</returns>
    /// <exception cref="ArgumentException">
    /// The stage is empty or white space, the portal URL is not an absolute HTTP URL without user
    /// information, or a portal URL or seed update settings are given without a stage.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Nitro is already added for another stage or portal URL.
    /// </exception>
    /// <remarks>
    /// A source schema of the distributed application replaces the source schema of the same name
    /// in the fusion configuration. The name of a source schema declared with
    /// <see cref="GraphQLResourceBuilderExtensions.WithGraphQLHttpEndpoint{T}"/> is the
    /// <c>name</c> in its settings file. A source schema that ends up with another name is added
    /// to the composition instead of replacing the one in the fusion configuration.
    /// </remarks>
    [AspireExport]
    public static IDistributedApplicationBuilder AddNitroComposition(
        this IDistributedApplicationBuilder builder,
        string? stage = null,
        Uri? portalUrl = null,
        NitroSeedUpdateOptions? seedUpdates = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (stage is null)
        {
            if (portalUrl is not null)
            {
                throw new ArgumentException(
                    "The Nitro portal URL can only be set together with a stage.",
                    nameof(portalUrl));
            }

            if (seedUpdates is not null)
            {
                throw new ArgumentException(
                    "The Nitro seed update settings can only be set together with a stage.",
                    nameof(seedUpdates));
            }

            SchemaCompositionRegistration.Ensure(builder);

            return builder;
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(stage);

        if (portalUrl?.IsAbsoluteUri is false
            || (portalUrl is not null
                && !string.Equals(
                    portalUrl.Scheme,
                    Uri.UriSchemeHttp,
                    StringComparison.OrdinalIgnoreCase)
                && !string.Equals(
                    portalUrl.Scheme,
                    Uri.UriSchemeHttps,
                    StringComparison.OrdinalIgnoreCase))
            || !string.IsNullOrEmpty(portalUrl?.UserInfo))
        {
            throw new ArgumentException(
                "The Nitro portal URL must be an absolute HTTP URL without user information.",
                nameof(portalUrl));
        }

        var options = SchemaCompositionRegistration.Ensure(builder);
        var coordinator = options.Coordinator;

        if (coordinator is not null)
        {
            if (!string.Equals(coordinator.Stage, stage, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Nitro is already added for the stage '{coordinator.Stage}'. A distributed "
                    + "application composes against a single stage, so AddNitroComposition cannot "
                    + $"be called again for the stage '{stage}'.");
            }

            if (portalUrl is not null
                && options.PortalUrl is not null
                && options.PortalUrl != portalUrl)
            {
                throw new InvalidOperationException(
                    $"Nitro is already added with the portal URL '{options.PortalUrl}'.");
            }
        }

        if (seedUpdates is not null)
        {
            options.SeedUpdates = seedUpdates;
        }

        if (coordinator is null)
        {
            options.Coordinator = NitroSeedCoordinator.CreateProduction(
                stage,
                options.SeedUpdates.AutoUpdate);
            options.PortalUrl = portalUrl;
        }
        else
        {
            coordinator.SetInitialAutoUpdate(options.SeedUpdates.AutoUpdate);
            options.PortalUrl ??= portalUrl;
        }

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
    /// distributed application composes against a Nitro stage.
    /// </remarks>
    [AspireExport]
    public static IResourceBuilder<T> WithNitroApiId<T>(
        this IResourceBuilder<T> builder,
        string apiId)
        where T : IResourceWithEndpoints
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
