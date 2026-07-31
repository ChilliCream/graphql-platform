using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using HotChocolate.Fusion.Aspire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HotChocolate.Fusion.Aspire;

#pragma warning disable ASPIREPIPELINES001

internal static class FusionPipeline
{
    internal const string ArtifactsStepName = "fusion-artifacts";
    internal const string DownloadStepName = "fusion-download";
    internal const string ComposeStepName = "fusion-compose";
    internal const string ReadinessStepName = "fusion-readiness";
    internal const string UploadStepName = "fusion-upload";
    internal const string PublishStageStepName = "fusion-publish-stage";
    internal const string PublishStepName = "fusion-publish";

    public static void Configure(
        IResourceBuilder<FusionPipelineResource> pipeline)
    {
        var topology = new FusionPipelineTopology();

        pipeline.WithPipelineStepFactory(
            context => CreateSteps(context, topology));
        pipeline.WithPipelineConfiguration(
            context => ConfigureSteps(context, topology));
    }

    internal static IReadOnlyList<FusionDeploymentResource> SelectDeployments(
        DistributedApplicationModel model,
        string environmentName)
    {
        var deployments = model.Resources
            .OfType<FusionDeploymentResource>()
            .Where(deployment =>
                deployment.EnvironmentName is null
                || string.Equals(
                    deployment.EnvironmentName,
                    environmentName,
                    StringComparison.Ordinal))
            .ToArray();

        foreach (var deployment in deployments)
        {
            ValidateDeclaration(deployment);
        }

        var duplicate = deployments
            .GroupBy(
                deployment => (
                    CloudUrl: deployment.Nitro.CloudUrl,
                    ApiId: deployment.Nitro.ApiId,
                    StageName: GetStageKey(deployment)),
                FusionDeploymentKeyComparer.Instance)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                $"Multiple Fusion deployments map environment '{environmentName}' to Nitro "
                + $"API '{duplicate.Key.ApiId}' stage '{duplicate.Key.StageName}'.");
        }

        return deployments;
    }

    /// <summary>
    /// Gets the declaration-time identity of the stage that a deployment publishes to. A stage that
    /// is supplied by a parameter is only known per publish, so the parameter itself is the
    /// identity.
    /// </summary>
    private static string GetStageKey(FusionDeploymentResource deployment)
        => deployment.StageName ?? $"{{{deployment.StageParameter!.Name}}}";

    internal static IResourceWithEndpoints GetCompositionResource(
        DistributedApplicationModel model)
    {
        var compositions = model.GetGraphQLCompositionResources().ToArray();
        return compositions.Length switch
        {
            1 => compositions[0],
            0 => throw new InvalidOperationException(
                "A Fusion deployment requires one resource with GraphQL schema composition."),
            _ => throw new InvalidOperationException(
                "A Fusion deployment requires exactly one resource with GraphQL schema composition.")
        };
    }

    internal static IEnumerable<PipelineStep> CreateSteps(
        PipelineStepFactoryContext context,
        FusionPipelineTopology topology)
    {
        var environment = context.PipelineContext.Services
            .GetRequiredService<IHostEnvironment>()
            .EnvironmentName;
        var deployments = SelectDeployments(
            context.PipelineContext.Model,
            environment);

        topology.HasDeployments = deployments.Count > 0;

        var session = new FusionPipelineSession(
            context.PipelineContext.CancellationToken);
        return CreateStepDefinitions(
            context.Resource,
            topology,
            session);
    }

    private static PipelineStep[] CreateStepDefinitions(
        IResource resource,
        FusionPipelineTopology topology,
        FusionPipelineSession session)
    {
        var buildSteps = new[]
        {
            new PipelineStep
            {
                Name = ArtifactsStepName,
                Description = "Produce portable Fusion deployment artifacts.",
                Resource = resource,
                RequiredBySteps = [WellKnownPipelineSteps.Publish],
                Action = ExecuteArtifactsAsync
            },
            new PipelineStep
            {
                Name = UploadStepName,
                Description = "Reconcile immutable Fusion source schema versions.",
                Resource = resource,
                DependsOnSteps = [ArtifactsStepName],
                Action = ExecuteUploadAsync
            }
        };

        return
        [
            .. buildSteps,
            new PipelineStep
            {
                Name = DownloadStepName,
                Description = "Download exact Fusion source schema versions from Nitro.",
                Resource = resource,
                Action = context => ExecuteSessionStepAsync(
                    context,
                    session,
                    static (executor, stepContext, pipelineSession) =>
                        executor.PreflightAsync(stepContext, pipelineSession))
            },
            new PipelineStep
            {
                Name = ComposeStepName,
                Description = "Compose the Fusion configuration for this environment.",
                Resource = resource,
                DependsOnSteps = [DownloadStepName],
                Action = context => ExecuteSessionStepAsync(
                    context,
                    session,
                    static async (executor, stepContext, pipelineSession) =>
                    {
                        await executor.DownloadAsync(
                            stepContext,
                            pipelineSession);
                        await executor.ComposeAsync(
                            stepContext,
                            pipelineSession);
                    })
            },
            new PipelineStep
            {
                Name = ReadinessStepName,
                Description = "Verify deployed Fusion source services are ready.",
                Resource = resource,
                DependsOnSteps = [ComposeStepName],
                Action = context => ExecuteSessionStepAsync(
                    context,
                    session,
                    (executor, stepContext, pipelineSession) =>
                    {
                        EnsureResourceDeploymentOrdering(
                            topology.ResourcesWithoutCompute);
                        return executor.VerifyReadinessAsync(
                            stepContext,
                            pipelineSession);
                    })
            },
            new PipelineStep
            {
                Name = PublishStageStepName,
                Description = "Publish the Fusion configuration to Nitro.",
                Resource = resource,
                DependsOnSteps = [ReadinessStepName],
                Action = context => ExecuteSessionStepAsync(
                    context,
                    session,
                    static (executor, stepContext, pipelineSession) =>
                        executor.PublishAsync(stepContext, pipelineSession))
            },
            new PipelineStep
            {
                Name = PublishStepName,
                Description = "Complete the Fusion deployment.",
                Resource = resource,
                DependsOnSteps = [PublishStageStepName],
                RequiredBySteps = [WellKnownPipelineSteps.Deploy],
                Action = _ =>
                {
                    session.Dispose();
                    return Task.CompletedTask;
                }
            }
        ];
    }

    private static void ConfigureSteps(
        PipelineConfigurationContext context,
        FusionPipelineTopology topology)
    {
        if (!topology.HasDeployments)
        {
            return;
        }

        var composition = GetCompositionResource(context.Model);
        var sources = GraphQLResourceModel.GetReferencedSourceSchemas(
            composition,
            context.Model);
        var download = context.Steps.Single(
            step => step.Name == DownloadStepName);
        var readiness = context.Steps.Single(
            step => step.Name == ReadinessStepName);
        var compose = context.Steps.Single(
            step => step.Name == ComposeStepName);
        var stagePublication = context.Steps.Single(
            step => step.Name == PublishStageStepName);
        var publication = context.Steps.Single(
            step => step.Name == PublishStepName);

        topology.ResourcesWithoutCompute.Clear();

        foreach (var source in sources)
        {
            var computeSteps = SelectResourceDeploymentSteps(
                context,
                source.Resource);

            if (computeSteps.Length == 0)
            {
                topology.ResourcesWithoutCompute.Add(source.Resource.Name);
                continue;
            }

            foreach (var computeStep in computeSteps)
            {
                computeStep.DependsOn(download);
                compose.DependsOn(computeStep);
                readiness.DependsOn(computeStep);
            }
        }

        var gatewayComputeSteps = SelectResourceDeploymentSteps(
            context,
            composition);

        if (gatewayComputeSteps.Length == 0)
        {
            topology.ResourcesWithoutCompute.Add(composition.Name);
            return;
        }

        WireGatewayDeployment(
            stagePublication,
            publication,
            gatewayComputeSteps);
    }

    internal static void WireGatewayDeployment(
        PipelineStep stagePublication,
        PipelineStep publication,
        IEnumerable<PipelineStep> gatewayComputeSteps)
    {
        foreach (var gatewayComputeStep in gatewayComputeSteps)
        {
            gatewayComputeStep.DependsOn(stagePublication);
            publication.DependsOn(gatewayComputeStep);
        }
    }

    internal static PipelineStep[] SelectResourceDeploymentSteps(
        PipelineConfigurationContext context,
        IResource resource)
    {
        var deployComputeSteps = context
            .GetSteps(resource, WellKnownPipelineTags.DeployCompute)
            .ToArray();

        if (deployComputeSteps.Length > 0)
        {
            return deployComputeSteps;
        }

        var deploymentTarget = resource
            .GetDeploymentTargetAnnotation()
            ?.DeploymentTarget;

        if (deploymentTarget is not null)
        {
            deployComputeSteps = context
                .GetSteps(
                    deploymentTarget,
                    WellKnownPipelineTags.DeployCompute)
                .ToArray();

            if (deployComputeSteps.Length > 0)
            {
                return deployComputeSteps;
            }
        }

        return [];
    }

    private static Task ExecuteArtifactsAsync(PipelineStepContext context)
        => FusionPipelineExecutor.Instance.CreateArtifactsAsync(context);

    private static async Task ExecuteSessionStepAsync(
        PipelineStepContext context,
        FusionPipelineSession session,
        Func<
            FusionPipelineExecutor,
            PipelineStepContext,
            FusionPipelineSession,
            Task> execute)
    {
        try
        {
            await execute(FusionPipelineExecutor.Instance, context, session);
        }
        catch
        {
            session.Dispose();
            throw;
        }
    }

    internal static void EnsureResourceDeploymentOrdering(
        IReadOnlyCollection<string> resourcesWithoutCompute)
    {
        if (resourcesWithoutCompute.Count > 0)
        {
            throw new InvalidOperationException(
                "Fusion publication cannot prove compute deployment ordering for resources: "
                + string.Join(", ", resourcesWithoutCompute.Order()));
        }
    }

    private static Task ExecuteUploadAsync(PipelineStepContext context)
        => FusionPipelineExecutor.Instance.UploadAsync(context);

    private static void ValidateDeclaration(
        FusionDeploymentResource deployment)
    {
        if (string.IsNullOrWhiteSpace(deployment.StageName)
            && deployment.StageParameter is null)
        {
            throw new InvalidOperationException(
                $"Fusion deployment '{deployment.Name}' must select a Nitro stage.");
        }

        if (string.IsNullOrWhiteSpace(deployment.Nitro.CloudUrl))
        {
            throw new InvalidOperationException(
                $"Nitro target '{deployment.Nitro.Name}' must specify a cloud URL.");
        }

        if (!Uri.TryCreate(
                deployment.Nitro.CloudUrl,
                UriKind.Absolute,
                out var cloudUri)
            || cloudUri.Scheme is not "https")
        {
            throw new InvalidOperationException(
                $"Nitro target '{deployment.Nitro.Name}' cloud URL must use HTTPS.");
        }

        if (!string.IsNullOrEmpty(cloudUri.UserInfo)
            || cloudUri.AbsolutePath is not "/"
            || !string.IsNullOrEmpty(cloudUri.Query)
            || !string.IsNullOrEmpty(cloudUri.Fragment))
        {
            throw new InvalidOperationException(
                $"Nitro target '{deployment.Nitro.Name}' cloud URL must be an origin.");
        }

        if (string.IsNullOrWhiteSpace(deployment.Nitro.ApiId))
        {
            throw new InvalidOperationException(
                $"Nitro target '{deployment.Nitro.Name}' must specify an API ID.");
        }

        if (deployment.ConfigurationTagParameter is null
            && string.IsNullOrWhiteSpace(deployment.ConfigurationTag))
        {
            throw new InvalidOperationException(
                $"Fusion deployment '{deployment.Name}' must specify a configuration tag.");
        }
    }

    private sealed class FusionDeploymentKeyComparer
        : IEqualityComparer<(string? CloudUrl, string? ApiId, string? StageName)>
    {
        public static FusionDeploymentKeyComparer Instance { get; } = new();

        public bool Equals(
            (string? CloudUrl, string? ApiId, string? StageName) x,
            (string? CloudUrl, string? ApiId, string? StageName) y)
            => string.Equals(x.CloudUrl, y.CloudUrl, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.ApiId, y.ApiId, StringComparison.Ordinal)
                && string.Equals(x.StageName, y.StageName, StringComparison.Ordinal);

        public int GetHashCode(
            (string? CloudUrl, string? ApiId, string? StageName) obj)
            => HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.CloudUrl ?? ""),
                StringComparer.Ordinal.GetHashCode(obj.ApiId ?? ""),
                StringComparer.Ordinal.GetHashCode(obj.StageName ?? ""));
    }
}

internal sealed class FusionPipelineTopology
{
    public bool HasDeployments { get; set; }

    public HashSet<string> ResourcesWithoutCompute { get; } =
        new(StringComparer.Ordinal);
}

#pragma warning restore ASPIREPIPELINES001
