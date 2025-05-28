namespace HotChocolate.Execution;

/// <summary>
/// Provides extension methods for <see cref="IRequestContext"/>.
/// </summary>
public static class WarmupRequestExecutorExtensions
{
    /// <summary>
    /// Checks if the request is a warmup request.
    /// </summary>
    /// <param name="requestContext">
    /// The request context.
    /// </param>
    /// <returns>
    /// Returns <see langword="true"/> if the request is a warmup request;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool IsWarmupRequest(this IRequestContext requestContext)
        => requestContext.ContextData.ContainsKey(ExecutionContextData.IsWarmupRequest);
}
