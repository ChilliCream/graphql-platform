using Microsoft.Extensions.Options;

namespace HotChocolate.AspNetCore.Authorization;

public class DelegatePolicyResultHandler<T, TOutput> : PolicyResultHandlerBase<T, TOutput>
    where TOutput : ResponseBase
{
    private readonly Func<PolicyResultContext<T>, Task<TOutput>> _process;
    public DelegatePolicyResultHandler(Func<PolicyResultContext<T>, Task<TOutput>> process, IOptions<OpaOptions> options) : base(options) => _process = process;
    protected override Task<TOutput> ProcessAsync(PolicyResultContext<T> context) => _process(context);
}
