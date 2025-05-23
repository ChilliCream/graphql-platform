using System.Security.Claims;

namespace HotChocolate.Execution;

/// <summary>
/// Extensions methods for <see cref="OperationRequestBuilder"/>.
/// </summary>
public static class OperationRequestBuilderExtensions
{
    /// <summary>
    /// Marks the current request to allow non-persisted operations.
    /// </summary>
    public static OperationRequestBuilder AllowNonPersistedOperation(
        this OperationRequestBuilder builder)
        => builder.SetGlobalState(ExecutionContextData.NonPersistedOperationAllowed, true);

    /// <summary>
    /// Skips the request execution depth analysis.
    /// </summary>
    public static OperationRequestBuilder SkipExecutionDepthAnalysis(
        this OperationRequestBuilder builder)
        => builder.SetGlobalState(ExecutionContextData.SkipDepthAnalysis, null);

    /// <summary>
    /// Set allowed-execution-depth for this request and override the
    /// global allowed execution depth.
    /// </summary>
    public static OperationRequestBuilder SetMaximumAllowedExecutionDepth(
        this OperationRequestBuilder builder,
        int maximumAllowedDepth)
        => builder.SetGlobalState(ExecutionContextData.MaxAllowedExecutionDepth, maximumAllowedDepth);

    /// <summary>
    /// Sets the user for this request.
    /// </summary>
    public static OperationRequestBuilder SetUser(
        this OperationRequestBuilder builder,
        ClaimsPrincipal claimsPrincipal)
        => builder.SetGlobalState(nameof(ClaimsPrincipal), claimsPrincipal);

    /// <summary>
    /// Marks this request as a warmup request that will bypass security measures and skip execution.
    /// </summary>
    public static OperationRequestBuilder MarkAsWarmupRequest(
        this OperationRequestBuilder builder)
        => builder.SetGlobalState(ExecutionContextData.IsWarmupRequest, true);
}
