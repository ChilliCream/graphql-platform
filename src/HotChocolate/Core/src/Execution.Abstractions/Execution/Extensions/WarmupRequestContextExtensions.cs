namespace HotChocolate.Execution;

/// <summary>
/// Provides extension methods for <see cref="RequestContext"/>.
/// </summary>
public static class WarmupRequestContextExtensions
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
    public static bool IsWarmupRequest(this RequestContext requestContext)
    {
        ArgumentNullException.ThrowIfNull(requestContext);

        return requestContext.ContextData.ContainsKey(ExecutionContextData.IsWarmupRequest);
    }
}
