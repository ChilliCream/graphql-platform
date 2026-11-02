using System.Text.Encodings.Web;
using System.Text.Json;
using HotChocolate.Execution;

namespace HotChocolate.Fusion.WireFormat;

/// <summary>
/// Renders the gateway to subgraph exchange of a wire-format test as a markdown snapshot: one
/// section per captured HTTP request, in the order the requests were sent, followed by the merged
/// gateway result.
/// </summary>
internal static class WireFormatSnapshot
{
    private static readonly JsonSerializerOptions s_indented =
        new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>
    /// Matches the captured exchange against the markdown snapshot of the calling test.
    /// </summary>
    /// <param name="capture">The recorded gateway to subgraph requests.</param>
    /// <param name="result">The merged gateway result.</param>
    /// <param name="postFix">
    /// Distinguishes the snapshots of a test that is executed for several inputs.
    /// </param>
    public static async Task MatchExchangeAsync(
        SubgraphRequestCapture capture,
        IExecutionResult result,
        string? postFix = null)
    {
        var snapshot = Snapshot.Create(postFix);
        var requests = capture.Requests;

        for (var i = 0; i < requests.Count; i++)
        {
            var request = requests[i];
            snapshot.Add(
                FormatJson(request.Body),
                $"HTTP Request {i + 1} to '{request.SubgraphName}'",
                "json");
        }

        snapshot.Add(result, "Gateway Result");

        await snapshot.MatchMarkdownAsync();
    }

    // Reformats the captured request body with indentation for readability while
    // preserving the wire escaping (embedded quotes stay as \" rather than "),
    // so the snapshot shows the body shape the client actually sent.
    private static string FormatJson(string body)
    {
        using var document = JsonDocument.Parse(body);
        return JsonSerializer.Serialize(document, s_indented);
    }
}
