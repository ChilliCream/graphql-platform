using System.Buffers;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace CookieCrumble.Formatters;

internal sealed class GraphQLHttpResponseFormatter : SnapshotValueFormatter<HttpResponseMessage>
{
    protected override void Format(IBufferWriter<byte> snapshot, HttpResponseMessage value)
    {
        var contentType = value.Content.Headers.ContentType;

        if (string.Equals(contentType?.MediaType, "multipart/mixed", StringComparison.Ordinal))
        {
            var boundary = contentType!.Parameters.First(
                t => string.Equals(t.Name, "boundary", StringComparison.Ordinal));
            FormatStreamAsync(snapshot, boundary, value.Content.ReadAsStream()).Wait();
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