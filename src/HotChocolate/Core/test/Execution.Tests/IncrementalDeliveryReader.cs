using System.Text.Json;

namespace HotChocolate.Execution;

/// <summary>
/// Projects a response stream to the ordered delivery events it carries. The events are
/// independent of the payload boundaries that the delivery coalescing produces.
/// </summary>
internal static class IncrementalDeliveryReader
{
    public static async Task<List<string>> ReadDeliveryAsync(
        IExecutionResult result,
        CancellationToken cancellationToken)
    {
        var events = new List<string>();

        await foreach (var payload in
            Assert.IsType<ResponseStream>(result).ReadResultsAsync().WithCancellation(cancellationToken))
        {
            await using var current = payload;
            using var document = JsonDocument.Parse(current.ToJson(withIndentations: false));
            var root = document.RootElement;

            if (root.TryGetProperty("data", out var data))
            {
                events.Add($"initial:{data.GetRawText()}");
            }

            if (root.TryGetProperty("incremental", out var incremental))
            {
                foreach (var entry in incremental.EnumerateArray())
                {
                    if (entry.TryGetProperty("items", out var items))
                    {
                        events.Add($"items:{items.GetRawText()}");
                    }
                    else if (entry.TryGetProperty("data", out var deferred))
                    {
                        events.Add($"deferred:{deferred.GetRawText()}");
                    }
                }
            }

            if (root.TryGetProperty("hasNext", out var hasNext) && !hasNext.GetBoolean())
            {
                events.Add("end");
            }
        }

        return events;
    }
}
