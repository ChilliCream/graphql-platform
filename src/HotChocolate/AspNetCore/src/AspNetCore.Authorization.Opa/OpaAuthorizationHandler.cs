#if NET6_0
using System.Net.Http.Json;
#endif
using System.Text.Json;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore.Authorization;

public sealed class QueryResponse
{
    public Guid? DecisionId { get; set; }
    public bool Result { get; set; }
}

public sealed class QueryRequest
{
    public Input Input { get; set; } = Input.Empty;
}

internal static class OpaHttpExtensions
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

public sealed class Input
{
    public static readonly Input Empty = new();
}

public interface IOpaService
{
    Task<QueryResponse?> QueryAsync(string policyPath, QueryRequest request, CancellationToken token);
}

public sealed class OpaOptions
{
    public JsonSerializerOptions JsonSerializerOptions { get; set; } = new JsonSerializerOptions();
}

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


public interface IOpaDecision
{
    AuthorizeResult Map(QueryResponse? response);
}

public sealed class OpaDecision : IOpaDecision
{
    public AuthorizeResult Map(QueryResponse? response)
    {
        if (response is not null && response.Result)
        {
            return AuthorizeResult.Allowed;
        }
        return AuthorizeResult.NotAllowed;
    }
}

/// <summary>
/// An implementation that delegates authz to OPA (Open Policy Agent) REST API endpoint
/// </summary>
public class OpaAuthorizationHandler : IAuthorizationHandler
{
    /// <summary>
    /// Authorize current directive using Microsoft.AspNetCore.Authorization.
    /// </summary>
    /// <param name="context">The current middleware context.</param>
    /// <param name="directive">The authorization directive.</param>
    /// <returns>
    /// Returns a value indicating if the current session is authorized to
    /// access the resolver data.
    /// </returns>
    public async ValueTask<AuthorizeResult> AuthorizeAsync(
        IMiddlewareContext context,
        AuthorizeDirective directive)
    {
        IOpaService? opaService = context.Services.GetRequiredService<IOpaService>();
        IOpaDecision? opaDecision = context.Services.GetRequiredService<IOpaDecision>();
        var request = new QueryRequest { Input = new Input()};
        QueryResponse? response = await opaService.QueryAsync(directive.Policy ?? string.Empty, request, context.RequestAborted);
        return opaDecision.Map(response);
    }
}
