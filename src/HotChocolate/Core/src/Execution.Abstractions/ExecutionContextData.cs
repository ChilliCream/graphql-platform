namespace HotChocolate;

public static class ExecutionContextData
{
    /// <summary>
    /// The key to override the max allowed execution depth.
    /// </summary>
    public const string MaxAllowedExecutionDepth = "HotChocolate.Execution.MaxAllowedDepth";

    /// <summary>
    /// The key to determine whether the request is a warmup request.
    /// </summary>
    public const string IsWarmupRequest = "HotChocolate.AspNetCore.Warmup.IsWarmupRequest";

    /// <summary>
    /// The key to skip the execution depth analysis.
    /// </summary>
    public const string SkipDepthAnalysis = "HotChocolate.Execution.SkipDepthAnalysis";

    /// <summary>
    /// The key that specifies that the current context allows standard operations
    /// that are not known to the server.
    /// </summary>
    public const string NonPersistedOperationAllowed = "HotChocolate.Execution.NonPersistedOperationAllowed";
}
