using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static StrawberryShake.ResultFields;

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
                        new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip },
                        cancellationToken)
                    .ConfigureAwait(false);

            if (document.RootElement.ValueKind is JsonValueKind.Object)
            {
                var hasNext = false;
                var isPatch = document.RootElement.TryGetProperty(ResultFields.Path, out _);

                if (document.RootElement.TryGetProperty(HasNext, out JsonElement hasNextProp) &&
                   hasNextProp.GetBoolean())
                {
                    hasNext = true;
                }

                return new Response<JsonDocument>(document, null, isPatch, hasNext);
            }

            return new Response<JsonDocument>(document, null);
        }
        catch (Exception ex)
        {
            return new Response<JsonDocument>(null, ex);
        }
    }
}
