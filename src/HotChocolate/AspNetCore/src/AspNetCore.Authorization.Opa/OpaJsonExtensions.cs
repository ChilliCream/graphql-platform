#if NET6_0
using System.Net.Http.Json;
#endif
using System.Text.Json;

namespace HotChocolate.AspNetCore.Authorization;

internal static class OpaJsonExtensions
{
    internal static HttpContent ToJsonContent(this QueryRequest request, JsonSerializerOptions options)
    {
#if NET6_0
        return JsonContent.Create(request, options: options);
#else
        var body = JsonSerializer.Serialize(request, options);
        return new StringContent(body,  System.Text.Encoding.UTF8, "application/json");
#endif
    }

    internal static async Task<QueryResponse?> QueryResponseFromJsonAsync(this HttpContent content, JsonSerializerOptions options, CancellationToken token)
    {
#if NET6_0
        return await content.ReadFromJsonAsync<QueryResponse>(options, token);
#else
        return await JsonSerializer.DeserializeAsync<QueryResponse>(await content.ReadAsStreamAsync(), options, token);
#endif
    }
}
