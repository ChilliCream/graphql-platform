using Microsoft.Extensions.Options;

namespace HotChocolate.AspNetCore.Authorization;

public class DefaultPolicyResultHandler : PolicyResultHandlerBase<QueryResponse<bool?>, ResponseBase>
{
    public DefaultPolicyResultHandler(IOptions<OpaOptions> options) : base(options) { }
    protected override Task<ResponseBase> ProcessAsync(PolicyResultContext<QueryResponse<bool?>> context)
    {
        return Task.FromResult<ResponseBase>(context.Result switch
        {
            { Result: null } when context.PolicyPath.Equals(string.Empty) => NoDefaultPolicy.Response,
            { Result: null } => PolicyNotFound.Response,
            _ => new QueryResponse<bool?> { Result = false }
        });
    }
}
