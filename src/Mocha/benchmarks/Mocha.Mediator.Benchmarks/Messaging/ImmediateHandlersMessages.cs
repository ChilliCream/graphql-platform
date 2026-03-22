using Immediate.Handlers.Shared;

namespace Mocha.Mediator.Benchmarks.Messaging;

// Command handler for Immediate.Handlers (source-generated)
[Handler]
public static partial class ImmediateCommandHandler
{
    public sealed record Command(Guid Id);

    private static readonly BenchmarkResponse _response = new(Guid.NewGuid());

    private static ValueTask<BenchmarkResponse> HandleAsync(
        Command command,
        CancellationToken ct)
        => new(_response);
}

// Pipeline behavior for Immediate.Handlers
public sealed class ImmediateBenchmarkBehavior<TRequest, TResponse>
    : Behavior<TRequest, TResponse>
{
    public override async ValueTask<TResponse> HandleAsync(
        TRequest request,
        CancellationToken cancellationToken)
    {
        return await Next(request, cancellationToken);
    }
}

// Separate handler with pipeline behavior for pipeline benchmarks
[Handler]
[Behaviors(typeof(ImmediateBenchmarkBehavior<,>))]
public static partial class ImmediatePipelineCommandHandler
{
    public sealed record Command(Guid Id);

    private static readonly BenchmarkResponse _response = new(Guid.NewGuid());

    private static ValueTask<BenchmarkResponse> HandleAsync(
        Command command,
        CancellationToken ct)
        => new(_response);
}
