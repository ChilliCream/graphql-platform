using Aspire.Hosting.Pipelines;

namespace ChilliCream.Nitro.Aspire;

#pragma warning disable ASPIREPIPELINES001

internal interface IFusionPipelineExecutor
{
    Task CreateArtifactsAsync(PipelineStepContext context);

    Task VerifyReadinessAsync(
        PipelineStepContext context,
        FusionPipelineSession session);

    Task UploadAsync(PipelineStepContext context);

    Task PreflightAsync(
        PipelineStepContext context,
        FusionPipelineSession session);

    Task DownloadAsync(
        PipelineStepContext context,
        FusionPipelineSession session);

    Task ComposeAsync(
        PipelineStepContext context,
        FusionPipelineSession session);

    Task PublishAsync(
        PipelineStepContext context,
        FusionPipelineSession session);
}

#pragma warning restore ASPIREPIPELINES001
