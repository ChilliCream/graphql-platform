using System.Security.Claims;

namespace HotChocolate.Execution;

/// <summary>
/// Extensions methods for <see cref="OperationRequestBuilder"/>.
/// </summary>
public static class OperationRequestBuilderExtensions
{
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
