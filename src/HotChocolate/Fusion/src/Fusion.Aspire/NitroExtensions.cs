using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using HotChocolate.Fusion.Aspire.Nitro;
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

            options.PortalUrl ??= portalUrl;
            return builder;
        }

        options.Coordinator = NitroSeedCoordinator.CreateProduction(stage);
        options.PortalUrl = portalUrl;

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

        return builder;
    }

    /// <summary>
    /// Validates each successfully composed gateway schema against the configured Nitro API and
    /// stage. Validation uploads the full composed gateway schema to Nitro in the background and
    /// reports client-contract transitions through Aspire notifications and resource logs.
    /// </summary>
    /// <param name="builder">
    /// The resource builder of a Nitro-composed gateway.
    /// </param>
    /// <returns>
    /// The resource builder for chaining.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Nitro was not added with a stage, the gateway has no Nitro API id, or the resource is not
    /// configured for schema composition.
    /// </exception>
    public static IResourceBuilder<T> WithNitroSchemaValidation<T>(
        this IResourceBuilder<T> builder)
        where T : IResource
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = SchemaCompositionRegistration.GetOptions(builder.ApplicationBuilder);

        if (options?.Coordinator is null)
        {
            throw new InvalidOperationException(
                "Nitro schema validation requires AddNitro(stage) to be called before "
                + "WithNitroSchemaValidation.");
        }

        if (builder.Resource.GetNitroApiId() is null)
        {
            throw new InvalidOperationException(
                "Nitro schema validation requires WithNitroApiId(apiId) to be configured first.");
        }

        if (!builder.Resource.NeedsGraphQLSchemaComposition())
        {
            throw new InvalidOperationException(
                "Nitro schema validation can only be enabled on a gateway configured with "
                + "WithGraphQLSchemaComposition.");
        }

        builder.WithAnnotation(
            new NitroSchemaValidationAnnotation(),
            ResourceAnnotationMutationBehavior.Replace);

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
    /// Adds an environment-specific Fusion deployment that publishes to the Nitro publish target.
    /// </summary>
    /// <param name="builder">
    /// The resource builder of a Nitro publish target.
    /// </param>
    /// <param name="name">
    /// The name of the deployment resource.
    /// </param>
    /// <returns>
    /// The resource builder of the deployment for chaining.
    /// </returns>
    public static IResourceBuilder<FusionDeploymentResource> AddFusionDeployment(
        this IResourceBuilder<NitroPublishTargetResource> builder,
        string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var resource = new FusionDeploymentResource(name, builder.Resource);
        return builder.ApplicationBuilder.AddResource(resource);
    }

    /// <summary>
    /// Maps the deployment to an exact Aspire environment.
    /// </summary>
    /// <param name="builder">
    /// The resource builder of a Fusion deployment.
    /// </param>
    /// <param name="environmentName">
    /// The name of the Aspire environment.
    /// </param>
    /// <returns>
    /// The resource builder for chaining.
    /// </returns>
    public static IResourceBuilder<FusionDeploymentResource> ForEnvironment(
        this IResourceBuilder<FusionDeploymentResource> builder,
        string environmentName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentName);

        builder.Resource.EnvironmentName = environmentName;
        return builder;
    }

    /// <summary>
    /// Maps the deployment to an exact Nitro stage.
    /// </summary>
    /// <param name="builder">
    /// The resource builder of a Fusion deployment.
    /// </param>
    /// <param name="stageName">
    /// The name of the Nitro stage that the deployment publishes to.
    /// </param>
    /// <returns>
    /// The resource builder for chaining.
    /// </returns>
    public static IResourceBuilder<FusionDeploymentResource> ToStage(
        this IResourceBuilder<FusionDeploymentResource> builder,
        string stageName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(stageName);

        builder.Resource.StageName = stageName;
        return builder;
    }

    /// <summary>
    /// Selects the exact <c>schema-settings.json</c> environment used for composition.
    /// </summary>
    /// <param name="builder">
    /// The resource builder of a Fusion deployment.
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
    public static IResourceBuilder<FusionDeploymentResource> WithCompositionEnvironment(
        this IResourceBuilder<FusionDeploymentResource> builder,
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
    /// The resource builder of a Fusion deployment.
    /// </param>
    /// <param name="configurationTag">
    /// The parameter that supplies the release tag.
    /// </param>
    /// <returns>
    /// The resource builder for chaining.
    /// </returns>
    public static IResourceBuilder<FusionDeploymentResource> WithConfigurationTag(
        this IResourceBuilder<FusionDeploymentResource> builder,
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
    /// The resource builder of a Fusion deployment.
    /// </param>
    /// <param name="configurationTag">
    /// The release tag.
    /// </param>
    /// <returns>
    /// The resource builder for chaining.
    /// </returns>
    public static IResourceBuilder<FusionDeploymentResource> WithConfigurationTag(
        this IResourceBuilder<FusionDeploymentResource> builder,
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
    /// The resource builder of a Fusion deployment.
    /// </param>
    /// <param name="waitForApproval">
    /// Specifies whether the publication waits for approval.
    /// </param>
    /// <returns>
    /// The resource builder for chaining.
    /// </returns>
    public static IResourceBuilder<FusionDeploymentResource> WithApproval(
        this IResourceBuilder<FusionDeploymentResource> builder,
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
    /// The resource builder of a Fusion deployment.
    /// </param>
    /// <param name="force">
    /// Specifies whether the publication proceeds despite validation failures.
    /// </param>
    /// <returns>
    /// The resource builder for chaining.
    /// </returns>
    public static IResourceBuilder<FusionDeploymentResource> WithForce(
        this IResourceBuilder<FusionDeploymentResource> builder,
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
    /// The resource builder of a Fusion deployment.
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
    public static IResourceBuilder<FusionDeploymentResource> WithTimeouts(
        this IResourceBuilder<FusionDeploymentResource> builder,
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

    internal static bool HasNitroSchemaValidation(this IResource resource)
        => resource.Annotations.OfType<NitroSchemaValidationAnnotation>().Any();
}
