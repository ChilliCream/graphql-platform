using System.Buffers;
using System.Text.Json;
using CookieCrumble.Formatters;
using HotChocolate.Transport.Http;
using Microsoft.AspNetCore.WebUtilities;
using static CookieCrumble.HotChocolate.Formatters.StableSnapshotHelpers;

namespace CookieCrumble.HotChocolate.Formatters;

/// <summary>
/// Produces a deterministic snapshot representation for incremental HTTP responses.
/// Reads multipart HTTP response bodies and normalizes payload timing/chunking
/// by aggregating pending/incremental/completed entries and writing a stable summary
/// plus the merged final result.
/// </summary>
internal sealed class StableGraphQLHttpResponseFormatter
    : SnapshotValueFormatter<GraphQLHttpResponse>
{
    public StableGraphQLHttpResponseFormatter()
        : base("json")
    {
    }

    protected override void Format(IBufferWriter<byte> snapshot, GraphQLHttpResponse value)
    {
        var contentType = value.ContentHeaders.ContentType;

        if (contentType is null)
        {
            return;
        }

        if (string.Equals(contentType.MediaType, "multipart/mixed", StringComparison.Ordinal))
        {
            var boundary = contentType.Parameters.First(
                t => string.Equals(t.Name, "boundary", StringComparison.Ordinal));
            FormatStreamAsync(snapshot, boundary.Value!.Trim('"'), value.HttpResponseMessage.Content.ReadAsStream())
                .GetAwaiter().GetResult();
            return;
        }

        // Single response (not multipart), format as canonical JSON.
        FormatSingleResponseAsync(snapshot, value.HttpResponseMessage.Content.ReadAsStream())
            .GetAwaiter().GetResult();
    }

    private static async Task FormatSingleResponseAsync(
        IBufferWriter<byte> snapshot,
        Stream body)
    {
        using var doc = await JsonDocument.ParseAsync(body);
        await using var writer = new Utf8JsonWriter(snapshot, IndentedWriterOptions);
        WriteCanonicalJson(writer, doc.RootElement);
        writer.Flush();
        snapshot.AppendLine();
    }

    private static async Task FormatStreamAsync(
        IBufferWriter<byte> snapshot,
        string boundary,
        Stream body)
    {
        var reader = new MultipartReader(boundary, body);
        var docs = new List<JsonDocument>();
        JsonResultPatcher? patcher = null;
        var acc = new StreamAccumulator();

        try
        {
            var section = await reader.ReadNextSectionAsync().ConfigureAwait(false);

            while (section is not null)
            {
                await using var sectionBody = section.Body;
                var doc = await JsonDocument.ParseAsync(sectionBody);
                docs.Add(doc);

                acc.AddPayload(doc.RootElement);

                if (patcher is null)
                {
                    patcher = new JsonResultPatcher();
                    patcher.SetResponse(doc);
                }
                else
                {
                    patcher.ApplyPatch(doc);
                }

                section = await reader.ReadNextSectionAsync().ConfigureAwait(false);
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
