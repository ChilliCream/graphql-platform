using System;
using System.Buffers;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Internal;
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
            var document =
                await JsonDocument.ParseAsync(
                        stream,
                        new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip },
                        cancellationToken)
                    .ConfigureAwait(false);

            if (document.RootElement.ValueKind is JsonValueKind.Object)
            {
                var hasNext = false;
                var isPatch = document.RootElement.TryGetProperty(ResultFields.Path, out _);

                if (document.RootElement.TryGetProperty(HasNext, out var hasNextProp) &&
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
            return new Response<JsonDocument>(CreateBodyFromException(ex), ex);
        }
    }

    internal static JsonDocument CreateBodyFromException(Exception exception)
    {
        using var bufferWriter = new ArrayWriter();

        using var jsonWriter = new Utf8JsonWriter(bufferWriter);
        jsonWriter.WriteStartObject();
        jsonWriter.WritePropertyName("errors");
        jsonWriter.WriteStartArray();
        jsonWriter.WriteStartObject();
        jsonWriter.WriteString("message", exception.Message);
        jsonWriter.WriteEndObject();
        jsonWriter.WriteEndArray();
        jsonWriter.WriteEndObject();
        jsonWriter.Flush();

        return JsonDocument.Parse(bufferWriter.Body);
    }
}
