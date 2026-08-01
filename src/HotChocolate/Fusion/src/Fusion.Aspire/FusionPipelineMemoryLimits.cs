namespace HotChocolate.Fusion.Aspire;

internal sealed record FusionPipelineMemoryLimits(int SourceArchiveBytes)
{
    public const int DefaultSourceArchiveBytes = 128_000_000;

    public static FusionPipelineMemoryLimits Default { get; } = new(
        DefaultSourceArchiveBytes);
}
