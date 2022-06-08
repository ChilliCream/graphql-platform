using System;
using System.Net.Http;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using Microsoft.Extensions.Options;

namespace HotChocolate.AspNetCore.Authorization;

public abstract class PolicyResultHandlerBase<T> : IPolicyResultHandler
{
    private readonly IOptions<OpaOptions> _options;
    protected PolicyResultHandlerBase(IOptions<OpaOptions> options) => _options = options;
    protected abstract Task<IOpaAuthzResult<T>> MakeDecision(PolicyResultContext<T> context);
    protected virtual Task OnAllowed(IMiddlewareContext context, IOpaAuthzResult<T> result) => Task.CompletedTask;
    protected virtual Task OnNotAllowed(IMiddlewareContext context, IOpaAuthzResult<T> result) => Task.CompletedTask;

    protected virtual Task OnPolicyNotFound(IMiddlewareContext context, IOpaAuthzResult<T> result) =>
        Task.CompletedTask;

    protected virtual Task OnNotAuthenticated(IMiddlewareContext context, IOpaAuthzResult<T> result) =>
        Task.CompletedTask;

    protected virtual Task OnNoDefaultPolicy(IMiddlewareContext context, IOpaAuthzResult<T> result) =>
        Task.CompletedTask;

    public async Task<AuthorizeResult> HandleAsync(string policyPath, HttpResponseMessage response,
        IMiddlewareContext context)
    {
        QueryResponse<T?> responseObj = await response.Content
            .FromJsonAsync<QueryResponse<T?>>(_options.Value.JsonSerializerOptions, context.RequestAborted)
            .ConfigureAwait(false);

        if (responseObj is not { Result: var result })
            throw new InvalidOperationException("Opa deserialized response must not be null");

        IOpaAuthzResult<T> opaAuthzResult = await MakeDecision(new PolicyResultContext<T>(policyPath, result, context))
            .ConfigureAwait(false);

        switch (opaAuthzResult.Result)
        {
            case AuthorizeResult.Allowed:
                await OnAllowed(context, opaAuthzResult);
                break;
            case AuthorizeResult.NotAllowed:
                await OnNotAllowed(context, opaAuthzResult);
                break;
            case AuthorizeResult.NotAuthenticated:
                await OnNotAuthenticated(context, opaAuthzResult);
                break;
            case AuthorizeResult.NoDefaultPolicy:
                await OnNoDefaultPolicy(context, opaAuthzResult);
                break;
            case AuthorizeResult.PolicyNotFound:
                await OnPolicyNotFound(context, opaAuthzResult);
                break;
            default:
                throw new ArgumentOutOfRangeException($"{opaAuthzResult.Result}");
        }

        return opaAuthzResult.Result;
    }
}
