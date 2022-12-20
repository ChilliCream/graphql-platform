namespace HotChocolate.Execution.Processing;

internal static class ResultPoolDefaults
{
    public const int MaximumRetained = 512;
    public const int BucketSize = 64;
    public const int MaximumAllowedCapacity = 512;
}
