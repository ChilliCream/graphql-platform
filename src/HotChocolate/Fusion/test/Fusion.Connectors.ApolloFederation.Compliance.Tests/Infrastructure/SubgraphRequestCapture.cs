using System.Collections.Concurrent;

namespace HotChocolate.Fusion;

/// <summary>
/// Records the gateway to subgraph HTTP requests sent while executing a gateway
/// operation, so a test can assert the number and wire shape of the requests that
/// the source schema client produced.
/// </summary>
public sealed class SubgraphRequestCapture
{
    private readonly ConcurrentQueue<CapturedSubgraphRequest> _requests = new();

    internal void Record(string subgraphName, string body)
        => _requests.Enqueue(new CapturedSubgraphRequest(subgraphName, body));

    /// <summary>
    /// All captured requests in the order they were sent.
    /// </summary>
    public IReadOnlyList<CapturedSubgraphRequest> Requests => _requests.ToArray();

    /// <summary>
    /// The captured requests that were sent to the named subgraph.
    /// </summary>
    /// <param name="subgraphName">The source schema name of the subgraph.</param>
    public IReadOnlyList<CapturedSubgraphRequest> ForSubgraph(string subgraphName)
        => _requests
            .Where(r => string.Equals(r.SubgraphName, subgraphName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
}

/// <summary>
/// A single gateway to subgraph HTTP request body.
/// </summary>
/// <param name="SubgraphName">The subgraph the request was sent to.</param>
/// <param name="Body">The UTF-8 request body.</param>
public sealed record CapturedSubgraphRequest(string SubgraphName, string Body);
