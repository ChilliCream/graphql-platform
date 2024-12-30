namespace HotChocolate.Execution;

public static class WarmupRequestExecutorExtensions
{
    public static bool IsWarmupRequest(this IRequestContext requestContext)
        => requestContext.ContextData.ContainsKey(WellKnownContextData.IsWarmupRequest);
}
