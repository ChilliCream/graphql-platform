using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using HotChocolate.Fusion.Aspire.Nitro;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
    /// On a gateway the api id selects the fusion configuration that the gateway composes on top
    /// of, and it only takes effect when the distributed application calls
    /// <see cref="AddNitro(IDistributedApplicationBuilder, string, Uri)"/>. On a publish target
    /// added with <see cref="AddNitroPublishTarget"/> the api id selects the api that Fusion
    /// deployments publish to. On any other resource it is metadata without an effect.
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

    /// <summary>
    /// Adds the Nitro api that the Fusion deployments of the distributed application publish to.
    /// The cloud URL and the api id default to the <c>Nitro:CloudUrl</c> and <c>Nitro:ApiId</c>
    /// configuration values, or to the <c>NITRO_CLOUD_URL</c> and <c>NITRO_API_ID</c> environment
    /// variables.
    /// </summary>
    /// <param name="builder">
    /// The distributed application builder.
    /// </param>
    /// <param name="name">
    /// The name of the publish target resource.
    /// </param>
    /// <returns>
    /// The resource builder of the publish target for chaining.
    /// </returns>
    public static IResourceBuilder<NitroPublishTargetResource> AddNitroPublishTarget(
        this IDistributedApplicationBuilder builder,
        string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        EnsureFusionPipeline(builder);
        builder.Services.TryAddSingleton(_ => NitroFusionApi.Create());
        builder.Services.TryAddSingleton<FusionDeploymentWorkflow>();

        var configuredCloudUrl =
            builder.Configuration["Nitro:CloudUrl"]
            ?? builder.Configuration["NITRO_CLOUD_URL"];
        var resource = new NitroPublishTargetResource(name)
        {
            CloudUrl = string.IsNullOrWhiteSpace(configuredCloudUrl)
                ? null
                : NormalizeCloudUrl(configuredCloudUrl)
        };
        var resourceBuilder = builder.AddResource(resource);

        var configuredApiId =
            builder.Configuration["Nitro:ApiId"]
            ?? builder.Configuration["NITRO_API_ID"];

        if (!string.IsNullOrWhiteSpace(configuredApiId))
        {
            resourceBuilder.WithNitroApiId(configuredApiId);
        }

        return resourceBuilder;
    }

    /// <summary>
    /// Sets the Nitro api URL that Fusion deployments publish to.
    /// </summary>
    /// <param name="builder">
    /// The resource builder of a Nitro publish target.
    /// </param>
    /// <param name="cloudUrl">
    /// An absolute HTTPS origin without a path, query, fragment, or user information.
    /// </param>
    /// <returns>
    /// The resource builder for chaining.
    /// </returns>
    public static IResourceBuilder<NitroPublishTargetResource> WithNitroCloudUrl(
        this IResourceBuilder<NitroPublishTargetResource> builder,
        string cloudUrl)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Resource.CloudUrl = NormalizeCloudUrl(cloudUrl);
        return builder;
    }

    /// <summary>
    /// Sets the secret parameter that supplies the Nitro api key. When this is not configured, the
    /// credential is resolved from the <c>NITRO_API_KEY</c> environment variable or the Nitro CLI
    /// session.
    /// </summary>
    /// <param name="builder">
    /// The resource builder of a Nitro publish target.
    /// </param>
    /// <param name="apiKey">
    /// The parameter that supplies the api key. It has to be declared as a secret.
    /// </param>
    /// <returns>
    /// The resource builder for chaining.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// The parameter is not declared as a secret.
    /// </exception>
    public static IResourceBuilder<NitroPublishTargetResource> WithNitroApiKey(
        this IResourceBuilder<NitroPublishTargetResource> builder,
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
    /// Declares a Nitro stage that this api publishes to. Every stage an api publishes to is
    /// declared once, and each invocation selects one of them by name.
    /// </summary>
    /// <param name="builder">
    /// The resource builder of a Nitro publish target.
    /// </param>
    /// <param name="stageName">
    /// The name of the Nitro stage.
    /// </param>
    /// <returns>
    /// The resource builder of the stage for chaining.
    /// </returns>
    public static IResourceBuilder<FusionStageResource> AddStage(
        this IResourceBuilder<NitroPublishTargetResource> builder,
        string stageName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(stageName);

        var resource = new FusionStageResource(
            $"{builder.Resource.Name}-{stageName}",
            stageName,
            builder.Resource);
        return builder.ApplicationBuilder.AddResource(resource);
    }

    /// <summary>
    /// Sets the parameter that names the declared stage an invocation publishes to.
    /// </summary>
    /// <param name="builder">
    /// The resource builder of a Nitro publish target.
    /// </param>
    /// <param name="stage">
    /// The parameter that supplies the name of the stage.
    /// </param>
    /// <returns>
    /// The resource builder for chaining.
    /// </returns>
    /// <remarks>
    /// The publication fails when the parameter names a stage that the api does not declare.
    /// </remarks>
    public static IResourceBuilder<NitroPublishTargetResource> WithStageParameter(
        this IResourceBuilder<NitroPublishTargetResource> builder,
        IResourceBuilder<ParameterResource> stage)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(stage);

        builder.Resource.StageParameter = stage.Resource;
        return builder;
    }

    /// <summary>
    /// Selects the exact <c>schema-settings.json</c> environment used for composition.
    /// </summary>
    /// <param name="builder">
    /// The resource builder of a Fusion stage.
    /// </param>
    /// <param name="environmentName">
    /// The name of the settings environment.
    /// </param>
    /// <returns>
    /// The resource builder for chaining.
    /// </returns>
    /// <remarks>
    /// When this is not configured, an environment selected by the GraphQL composition is used,
    /// followed by the Nitro stage name.
    /// </remarks>
    public static IResourceBuilder<FusionStageResource> WithCompositionEnvironment(
        this IResourceBuilder<FusionStageResource> builder,
        string environmentName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentName);

        builder.Resource.CompositionEnvironmentName = environmentName;
        return builder;
    }

    /// <summary>
    /// Sets the immutable release tag. The tag is both the source schema version and the fusion
    /// configuration tag of the deployment.
    /// </summary>
    /// <param name="builder">
    /// The resource builder of a Nitro publish target.
    /// </param>
    /// <param name="configurationTag">
    /// The parameter that supplies the release tag.
    /// </param>
    /// <returns>
    /// The resource builder for chaining.
    /// </returns>
    public static IResourceBuilder<NitroPublishTargetResource> WithConfigurationTag(
        this IResourceBuilder<NitroPublishTargetResource> builder,
        IResourceBuilder<ParameterResource> configurationTag)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configurationTag);

        builder.Resource.ConfigurationTagParameter = configurationTag.Resource;
        builder.Resource.ConfigurationTag = null;
        return builder;
    }

    /// <summary>
    /// Sets the immutable release tag. The tag is both the source schema version and the fusion
    /// configuration tag of the deployment.
    /// </summary>
    /// <param name="builder">
    /// The resource builder of a Nitro publish target.
    /// </param>
    /// <param name="configurationTag">
    /// The release tag.
    /// </param>
    /// <returns>
    /// The resource builder for chaining.
    /// </returns>
    public static IResourceBuilder<NitroPublishTargetResource> WithConfigurationTag(
        this IResourceBuilder<NitroPublishTargetResource> builder,
        string configurationTag)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationTag);

        builder.Resource.ConfigurationTag = configurationTag;
        builder.Resource.ConfigurationTagParameter = null;
        return builder;
    }

    /// <summary>
    /// Configures whether Nitro waits for approval before the configuration is committed.
    /// </summary>
    /// <param name="builder">
    /// The resource builder of a Fusion stage.
    /// </param>
    /// <param name="waitForApproval">
    /// Specifies whether the publication waits for approval.
    /// </param>
    /// <returns>
    /// The resource builder for chaining.
    /// </returns>
    public static IResourceBuilder<FusionStageResource> WithApproval(
        this IResourceBuilder<FusionStageResource> builder,
        bool waitForApproval)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Resource.WaitForApproval = waitForApproval;
        return builder;
    }

    /// <summary>
    /// Configures whether validation failures may be forced.
    /// </summary>
    /// <param name="builder">
    /// The resource builder of a Fusion stage.
    /// </param>
    /// <param name="force">
    /// Specifies whether the publication proceeds despite validation failures.
    /// </param>
    /// <returns>
    /// The resource builder for chaining.
    /// </returns>
    public static IResourceBuilder<FusionStageResource> WithForce(
        this IResourceBuilder<FusionStageResource> builder,
        bool force)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Resource.Force = force;
        return builder;
    }

    /// <summary>
    /// Configures the operation and approval timeouts of the deployment.
    /// </summary>
    /// <param name="builder">
    /// The resource builder of a Fusion stage.
    /// </param>
    /// <param name="operation">
    /// The time a single remote operation may take.
    /// </param>
    /// <param name="approval">
    /// The time the publication waits for approval.
    /// </param>
    /// <returns>
    /// The resource builder for chaining.
    /// </returns>
    public static IResourceBuilder<FusionStageResource> WithTimeouts(
        this IResourceBuilder<FusionStageResource> builder,
        TimeSpan operation,
        TimeSpan approval)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(operation, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(approval, TimeSpan.Zero);

        builder.Resource.OperationTimeout = operation;
        builder.Resource.ApprovalTimeout = approval;
        return builder;
    }

    private static void EnsureFusionPipeline(IDistributedApplicationBuilder builder)
    {
        if (builder.Resources.OfType<FusionPipelineResource>().Any())
        {
            return;
        }

        var pipeline = builder.AddResource(
            new FusionPipelineResource("fusion-nitro-pipeline"));
        FusionPipeline.Configure(pipeline);
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
