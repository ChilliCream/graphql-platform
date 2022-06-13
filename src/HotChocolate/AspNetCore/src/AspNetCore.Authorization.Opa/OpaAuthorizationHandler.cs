using System;
using System.Net.Http;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        IOpaQueryRequestFactory? factory = context.Services.GetRequiredService<IOpaQueryRequestFactory>();
        IOptions<OpaOptions> options = context.Services.GetRequiredService<IOptions<OpaOptions>>();

        var policyPath = directive.Policy ?? string.Empty;

        HttpResponseMessage? httpResponse = await opaService.QueryAsync(policyPath,
            factory.CreateRequest(context, directive), context.RequestAborted).ConfigureAwait(false);

        if (httpResponse is null) throw new InvalidOperationException("Opa response must not be null");

        return await options.Value.GetResultHandlerFor(policyPath).HandleAsync(policyPath, httpResponse, context)
            .ConfigureAwait(false);
    }
}
