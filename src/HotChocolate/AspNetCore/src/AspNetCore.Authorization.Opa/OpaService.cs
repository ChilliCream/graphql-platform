namespace HotChocolate.AspNetCore.Authorization;

public sealed class OpaService : IOpaService
{
    private readonly HttpClient _httpClient;
    private readonly OpaOptions _options;

    public OpaService(HttpClient httpClient, OpaOptions options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<QueryResponse?> QueryAsync(string policyPath, QueryRequest request, CancellationToken token)
    {
        if (policyPath is null) throw new ArgumentNullException(nameof(policyPath));
        if (request is null) throw new ArgumentNullException(nameof(request));

        HttpResponseMessage? response = await _httpClient.PostAsync(policyPath,  request.ToJsonContent(_options.JsonSerializerOptions), token);
        response.EnsureSuccessStatusCode();
        return await response.Content.QueryResponseFromJsonAsync(_options.JsonSerializerOptions, token);
    }
}
