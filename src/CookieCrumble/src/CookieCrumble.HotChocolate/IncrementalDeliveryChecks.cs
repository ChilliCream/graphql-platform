namespace CookieCrumble.HotChocolate;

/// <summary>
/// Opt-in checks for snapshots of incrementally delivered (@defer and @stream) responses.
/// </summary>
public static class IncrementalDeliveryChecks
{
    private static readonly AsyncLocal<bool> s_rejectOverlappingPositions = new();

    internal static bool RejectsOverlappingPositions => s_rejectOverlappingPositions.Value;

    /// <summary>
    /// Makes snapshot formatting fail when incremental payloads deliver the same response position more than once.
    /// The check covers the IExecutionResult formatters and the multipart and JSON-lines GraphQLHttpResponse
    /// formatters, and applies to the calling test until the returned scope is disposed.
    /// </summary>
    public static IDisposable RejectOverlappingPositions()
    {
        var previous = s_rejectOverlappingPositions.Value;
        s_rejectOverlappingPositions.Value = true;
        return new Scope(previous);
    }

    private sealed class Scope(bool previous) : IDisposable
    {
        public void Dispose() => s_rejectOverlappingPositions.Value = previous;
    }
}
