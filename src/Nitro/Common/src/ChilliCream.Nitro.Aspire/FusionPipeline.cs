using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using HotChocolate.Fusion.Aspire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ChilliCream.Nitro.Aspire;

#pragma warning disable ASPIREPIPELINES001

internal static class FusionPipeline
{
    internal const string ArtifactsStepName = "fusion-artifacts";
    internal const string ReadinessStepName = "fusion-readiness";
    internal const string UploadStepName = "fusion-upload";
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
                string.Equals(
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
                    deployment.Nitro.CloudUrl,
                    deployment.Nitro.ApiId,
                    deployment.StageName),
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

    private static IEnumerable<PipelineStep> CreateSteps(
        PipelineStepFactoryContext context,
        FusionPipelineTopology topology)
    {
        var environment = context.PipelineContext.Services
            .GetRequiredService<IHostEnvironment>()
            .EnvironmentName;
        var deployments = SelectDeployments(
            context.PipelineContext.Model,
            environment);

        topology.EnvironmentName = environment;
        topology.HasDeployments = deployments.Count > 0;

        return CreateStepDefinitions(context.Resource, topology);
    }

    internal static PipelineStep[] CreateStepDefinitionsForTest(
        IResource resource)
        => CreateStepDefinitions(resource, new FusionPipelineTopology());

    private static PipelineStep[] CreateStepDefinitions(
        IResource resource,
        FusionPipelineTopology topology)
        =>
        [
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
                Name = ReadinessStepName,
                Description = "Verify deployed Fusion source services are ready.",
                Resource = resource,
                DependsOnSteps = [ArtifactsStepName],
                Action = stepContext => ExecuteReadinessAsync(stepContext, topology)
            },
            new PipelineStep
            {
                Name = UploadStepName,
                Description = "Reconcile immutable Fusion source schema versions.",
                Resource = resource,
                DependsOnSteps = [ArtifactsStepName],
                Action = ExecuteUploadAsync
            },
            new PipelineStep
            {
                Name = PublishStepName,
                Description = "Compose and publish the Fusion configuration to Nitro.",
                Resource = resource,
                DependsOnSteps = [UploadStepName, ReadinessStepName],
                RequiredBySteps = [WellKnownPipelineSteps.Deploy],
                Action = ExecutePublishAsync
            }
        ];

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
        var readiness = context.Steps.Single(
            step => step.Name == ReadinessStepName);

        topology.SourcesWithoutCompute.Clear();

        foreach (var source in sources)
        {
            var computeSteps = context
                .GetSteps(source.Resource, WellKnownPipelineTags.DeployCompute)
                .ToArray();

            if (computeSteps.Length == 0)
            {
                topology.SourcesWithoutCompute.Add(source.Resource.Name);
                continue;
            }

            foreach (var computeStep in computeSteps)
            {
                readiness.DependsOn(computeStep);
            }
        }
    }

    private static Task ExecuteArtifactsAsync(PipelineStepContext context)
        => GetExecutor(context).CreateArtifactsAsync(context);

    private static Task ExecuteReadinessAsync(
        PipelineStepContext context,
        FusionPipelineTopology topology)
    {
        if (topology.SourcesWithoutCompute.Count > 0)
        {
            throw new InvalidOperationException(
                "Fusion publication cannot prove compute deployment ordering for resources: "
                + string.Join(", ", topology.SourcesWithoutCompute.Order()));
        }

        return GetExecutor(context).VerifyReadinessAsync(context);
    }

    private static Task ExecuteUploadAsync(PipelineStepContext context)
        => GetExecutor(context).UploadAsync(context);

    private static Task ExecutePublishAsync(PipelineStepContext context)
        => GetExecutor(context).PublishAsync(context);

    private static IFusionPipelineExecutor GetExecutor(
        PipelineStepContext context)
        => context.Services.GetService<IFusionPipelineExecutor>()
            ?? FusionPipelineExecutor.Instance;

    private static void ValidateDeclaration(
        FusionDeploymentResource deployment)
    {
        if (string.IsNullOrWhiteSpace(deployment.EnvironmentName))
        {
            throw new InvalidOperationException(
                $"Fusion deployment '{deployment.Name}' must select an Aspire environment.");
        }

        if (string.IsNullOrWhiteSpace(deployment.StageName))
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

    private sealed class FusionPipelineTopology
    {
        public string? EnvironmentName { get; set; }

        public bool HasDeployments { get; set; }

        public HashSet<string> SourcesWithoutCompute { get; } =
            new(StringComparer.Ordinal);
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

#pragma warning restore ASPIREPIPELINES001
