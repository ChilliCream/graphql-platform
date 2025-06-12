using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using static System.Globalization.CultureInfo;
using static System.String;

namespace HotChocolate.AspNetCore;

internal static class HttpResponseExtensions
{
    private const string ContentDispositionHeader = "Content-Disposition";
    private const string ContentDispositionValue = "attachment; filename=\"{0}\"";
    private static readonly JsonSerializerOptions s_serializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    internal static Task WriteAsJsonAsync<TValue>(
        this HttpResponse response,
        TValue value,
        CancellationToken cancellationToken = default)
    {
        response.ContentType = ContentType.Json;
        response.StatusCode = 200;

        return JsonSerializer.SerializeAsync(
            response.Body,
            value,
            s_serializerOptions,
            cancellationToken);
    }

    public static IHeaderDictionary SetContentDisposition(
        this IHeaderDictionary headers,
        string fileName)
    {
        headers[ContentDispositionHeader] =
            Format(InvariantCulture, ContentDispositionValue, fileName);
        return headers;
    }
}
