using Aspire.Hosting.Pipelines;

namespace ChilliCream.Nitro.Aspire;

#pragma warning disable ASPIREPIPELINES001

internal interface IFusionPipelineExecutor
{
    Task CreateArtifactsAsync(PipelineStepContext context);

    Task VerifyReadinessAsync(PipelineStepContext context);

    Task UploadAsync(PipelineStepContext context);

    Task DownloadAsync(PipelineStepContext context);

    Task ComposeAsync(PipelineStepContext context);

    Task PublishAsync(PipelineStepContext context);
}

#pragma warning restore ASPIREPIPELINES001
