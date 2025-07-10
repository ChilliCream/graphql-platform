using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace HotChocolate.AspNetCore.Authorization;

internal sealed class OpaService : IOpaService
{
    private readonly HttpClient _client;
    private readonly OpaOptions _options;

    public OpaService(HttpClient httpClient, IOptions<OpaOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(httpClient);

        _client = httpClient;
        _options = options.Value;
    }

    public async Task<OpaQueryResponse> QueryAsync(
        string policyPath,
        OpaQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policyPath);
        ArgumentNullException.ThrowIfNull(request);

        using var body = JsonContent.Create(request, options: _options.JsonSerializerOptions);

        using var response = await _client.PostAsync(policyPath, body, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var document = await JsonDocument.ParseAsync(stream, default, cancellationToken);
        return new OpaQueryResponse(document);
    }
}
