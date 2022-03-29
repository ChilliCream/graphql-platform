using HotChocolate.Resolvers;
using Microsoft.Extensions.Options;

namespace HotChocolate.AspNetCore.Authorization;

public abstract class PolicyResultHandlerBase<T, TOutput> : IPolicyResultHandler
    where TOutput : ResponseBase
{
    private readonly IOptions<OpaOptions> _options;
    protected PolicyResultHandlerBase(IOptions<OpaOptions> options) => _options = options;
    protected abstract Task<TOutput> ProcessAsync(PolicyResultContext<T> context);

    public async Task<ResponseBase?> HandleAsync(string policyPath, HttpResponseMessage response,
        IMiddlewareContext context)
    {
        QueryResponse<T?> responseObj = await response.Content
            .FromJsonAsync<QueryResponse<T?>>(_options.Value.JsonSerializerOptions, context.RequestAborted)
            .ConfigureAwait(false);
        return await ProcessAsync(new PolicyResultContext<T>(policyPath, responseObj.Result, context));
    }
}
