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

public sealed class OriginalRequest
{
}

public sealed class Input
{
    public OriginalRequest Request { get; set; } = new();
    public static readonly Input Empty = new();
}

public interface IOpaService
{
    Task<QueryResponse?> QueryAsync(string policyPath, QueryRequest request, CancellationToken token);
}

public sealed class OpaOptions
{
    public Uri BaseAddress { get; set; } = new Uri("http://127.0.0.1:8181");
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromMilliseconds(250);
    public JsonSerializerOptions JsonSerializerOptions { get; set; } = new JsonSerializerOptions();
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
        OpaService? opaService = context.Services.GetRequiredService<OpaService>();
        IOpaDecision? opaDecision = context.Services.GetRequiredService<IOpaDecision>();
        
        var request = new QueryRequest { Input = new Input()};
        QueryResponse? response = await opaService.QueryAsync(directive.Policy ?? string.Empty, request, context.RequestAborted);
        return opaDecision.Map(response);
    }
}
