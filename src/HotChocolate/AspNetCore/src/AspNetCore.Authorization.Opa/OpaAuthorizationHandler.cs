using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Authorization;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HotChocolate.AspNetCore.Authorization;

/// <summary>
/// An implementation that delegates auth to OPA (Open Policy Agent) REST API endpoint
/// </summary>
public sealed class OpaAuthorizationHandler : IAuthorizationHandler
{
    /// <summary>
    /// Authorize current directive using OPA (Open Policy Agent).
    /// </summary>
    /// <param name="context">The current middleware context.</param>
    /// <param name="directive">The authorization directive.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// Returns a value indicating if the current session is authorized to
    /// access the resolver data.
    /// </returns>
    public async ValueTask<AuthorizeResult> AuthorizeAsync(
        IMiddlewareContext context,
        AuthorizeDirective directive,
        CancellationToken cancellationToken)
    {
        var opa = context.Services.GetRequiredService<IOpaService>();
        var factory = context.Services.GetRequiredService<IOpaQueryRequestFactory>();
        var options = context.Services.GetRequiredService<IOptions<OpaOptions>>();

        var path = directive.Policy ?? string.Empty;
        var request = factory.CreateRequest(context, directive);

        var response = await opa.QueryAsync(path, request, cancellationToken).ConfigureAwait(false);

        if (response is null)
        {
            throw new InvalidOperationException("Opa response must not be null");
        }

        return await options.Value.GetResultHandlerFor(path)
            .HandleAsync(path, httpResponse, context)
            .ConfigureAwait(false);
    }

    public async ValueTask<AuthorizeResult> AuthorizeAsync(
        AuthorizationContext context,
        IReadOnlyList<AuthorizeDirective> directives,
        CancellationToken cancellationToken)
    {
        var opaService = context.Services.GetRequiredService<IOpaService>();
        var factory = context.Services.GetRequiredService<IOpaQueryRequestFactory>();
        var options = context.Services.GetRequiredService<IOptions<OpaOptions>>();

        foreach (var directive in directives)
        {
            var policyPath = directive.Policy ?? string.Empty;

            var httpResponse = await opaService.QueryAsync(
                    policyPath,
                    factory.CreateRequest(context, directive),
                    cancellationToken)
                .ConfigureAwait(false);

            if (httpResponse is null)
            {
                throw new InvalidOperationException("Opa response must not be null");
            }

            return await options.Value.GetResultHandlerFor(policyPath)
                .HandleAsync(policyPath, httpResponse, context)
                .ConfigureAwait(false);
        }
    }

    private static async ValueTask<AuthorizeResult> AuthorizeAsync(
        AuthorizationContext context,
        IOpaService opa,
        IOpaQueryRequestFactory requestFactory,
        IOptions<OpaOptions> options,
        AuthorizeDirective directive,
        CancellationToken cancellationToken)
    {
        var request = requestFactory.CreateRequest(context, directive);

        var path = directive.Policy ?? string.Empty;
        var response = await opa.QueryAsync(path, request, cancellationToken).ConfigureAwait(false);

        if (response is null)
        {
            throw new InvalidOperationException("Opa response must not be null");
        }

        return await options.Value
            .GetResultHandlerFor(path)
            .HandleAsync(path, response, context)
            .ConfigureAwait(false);
    }
}
