using System.Collections.Concurrent;

namespace HotChocolate.Fusion;

/// <summary>
/// Records HTTP requests and responses exchanged between the gateway and subgraphs.
/// </summary>
public sealed class SubgraphRequestCapture
{
    private readonly ConcurrentQueue<CapturedSubgraphRequest> _requests = new();

    internal CapturedSubgraphRequest Record(string subgraphName, string body)
    {
        var request = new CapturedSubgraphRequest(subgraphName, body);
        _requests.Enqueue(request);
        return request;
    }

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
/// A subgraph HTTP request and its response.
/// </summary>
/// <param name="SubgraphName">The subgraph the request was sent to.</param>
/// <param name="Body">The UTF-8 request body.</param>
public sealed record CapturedSubgraphRequest(string SubgraphName, string Body)
{
    public string ResponseBody { get; internal set; } = string.Empty;

    public int? StatusCode { get; internal set; }

    public string? ContentType { get; internal set; }
}
