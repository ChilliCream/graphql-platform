using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore.Authorization;

/// <summary>
/// An implementation that delegates authz to OPA (Open Policy Agent) REST API endpoint
/// </summary>
public class OpaAuthorizationHandler : IAuthorizationHandler
{
    /// <summary>
    /// Authorize current directive using OPA (Open Policy Agent).
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
        IOpaQueryRequestFactory? factory = context.Services.GetRequiredService<IOpaQueryRequestFactory>();
      
        ResponseBase? response = await opaService.QueryAsync(directive.Policy ?? string.Empty, factory.CreateRequest(context, directive), context.RequestAborted);
        return opaDecision.Map(response);
    }
}
