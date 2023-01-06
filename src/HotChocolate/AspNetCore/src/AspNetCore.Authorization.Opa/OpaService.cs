using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace HotChocolate.AspNetCore.Authorization;

internal sealed class OpaService : IOpaService
{
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

        using var body = request.ToJsonContent(_options.JsonSerializerOptions);
        using var response = await _client.PostAsync(policyPath, body, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        var document = await JsonDocument.ParseAsync(stream, default, ct);
        return new OpaQueryResponse(document);
    }
}
