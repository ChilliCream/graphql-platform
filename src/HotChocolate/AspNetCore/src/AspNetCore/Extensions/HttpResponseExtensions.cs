using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore
{
    internal static class HttpResponseExtensions
    {
        private static readonly JsonSerializerOptions _serializerOptions = new()
        {
#if NETCOREAPP3_1
            IgnoreNullValues = true,
#else
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
#endif
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
