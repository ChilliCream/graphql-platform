using System.Buffers;
using System.Text.Json;
using CookieCrumble.Formatters;
using HotChocolate;
using HotChocolate.Execution;
using static CookieCrumble.HotChocolate.Formatters.StableSnapshotHelpers;

namespace CookieCrumble.HotChocolate.Formatters;

/// <summary>
/// Produces a deterministic snapshot representation for incremental execution results.
/// The formatter normalizes payload timing/chunking by aggregating pending/incremental/completed
/// entries and writing a stable summary plus the merged final result.
/// </summary>
internal sealed class StableExecutionResultSnapshotValueFormatter
    : SnapshotValueFormatter<IExecutionResult>
{
    public StableExecutionResultSnapshotValueFormatter()
        : base("json")
    {
    }

    protected override void Format(IBufferWriter<byte> snapshot, IExecutionResult value)
    {
        if (value.Kind is ExecutionResultKind.SingleResult)
        {
            using var resultDoc = JsonDocument.Parse(value.ToJson());
            using var writer = new Utf8JsonWriter(snapshot, IndentedWriterOptions);
            WriteCanonicalResponseObject(writer, resultDoc.RootElement);
            writer.Flush();
            snapshot.AppendLine();
            return;
        }

        FormatStreamAsync(snapshot, (IResponseStream)value).GetAwaiter().GetResult();
    }

    private static async Task FormatStreamAsync(
        IBufferWriter<byte> snapshot,
        IResponseStream stream)
    {
        var docs = new List<JsonDocument>();
        JsonResultPatcher? patcher = null;
        var acc = new StreamAccumulator();

        try
        {
            await foreach (var queryResult in stream.ReadResultsAsync().ConfigureAwait(false))
            {
                var doc = JsonDocument.Parse(queryResult.ToJson());
                docs.Add(doc);

                var root = doc.RootElement;
                acc.AddPayload(root);

                if (patcher is null)
                {
                    patcher = new JsonResultPatcher();
                    patcher.SetResponse(doc);
                }
                else
                {
                    patcher.ApplyPatch(doc);
                }
            }

            await using var writer = new Utf8JsonWriter(snapshot, IndentedWriterOptions);

            if (patcher is null)
            {
                writer.WriteStartObject();
                writer.WriteEndObject();
                writer.Flush();
                snapshot.AppendLine();
                return;
            }

            var mergedBuffer = new ArrayBufferWriter<byte>();
            patcher.WriteResponse(mergedBuffer);
            using var mergedDoc = JsonDocument.Parse(mergedBuffer.WrittenMemory);

            WriteStableStreamSnapshot(writer, acc, mergedDoc.RootElement);
            writer.Flush();
            snapshot.AppendLine();
        }
        finally
        {
            foreach (var doc in docs)
            {
                doc.Dispose();
            }
        }
    }
}
