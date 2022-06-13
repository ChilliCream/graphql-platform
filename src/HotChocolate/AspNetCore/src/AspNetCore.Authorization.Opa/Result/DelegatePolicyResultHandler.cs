using System;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using Microsoft.Extensions.Options;

namespace HotChocolate.AspNetCore.Authorization;

public class DelegatePolicyResultHandler<T> : PolicyResultHandlerBase<T>
{
    public Func<IMiddlewareContext, IOpaAuthzResult<T>, Task>? OnAllowedFunc { get; set; }

    private readonly Func<PolicyResultContext<T>, Task<IOpaAuthzResult<T>>> _makeDecision;

    public DelegatePolicyResultHandler(
        Func<PolicyResultContext<T>, Task<IOpaAuthzResult<T>>> makeDecision,
        IOptions<OpaOptions> options) : base(options)
    {
        _makeDecision = makeDecision;
    }

    protected override Task<IOpaAuthzResult<T>> MakeDecision(PolicyResultContext<T> context) => _makeDecision(context);

    protected override Task OnAllowed(IMiddlewareContext context, IOpaAuthzResult<T> result)
    {
        if (OnAllowedFunc is { } func) return func(context, result);
        return Task.CompletedTask;
    }
}
