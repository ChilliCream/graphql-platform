using System.Net;
using System.Text.Json;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Services;

internal sealed class NitroApiService : IDisposable
{
    private const string HttpClientName = "nitro-api";

    private readonly HttpClient _httpClient;
    private readonly GraphQLHttpClient _graphQLClient;

    public NitroApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient(HttpClientName);
        _graphQLClient = GraphQLHttpClient.Create(_httpClient, disposeHttpClient: false);
    }

    public async Task<SchemaDownloadResult> DownloadSchemaAsync(
        string apiId,
        string stage,
        CancellationToken cancellationToken)
    {
        var encodedApiId = Uri.EscapeDataString(apiId);
        var encodedStage = Uri.EscapeDataString(stage);

        using var response = await _httpClient.GetAsync(
            "/api/v1/apis/" + encodedApiId + "/schemas/latest/download?stage=" + encodedStage,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = MapHttpError(response.StatusCode, apiId, stage);
            return SchemaDownloadResult.Failure(errorMessage);
        }

        var sdl = await response.Content.ReadAsStringAsync(cancellationToken);
        return SchemaDownloadResult.Ok(sdl);
    }

    public async Task<string?> ResolveStageIdAsync(string apiId, string stageName, CancellationToken cancellationToken)
    {
        const string stageQuery = "query($apiId: ID!) { node(id: $apiId) { ... on Api { stages { id name } } } }";

        var variables = new Dictionary<string, object?> { ["apiId"] = apiId };

        var result = await ExecuteGraphQLAsync(
            new OperationRequest(stageQuery, variables: variables),
            cancellationToken);

        var data = result.Data;
        if (data.ValueKind == JsonValueKind.Undefined
            || !data.TryGetProperty("node", out var node)
            || !node.TryGetProperty("stages", out var stages))
        {
            return null;
        }

        foreach (var stage in stages.EnumerateArray())
        {
            if (stage.TryGetProperty("name", out var nameElement)
                && nameElement.GetString() == stageName)
            {
                return stage.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;
            }
        }

        return null;
    }

    public async Task<OperationResult> ExecuteGraphQLAsync(
        OperationRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await _graphQLClient.PostAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.ReadAsResultAsync(cancellationToken);
    }

    private static string MapHttpError(HttpStatusCode status, string apiId, string stage)
        => status switch
        {
            HttpStatusCode.Unauthorized => "Authentication failed. Run 'nitro login' to re-authenticate.",
            HttpStatusCode.Forbidden => "Access denied. You do not have Schemas.Read permission for this API.",
            HttpStatusCode.NotFound => "No published schema found for stage '"
                + stage
                + "' on API '"
                + apiId
                + "'. Ensure the API exists and a schema has been published to this stage.",
            _ => "Nitro API server error (" + (int)status + "). Try again later."
        };

    public void Dispose()
    {
        _graphQLClient.Dispose();
        _httpClient.Dispose();
    }
}

internal sealed class SchemaDownloadResult
{
    public bool IsSuccess { get; private init; }
    public string Sdl { get; private init; } = string.Empty;
    public string? ErrorMessage { get; private init; }

    public static SchemaDownloadResult Ok(string sdl) => new() { IsSuccess = true, Sdl = sdl };

    public static SchemaDownloadResult Failure(string errorMessage)
        => new() { IsSuccess = false, ErrorMessage = errorMessage };
}
