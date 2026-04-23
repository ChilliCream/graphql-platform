using System.Buffers;
using System.Net.Http.Headers;
using System.Text.Json;
using CookieCrumble.Formatters;
using HotChocolate.Transport.Http;
using Microsoft.AspNetCore.WebUtilities;

namespace CookieCrumble.HotChocolate.Formatters;

internal sealed class GraphQLHttpResponseFormatter : SnapshotValueFormatter<GraphQLHttpResponse>
{
    protected override void Format(IBufferWriter<byte> snapshot, GraphQLHttpResponse value)
    {
        var contentType = value.ContentHeaders.ContentType;
        var mediaType = contentType?.MediaType;

        if (string.Equals(mediaType, "multipart/mixed", StringComparison.Ordinal))
        {
            var boundary = contentType!.Parameters.First(
                t => string.Equals(t.Name, "boundary", StringComparison.Ordinal));
            FormatStreamAsync(snapshot, boundary, value.HttpResponseMessage.Content.ReadAsStream()).Wait();
        }
        else if (string.Equals(mediaType, "application/jsonl", StringComparison.Ordinal)
            || string.Equals(mediaType, "application/graphql-response+jsonl", StringComparison.Ordinal)
            || string.Equals(mediaType, "text/event-stream", StringComparison.Ordinal))
        {
            FormatResultStreamAsync(snapshot, value).Wait();
        }
    }

    private static async Task FormatResultStreamAsync(
        IBufferWriter<byte> snapshot,
        GraphQLHttpResponse response)
    {
        var content = await response.HttpResponseMessage.Content.ReadAsStringAsync();
        var docs = new List<JsonDocument>();
        var patcher = new JsonResultPatcher();
        var first = true;

        try
        {
            foreach (var line in content.Split('\n'))
            {
                var trimmed = line.Trim();

                // skip empty lines, SSE field names, and keep-alive comments
                if (trimmed.Length == 0
                    || trimmed.StartsWith("event:", StringComparison.Ordinal)
                    || trimmed.StartsWith(":", StringComparison.Ordinal))
                {
                    continue;
                }

                // strip SSE "data: " prefix if present
                var json = trimmed.StartsWith("data:", StringComparison.Ordinal)
                    ? trimmed["data:".Length..].Trim()
                    : trimmed;

                if (json.Length == 0 || json[0] != '{')
                {
                    continue;
                }

                var doc = JsonDocument.Parse(json);
                docs.Add(doc);

                if (first)
                {
                    patcher.SetResponse(doc);
                    first = false;
                }
                else
                {
                    patcher.ApplyPatch(doc);
                }
            }

            patcher.WriteResponse(snapshot);
        }
        finally
        {
            foreach (var doc in docs)
            {
                doc.Dispose();
            }
        }
    }

    private static async Task FormatStreamAsync(
        IBufferWriter<byte> snapshot,
        NameValueHeaderValue boundary,
        Stream body)
    {
        var reader = new MultipartReader(boundary.Value!.Trim('"'), body);

        var docs = new List<JsonDocument>();
        var patcher = new JsonResultPatcher();
        var first = true;

        try
        {
            var section = await reader.ReadNextSectionAsync().ConfigureAwait(false);

            while (section is not null)
            {
                await using var sectionBody = section.Body;

                if (first)
                {
                    var doc = await JsonDocument.ParseAsync(sectionBody);
                    docs.Add(doc);
                    patcher.SetResponse(doc);
                    first = false;
                }
                else
                {
                    var doc = await JsonDocument.ParseAsync(sectionBody);
                    docs.Add(doc);
                    patcher.ApplyPatch(doc);
                }

                section = await reader.ReadNextSectionAsync().ConfigureAwait(false);
            }

            patcher.WriteResponse(snapshot);
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
