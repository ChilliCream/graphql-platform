using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake.Transport.Http;

internal static class ResponseHelper
{
    public static async Task<Response<JsonDocument>> TryParseResponse(
        this Stream stream,
        CancellationToken cancellationToken)
    {
        try
        {
            JsonDocument document =
                await JsonDocument.ParseAsync(
                        stream,
                        new JsonDocumentOptions {CommentHandling = JsonCommentHandling.Skip},
                        cancellationToken)
                    .ConfigureAwait(false);
            return new Response<JsonDocument>(document, null);
        }
        catch(Exception ex)
        {
            return new Response<JsonDocument>(null, ex);
        }
    }
}
