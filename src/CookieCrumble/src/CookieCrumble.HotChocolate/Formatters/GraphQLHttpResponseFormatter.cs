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

        if (string.Equals(contentType?.MediaType, "multipart/mixed", StringComparison.Ordinal))
        {
            var boundary = contentType!.Parameters.First(
                t => string.Equals(t.Name, "boundary", StringComparison.Ordinal));
            FormatStreamAsync(snapshot, boundary, value.HttpResponseMessage.Content.ReadAsStream()).Wait();
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
