namespace HotChocolate.Fusion.Aspire;

internal sealed record FusionPipelineMemoryLimits(
    int SourceArchiveBytes,
    long TotalSourceArchiveBytes,
    int FusionArchiveBytes)
{
    public const int DefaultSourceArchiveBytes = 128_000_000;
    public const long DefaultTotalSourceArchiveBytes = 512_000_000;
    public const int DefaultFusionArchiveBytes = 256_000_000;

    public static FusionPipelineMemoryLimits Default { get; } = new(
        DefaultSourceArchiveBytes,
        DefaultTotalSourceArchiveBytes,
        DefaultFusionArchiveBytes);
}
