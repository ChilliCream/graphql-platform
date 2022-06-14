using System;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using Microsoft.Extensions.Options;

namespace HotChocolate.AspNetCore.Authorization;

public delegate Task OnAfterResult<in T>(IMiddlewareContext context, IOpaAuthzResult<T> result);
public class DelegatePolicyResultHandler<T> : PolicyResultHandlerBase<T>
{
    public OnAfterResult<T>? OnAllowedFunc { get; set; }
    public OnAfterResult<T>? OnNotAllowedFunc { get; set; }
    public OnAfterResult<T>? OnNotAuthenticatedFunc { get; set; }
    public OnAfterResult<T>? OnNoDefaultPolicyFunc { get; set; }
    public OnAfterResult<T>? OnPolicyNotFoundFunc { get; set; }

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

    protected override Task OnNotAllowed(IMiddlewareContext context, IOpaAuthzResult<T> result)
    {
        if (OnNotAllowedFunc is { } func) return func(context, result);
        return Task.CompletedTask;
    }

    protected override Task OnNotAuthenticated(IMiddlewareContext context, IOpaAuthzResult<T> result)
    {
        if (OnNotAuthenticatedFunc is { } func) return func(context, result);
        return Task.CompletedTask;
    }

    protected override Task OnNoDefaultPolicy(IMiddlewareContext context, IOpaAuthzResult<T> result)
    {
        if (OnNoDefaultPolicyFunc is { } func) return func(context, result);
        return Task.CompletedTask;
    }

    protected override Task OnPolicyNotFound(IMiddlewareContext context, IOpaAuthzResult<T> result)
    {
        if (OnPolicyNotFoundFunc is { } func) return func(context, result);
        return Task.CompletedTask;
    }
}
