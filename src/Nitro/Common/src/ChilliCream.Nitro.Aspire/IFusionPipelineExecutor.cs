using Aspire.Hosting.Pipelines;

namespace ChilliCream.Nitro.Aspire;

#pragma warning disable ASPIREPIPELINES001

internal interface IFusionPipelineExecutor
{
    Task CreateArtifactsAsync(PipelineStepContext context);

    Task VerifyReadinessAsync(PipelineStepContext context);

    Task UploadAsync(PipelineStepContext context);

    Task PrepareReleaseAsync(PipelineStepContext context);

    Task ComposeReleaseAsync(PipelineStepContext context);

    Task PublishAsync(PipelineStepContext context);
}

#pragma warning restore ASPIREPIPELINES001
