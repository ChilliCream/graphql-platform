using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using HotChocolate.Fusion.Aspire.Nitro;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotChocolate.Fusion.Aspire;

/// <summary>
/// Provides extension methods that connect a distributed application to Nitro.
/// </summary>
public static class NitroExtensions
{
    private const string NitroResourceName = "nitro";

    /// <summary>
    /// Adds Nitro and GraphQL schema composition orchestration to the distributed application.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <returns>The Nitro resource builder.</returns>
    public static IResourceBuilder<NitroResource> AddNitro(
        this IDistributedApplicationBuilder builder)
        => AddNitroCore(builder, portalUrl: null, configureSeedUpdates: null);

    /// <summary>
    /// Adds Nitro and configures the Nitro portal URL shown on Nitro-backed gateways.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="portalUrl">
    /// The Nitro portal URL. When <see langword="null"/>, it is derived from the effective Nitro
    /// API URL.
    /// </param>
    /// <returns>The Nitro resource builder.</returns>
    public static IResourceBuilder<NitroResource> AddNitro(
        this IDistributedApplicationBuilder builder,
        Uri? portalUrl)
        => AddNitroCore(builder, portalUrl, configureSeedUpdates: null);

    /// <summary>
    /// Adds Nitro and configures how Fusion Aspire follows stage changes during an AppHost run.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="portalUrl">
    /// The Nitro portal URL. When <see langword="null"/>, it is derived from the effective Nitro
    /// API URL.
    /// </param>
    /// <param name="configureSeedUpdates">
    /// Configures background stage-change subscriptions and automatic adoption.
    /// </param>
    /// <returns>The Nitro resource builder.</returns>
    public static IResourceBuilder<NitroResource> AddNitro(
        this IDistributedApplicationBuilder builder,
        Uri? portalUrl,
        Action<NitroSeedUpdateOptions> configureSeedUpdates)
    {
        ArgumentNullException.ThrowIfNull(configureSeedUpdates);
        return AddNitroCore(builder, portalUrl, configureSeedUpdates);
    }

    private static IResourceBuilder<NitroResource> AddNitroCore(
        IDistributedApplicationBuilder builder,
        Uri? portalUrl,
        Action<NitroSeedUpdateOptions>? configureSeedUpdates)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ValidatePortalUrl(portalUrl);

        var existing = builder.Resources.OfType<NitroResource>().SingleOrDefault();
        if (existing is not null)
        {
            if (portalUrl is not null
                && existing.PortalUrl is not null
                && existing.PortalUrl != portalUrl)
            {
                throw new InvalidOperationException(
                    $"Nitro is already added with the portal URL '{existing.PortalUrl}'.");
            }

            existing.PortalUrl ??= portalUrl;
            configureSeedUpdates?.Invoke(existing.SeedUpdates);
            SchemaCompositionRegistration.Ensure(builder);
            EnsureFusionPipeline(builder);
            return builder.CreateResourceBuilder(existing);
        }

        var configuredCloudUrl =
            builder.Configuration["Nitro:CloudUrl"]
            ?? builder.Configuration["NITRO_CLOUD_URL"];
        var resource = new NitroResource(NitroResourceName)
        {
            CloudUrl = string.IsNullOrWhiteSpace(configuredCloudUrl)
                ? null
                : NormalizeCloudUrl(configuredCloudUrl),
            PortalUrl = portalUrl
        };
        configureSeedUpdates?.Invoke(resource.SeedUpdates);

        var resourceBuilder = builder.AddResource(resource);
        SchemaCompositionRegistration.Ensure(builder);
        EnsureFusionPipeline(builder);

        return resourceBuilder;
    }

    /// <summary>
    /// Adds an API declaration to Nitro.
    /// </summary>
    /// <param name="builder">The Nitro resource builder.</param>
    /// <param name="name">The declarative name of the API.</param>
    /// <returns>The Nitro API resource builder.</returns>
    public static IResourceBuilder<NitroApiResource> AddApi(
        this IResourceBuilder<NitroResource> builder,
        string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (builder.ApplicationBuilder.Resources
            .OfType<NitroApiResource>()
            .Any(api => ReferenceEquals(api.Nitro, builder.Resource)
                && string.Equals(api.ApiName, name, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                $"Nitro already declares an API named '{name}'.");
        }

        var resource = new NitroApiResource(
            $"{builder.Resource.Name}-{name}",
            name,
            builder.Resource);

        return builder.ApplicationBuilder
            .AddResource(resource)
            .WithParentRelationship(builder);
    }

    /// <summary>
    /// Sets the Nitro ID of an API declaration.
    /// </summary>
    /// <param name="builder">The Nitro API resource builder.</param>
    /// <param name="apiId">The ID reported by the Nitro dashboard and CLI.</param>
    /// <returns>The Nitro API resource builder for chaining.</returns>
    public static IResourceBuilder<NitroApiResource> WithNitroApiId(
        this IResourceBuilder<NitroApiResource> builder,
        string apiId)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiId);

        builder.Resource.ApiId = apiId;
        return builder;
    }

    /// <summary>
    /// Adds a stage declaration to a Nitro API.
    /// </summary>
    /// <param name="builder">The Nitro API resource builder.</param>
    /// <param name="stageName">The Nitro stage name.</param>
    /// <returns>The Nitro stage resource builder.</returns>
    public static IResourceBuilder<NitroStageResource> AddStage(
        this IResourceBuilder<NitroApiResource> builder,
        string stageName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(stageName);

        if (builder.ApplicationBuilder.Resources
            .OfType<NitroStageResource>()
            .Any(stage => ReferenceEquals(stage.Api, builder.Resource)
                && string.Equals(stage.StageName, stageName, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                $"Nitro API '{builder.Resource.ApiName}' already declares the stage "
                + $"'{stageName}'.");
        }

        var resource = new NitroStageResource(
            $"{builder.Resource.Name}-{stageName}",
            stageName,
            builder.Resource);

        return builder.ApplicationBuilder
            .AddResource(resource)
            .WithParentRelationship(builder);
    }

    /// <summary>
    /// Selects the Nitro stage whose Fusion configuration is the base of a gateway composition.
    /// </summary>
    /// <typeparam name="T">The gateway resource type.</typeparam>
    /// <param name="builder">The gateway resource builder.</param>
    /// <param name="stage">The Nitro stage resource builder.</param>
    /// <returns>The gateway resource builder for chaining.</returns>
    public static IResourceBuilder<T> WithNitroCompositionBase<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<NitroStageResource> stage)
        where T : IResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(stage);

        var existing = builder.Resource.Annotations
            .OfType<NitroCompositionBaseAnnotation>()
            .SingleOrDefault();
        if (existing is not null && !ReferenceEquals(existing.Stage, stage.Resource))
        {
            throw new InvalidOperationException(
                $"Resource '{builder.Resource.Name}' already uses Nitro stage "
                + $"'{existing.Stage.StageName}' as its composition base.");
        }

        if (existing is null)
        {
            builder.WithAnnotation(new NitroCompositionBaseAnnotation { Stage = stage.Resource });
            builder.WithReferenceRelationship(stage);
        }

        TryAddAutoUpdateCommands(builder);
        return builder;
    }

    /// <summary>
    /// Sets the Nitro API URL used by local composition and publishing.
    /// </summary>
    /// <param name="builder">The Nitro resource builder.</param>
    /// <param name="cloudUrl">An absolute HTTPS origin.</param>
    /// <returns>The Nitro resource builder for chaining.</returns>
    public static IResourceBuilder<NitroResource> WithNitroCloudUrl(
        this IResourceBuilder<NitroResource> builder,
        string cloudUrl)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Resource.CloudUrl = NormalizeCloudUrl(cloudUrl);
        return builder;
    }

    /// <summary>
    /// Sets the secret parameter that supplies the Nitro API key.
    /// </summary>
    /// <param name="builder">The Nitro resource builder.</param>
    /// <param name="apiKey">A parameter declared as a secret.</param>
    /// <returns>The Nitro resource builder for chaining.</returns>
    public static IResourceBuilder<NitroResource> WithNitroApiKey(
        this IResourceBuilder<NitroResource> builder,
        IResourceBuilder<ParameterResource> apiKey)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(apiKey);

        if (!apiKey.Resource.Secret)
        {
            throw new ArgumentException(
                "The Nitro API key parameter must be declared as a secret.",
                nameof(apiKey));
        }

        builder.Resource.ApiKey = apiKey.Resource;
        return builder;
    }

    /// <summary>
    /// Sets the Nitro portal URL shown on Nitro-backed gateways.
    /// </summary>
    /// <param name="builder">The Nitro resource builder.</param>
    /// <param name="portalUrl">The absolute Nitro portal URL.</param>
    /// <returns>The Nitro resource builder for chaining.</returns>
    public static IResourceBuilder<NitroResource> WithNitroPortalUrl(
        this IResourceBuilder<NitroResource> builder,
        Uri portalUrl)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ValidatePortalUrl(portalUrl);
        builder.Resource.PortalUrl = portalUrl;
        return builder;
    }

    /// <summary>
    /// Configures whether Nitro waits for approval before a stage publication is committed.
    /// </summary>
    /// <param name="builder">The Nitro stage resource builder.</param>
    /// <param name="waitForApproval">Whether publication waits for approval.</param>
    /// <returns>The Nitro stage resource builder for chaining.</returns>
    public static IResourceBuilder<NitroStageResource> WithApproval(
        this IResourceBuilder<NitroStageResource> builder,
        bool waitForApproval)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Resource.WaitForApproval = waitForApproval;
        return builder;
    }

    /// <summary>
    /// Configures whether publication proceeds despite validation failures.
    /// </summary>
    /// <param name="builder">The Nitro stage resource builder.</param>
    /// <param name="force">Whether validation failures may be forced.</param>
    /// <returns>The Nitro stage resource builder for chaining.</returns>
    public static IResourceBuilder<NitroStageResource> WithForcePublish(
        this IResourceBuilder<NitroStageResource> builder,
        bool force)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Resource.Force = force;
        return builder;
    }

    internal static NitroStageResource? GetNitroCompositionBase(this IResource resource)
        => resource.Annotations
            .OfType<NitroCompositionBaseAnnotation>()
            .SingleOrDefault()
            ?.Stage;

    internal static void TryAddAutoUpdateCommands<T>(IResourceBuilder<T> builder)
        where T : IResource
    {
        if (!builder.Resource.NeedsGraphQLSchemaComposition()
            || builder.Resource.GetNitroCompositionBase() is null)
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

    private static void EnsureFusionPipeline(IDistributedApplicationBuilder builder)
    {
        if (builder.Resources.OfType<FusionPipelineResource>().Any())
        {
            return;
        }

        builder.Services.TryAddSingleton(_ => NitroFusionApi.Create());
        builder.Services.TryAddSingleton<FusionDeploymentWorkflow>();

        var pipeline = builder.AddResource(
            new FusionPipelineResource("fusion-nitro-pipeline"));
        FusionPipeline.Configure(pipeline);
    }

    private static void ValidatePortalUrl(Uri? portalUrl)
    {
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
    }

    private static string NormalizeCloudUrl(string cloudUrl)
    {
        if (!Uri.TryCreate(cloudUrl, UriKind.Absolute, out var uri)
            || uri.Scheme is not "https"
            || !string.IsNullOrEmpty(uri.UserInfo)
            || uri.AbsolutePath is not "/"
            || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment))
        {
            throw new ArgumentException(
                "The Nitro cloud URL must be an absolute HTTPS origin without "
                + "a path, query, fragment, or user information.",
                nameof(cloudUrl));
        }

        return uri.GetLeftPart(UriPartial.Authority);
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
