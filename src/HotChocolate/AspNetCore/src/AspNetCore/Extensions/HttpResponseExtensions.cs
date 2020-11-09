using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Utilities;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore
{
    internal static class HttpResponseExtensions
    {
        private static readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
        {
            IgnoreNullValues = true,
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
                _serializerOptions,
                cancellationToken);
        }
    }
}
