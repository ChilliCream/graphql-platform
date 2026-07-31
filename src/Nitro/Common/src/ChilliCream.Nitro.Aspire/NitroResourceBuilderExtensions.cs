using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using ChilliCream.Nitro.Fusion;

namespace ChilliCream.Nitro.Aspire;

/// <summary>
/// Provides extensions for declaring Nitro Fusion deployments.
/// </summary>
public static class NitroResourceBuilderExtensions
{
    /// <summary>
    /// Adds a Nitro target.
    /// </summary>
    public static IResourceBuilder<NitroResource> AddNitroTarget(
        this IDistributedApplicationBuilder builder,
        string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        EnsureFusionPipeline(builder);
        builder.Services.AddNitroFusionDeploymentWorkflow();

        var configuredCloudUrl =
            builder.Configuration["Nitro:CloudUrl"]
            ?? builder.Configuration["NITRO_CLOUD_URL"];
        var resource = new NitroResource(name)
        {
            CloudUrl = string.IsNullOrWhiteSpace(configuredCloudUrl)
                ? null
                : NormalizeCloudUrl(configuredCloudUrl),
            ApiId =
                builder.Configuration["Nitro:ApiId"]
                ?? builder.Configuration["NITRO_API_ID"]
        };

        return builder.AddResource(resource);
    }

    /// <summary>
    /// Sets the Nitro GraphQL API URL.
    /// </summary>
    public static IResourceBuilder<NitroResource> WithCloudUrl(
        this IResourceBuilder<NitroResource> builder,
        string cloudUrl)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Resource.CloudUrl = NormalizeCloudUrl(cloudUrl);
        return builder;
    }

    /// <summary>
    /// Sets the Nitro API identifier.
    /// </summary>
    public static IResourceBuilder<NitroResource> WithApiId(
        this IResourceBuilder<NitroResource> builder,
        string apiId)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiId);

        builder.Resource.ApiId = apiId;
        return builder;
    }

    /// <summary>
    /// Sets the secret parameter used as the Nitro API key.
    /// </summary>
    public static IResourceBuilder<NitroResource> WithApiKey(
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
    /// Adds an environment-specific Fusion deployment.
    /// </summary>
    public static IResourceBuilder<FusionDeploymentResource> AddFusionDeployment(
        this IResourceBuilder<NitroResource> builder,
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
    /// Uses a promoted Fusion release manifest for this deployment.
    /// </summary>
    /// <remarks>
    /// The parameter must resolve to the absolute path of <c>fusion-release.json</c>.
    /// </remarks>
    public static IResourceBuilder<FusionDeploymentResource> WithFusionReleaseManifest(
        this IResourceBuilder<FusionDeploymentResource> builder,
        IResourceBuilder<ParameterResource> manifestPath)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(manifestPath);

        if (builder.Resource.UseGitCommitAsSourceVersion)
        {
            throw new InvalidOperationException(
                "A promoted Fusion release cannot rediscover source versions from Git.");
        }

        builder.Resource.FusionReleaseManifestParameter = manifestPath.Resource;
        return builder;
    }

    /// <summary>
    /// Sets the immutable release tag.
    /// </summary>
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
    /// Sets the immutable release tag.
    /// </summary>
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
    /// Uses the current Git commit as the default source schema version.
    /// </summary>
    public static IResourceBuilder<FusionDeploymentResource>
        WithDefaultSourceVersionFromGitCommit(
            this IResourceBuilder<FusionDeploymentResource> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.Resource.FusionReleaseManifestParameter is not null)
        {
            throw new InvalidOperationException(
                "A promoted Fusion release cannot rediscover source versions from Git.");
        }

        builder.Resource.UseGitCommitAsSourceVersion = true;
        return builder;
    }

    /// <summary>
    /// Configures whether Nitro waits for approval.
    /// </summary>
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
    public static IResourceBuilder<FusionDeploymentResource> WithForce(
        this IResourceBuilder<FusionDeploymentResource> builder,
        bool force)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Resource.Force = force;
        return builder;
    }

    /// <summary>
    /// Configures operation and approval timeouts.
    /// </summary>
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
}
