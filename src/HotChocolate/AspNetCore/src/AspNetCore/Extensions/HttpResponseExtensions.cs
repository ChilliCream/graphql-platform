using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using static System.Globalization.CultureInfo;
using static System.String;

namespace HotChocolate.AspNetCore;

internal static class HttpResponseExtensions
{
    private const string _contentDepositionHeader = "Content-Disposition";
    private const string _contentDepositionValue = "attachment; filename=\"{0}\"";
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
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
            _serializerOptions,
            cancellationToken);
    }

    public static IHeaderDictionary SetContentDisposition(
        this IHeaderDictionary headers,
        string fileName)
    {
        headers[_contentDepositionHeader] =
            Format(InvariantCulture, _contentDepositionValue, fileName);
        return headers;
    }
}
