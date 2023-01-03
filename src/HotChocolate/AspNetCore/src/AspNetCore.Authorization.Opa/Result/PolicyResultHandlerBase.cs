using System;
using System.Net.Http;
using System.Threading.Tasks;
using HotChocolate.Authorization;
using HotChocolate.Resolvers;
using Microsoft.Extensions.Options;

namespace HotChocolate.AspNetCore.Authorization;

public abstract class PolicyResultHandlerBase<T> : IPolicyResultHandler
{
    private readonly IOptions<OpaOptions> _options;

    protected static readonly IOpaAuthzResult<T> PolicyNotFoundResult =
        new OpaAuthResult<T>(AuthorizeResult.PolicyNotFound, default);

    protected PolicyResultHandlerBase(IOptions<OpaOptions> options)
        => _options = options;

    protected abstract Task<IOpaAuthzResult<T>> MakeDecision(PolicyResultContext<T> context);

    protected virtual Task OnAllowed(IMiddlewareContext context, IOpaAuthzResult<T> result)
        => Task.CompletedTask;

    protected virtual Task OnNotAllowed(IMiddlewareContext context, IOpaAuthzResult<T> result)
        => Task.CompletedTask;

    protected virtual Task OnPolicyNotFound(IMiddlewareContext context, IOpaAuthzResult<T> result)
        => Task.CompletedTask;

    protected virtual Task OnNotAuthenticated(IMiddlewareContext context, IOpaAuthzResult<T> result)
        => Task.CompletedTask;

    protected virtual Task OnNoDefaultPolicy(IMiddlewareContext context, IOpaAuthzResult<T> result)
        => Task.CompletedTask;

    public async Task<AuthorizeResult> HandleAsync(
        string policyPath,
        HttpResponseMessage response,
        IMiddlewareContext context)
    {
        async Task<T> Deserialize()
        {
            var responseObj = await response.Content
                .FromJsonAsync<QueryResponse<T?>>(
                    _options.Value.JsonSerializerOptions,
                    context.RequestAborted)
                .ConfigureAwait(false);
            return responseObj is { Result: { } result }
                ? result
                : throw new InvalidOperationException(
                    "Opa deserialized response must not be null");
        }

        // The server returns 200 if the path refers to an undefined document.
        // In this case, the response will not contain a result property.
        // https://www.openpolicyagent.org/docs/latest/rest-api/#get-a-document
        const string emptyDocument = "{}";
        var policyNotFound =
            response.Content.Headers.ContentLength == 2 &&
            await response.Content.ReadAsStringAsync().ConfigureAwait(false) == emptyDocument;

        var opaResult = policyNotFound
            ? PolicyNotFoundResult
            : await MakeDecision(
                new PolicyResultContext<T>(
                    policyPath,
                    await Deserialize().ConfigureAwait(false),
                    context)).ConfigureAwait(false);

        switch (opaResult.Result)
        {
            case AuthorizeResult.Allowed:
                await OnAllowed(context, opaResult).ConfigureAwait(false);
                break;

            case AuthorizeResult.NotAllowed:
                await OnNotAllowed(context, opaResult).ConfigureAwait(false);
                break;

            case AuthorizeResult.NotAuthenticated:
                await OnNotAuthenticated(context, opaResult).ConfigureAwait(false);
                break;

            case AuthorizeResult.NoDefaultPolicy:
                await OnNoDefaultPolicy(context, opaResult).ConfigureAwait(false);
                break;

            case AuthorizeResult.PolicyNotFound:
                await OnPolicyNotFound(context, opaResult).ConfigureAwait(false);
                break;

            default:
                throw new ArgumentOutOfRangeException($"{opaResult.Result}");
        }

        return opaResult.Result;
    }
}
