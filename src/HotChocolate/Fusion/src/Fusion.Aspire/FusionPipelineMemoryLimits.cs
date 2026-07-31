namespace HotChocolate.Fusion.Aspire;

internal sealed record FusionPipelineMemoryLimits(
    int SourceArchiveBytes,
    long TotalSourceArchiveBytes)
{
    public const int DefaultSourceArchiveBytes = 128_000_000;

    public const int DefaultTotalSourceArchiveBytes = 512_000_000;

    public static FusionPipelineMemoryLimits Default { get; } = new(
        DefaultSourceArchiveBytes,
        DefaultTotalSourceArchiveBytes);
}
