using System;
using System.Net.Http;
#if NET6_0_OR_GREATER
using System.Net.Http.Json;
#else
using System.Text;
#endif
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace HotChocolate.AspNetCore.Authorization;

internal sealed class OpaService : IOpaService
{
#if NETSTANDARD2_0
    private static readonly Encoding _utf8 = Encoding.UTF8;
#endif
    private readonly HttpClient _client;
    private readonly OpaOptions _options;

    public OpaService(HttpClient httpClient, IOptions<OpaOptions> options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _client = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options.Value;
    }

    public async Task<OpaQueryResponse> QueryAsync(
        string policyPath,
        OpaQueryRequest request,
        CancellationToken ct)
    {
        if (policyPath is null)
        {
            throw new ArgumentNullException(nameof(policyPath));
        }

        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

#if NET6_0_OR_GREATER
        using var body = JsonContent.Create(request, options: _options.JsonSerializerOptions);
#else
        var json = JsonSerializer.Serialize(request, _options.JsonSerializerOptions);
        using var body = new StringContent(json, _utf8, "application/json");
#endif

        using var response = await _client.PostAsync(policyPath, body, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
#if NET6_0_OR_GREATER
        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
#else
        using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
        var document = await JsonDocument.ParseAsync(stream, default, ct);
        return new OpaQueryResponse(document);
    }
}
