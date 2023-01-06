#if NET6_0
using System.Net.Http.Json;
#endif
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.AspNetCore.Authorization;

internal static class OpaJsonExtensions
{
    internal static HttpContent ToJsonContent(
        this OpaQueryRequest request,
        JsonSerializerOptions options)
    {
#if NET6_0
        return JsonContent.Create(request, options: options);
#else
        var body = JsonSerializer.Serialize(request, options);
        return new StringContent(body, System.Text.Encoding.UTF8, "application/json");
#endif
    }

    internal static async Task<T?> FromJsonAsync<T>(
        this HttpContent content,
        JsonSerializerOptions options,
        CancellationToken token)
    {
#if NET6_0
        return await content.ReadFromJsonAsync<T>(options, token).ConfigureAwait(false);
#else
        return await JsonSerializer
            .DeserializeAsync<T>(
                await content.ReadAsStreamAsync().ConfigureAwait(false), options, token)
            .ConfigureAwait(false);
#endif
    }
}
