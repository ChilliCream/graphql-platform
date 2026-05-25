using Immediate.Handlers.Shared;

namespace Mocha.Mediator.Benchmarks.Messaging;

// ─── Full Pipeline Command (Mocha + MediatR shared) ───

public sealed record FullPipelineCommand(Guid Id)
    : Mocha.Mediator.ICommand<BenchmarkResponse>,
      MediatR.IRequest<BenchmarkResponse>;

public sealed class FullPipelineCommandHandler
    : Mocha.Mediator.ICommandHandler<FullPipelineCommand, BenchmarkResponse>,
      MediatR.IRequestHandler<FullPipelineCommand, BenchmarkResponse>
{
    private static readonly BenchmarkResponse _response = new(Guid.NewGuid());
    private static readonly Task<BenchmarkResponse> _taskResponse = Task.FromResult(_response);

    public ValueTask<BenchmarkResponse> HandleAsync(
        FullPipelineCommand command,
        CancellationToken cancellationToken)
        => new(_response);

    Task<BenchmarkResponse> MediatR.IRequestHandler<FullPipelineCommand, BenchmarkResponse>.Handle(
        FullPipelineCommand request,
        CancellationToken cancellationToken)
        => _taskResponse;
}

// Mocha pre-processing middleware
public sealed class FullPipelineMochaPreMiddleware
{
    public async ValueTask InvokeAsync(Mocha.Mediator.IMediatorContext context, Mocha.Mediator.MediatorDelegate next)
    {
        // Pre-processing (no-op)
        await next(context);
    }

    public static Mocha.Mediator.MediatorMiddlewareConfiguration Create()
        => new(static (_, next) =>
        {
            var mw = new FullPipelineMochaPreMiddleware();
            return ctx => mw.InvokeAsync(ctx, next);
        }, "Pre");
}

// Mocha pipeline behavior middleware
public sealed class FullPipelineMochaBehaviorMiddleware
{
    public async ValueTask InvokeAsync(Mocha.Mediator.IMediatorContext context, Mocha.Mediator.MediatorDelegate next)
    {
        await next(context);
    }

    public static Mocha.Mediator.MediatorMiddlewareConfiguration Create()
        => new(static (_, next) =>
        {
            var mw = new FullPipelineMochaBehaviorMiddleware();
            return ctx => mw.InvokeAsync(ctx, next);
        }, "Behavior");
}

// Mocha post-processing middleware
public sealed class FullPipelineMochaPostMiddleware
{
    public async ValueTask InvokeAsync(Mocha.Mediator.IMediatorContext context, Mocha.Mediator.MediatorDelegate next)
    {
        await next(context);
        // Post-processing (no-op)
    }

    public static Mocha.Mediator.MediatorMiddlewareConfiguration Create()
        => new(static (_, next) =>
        {
            var mw = new FullPipelineMochaPostMiddleware();
            return ctx => mw.InvokeAsync(ctx, next);
        }, "Post");
}

// MediatR pre-processor
public sealed class FullPipelineMediatRPreProcessor
    : MediatR.Pipeline.IRequestPreProcessor<FullPipelineCommand>
{
    public Task Process(FullPipelineCommand request, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

// MediatR post-processor
public sealed class FullPipelineMediatRPostProcessor
    : MediatR.Pipeline.IRequestPostProcessor<FullPipelineCommand, BenchmarkResponse>
{
    public Task Process(FullPipelineCommand request, BenchmarkResponse response, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

// MediatR pipeline behavior
public sealed class FullPipelineMediatRBehavior
    : MediatR.IPipelineBehavior<FullPipelineCommand, BenchmarkResponse>
{
    public async Task<BenchmarkResponse> Handle(
        FullPipelineCommand request,
        MediatR.RequestHandlerDelegate<BenchmarkResponse> next,
        CancellationToken cancellationToken)
        => await next();
}

// ─── MediatorSg (martinothamar) - native pre/post processors + 1 behavior ───

public sealed record FullPipelineMediatorSgCommand(Guid Id)
    : global::Mediator.IRequest<BenchmarkResponse>;

public sealed class FullPipelineMediatorSgCommandHandler
    : global::Mediator.IRequestHandler<FullPipelineMediatorSgCommand, BenchmarkResponse>
{
    private static readonly BenchmarkResponse _response = new(Guid.NewGuid());

    public ValueTask<BenchmarkResponse> Handle(
        FullPipelineMediatorSgCommand request,
        CancellationToken cancellationToken)
        => new(_response);
}

// Native pre-processor (inherits MessagePreProcessor which implements IPipelineBehavior)
public sealed class FullPipelineMediatorSgPreProcessor
    : global::Mediator.MessagePreProcessor<FullPipelineMediatorSgCommand, BenchmarkResponse>
{
    protected override ValueTask Handle(
        FullPipelineMediatorSgCommand message,
        CancellationToken cancellationToken)
        => default;
}

// Pipeline behavior
public sealed class FullPipelineMediatorSgBehavior
    : global::Mediator.IPipelineBehavior<FullPipelineMediatorSgCommand, BenchmarkResponse>
{
    public ValueTask<BenchmarkResponse> Handle(
        FullPipelineMediatorSgCommand message,
        global::Mediator.MessageHandlerDelegate<FullPipelineMediatorSgCommand, BenchmarkResponse> next,
        CancellationToken cancellationToken)
        => next(message, cancellationToken);
}

// Native post-processor (inherits MessagePostProcessor which implements IPipelineBehavior)
public sealed class FullPipelineMediatorSgPostProcessor
    : global::Mediator.MessagePostProcessor<FullPipelineMediatorSgCommand, BenchmarkResponse>
{
    protected override ValueTask Handle(
        FullPipelineMediatorSgCommand message,
        BenchmarkResponse response,
        CancellationToken cancellationToken)
        => default;
}

// ─── SwitchMediator - 3 behaviors to match pipeline depth ───

public sealed record FullPipelineSwitchMediatorCommand(Guid Id)
    : global::Mediator.Switch.IRequest<BenchmarkResponse>;

public sealed class FullPipelineSwitchMediatorCommandHandler
    : global::Mediator.Switch.IValueRequestHandler<FullPipelineSwitchMediatorCommand, BenchmarkResponse>
{
    private static readonly BenchmarkResponse _response = new(Guid.NewGuid());

    public ValueTask<BenchmarkResponse> Handle(
        FullPipelineSwitchMediatorCommand request,
        CancellationToken cancellationToken)
        => new(_response);
}

public sealed class FullPipelineSwitchMediatorPreBehavior
    : global::Mediator.Switch.IValuePipelineBehavior<FullPipelineSwitchMediatorCommand, BenchmarkResponse>
{
    public ValueTask<BenchmarkResponse> Handle(
        FullPipelineSwitchMediatorCommand request,
        global::Mediator.Switch.ValueRequestHandlerDelegate<BenchmarkResponse> next,
        CancellationToken cancellationToken)
        => next(cancellationToken);
}

public sealed class FullPipelineSwitchMediatorMainBehavior
    : global::Mediator.Switch.IValuePipelineBehavior<FullPipelineSwitchMediatorCommand, BenchmarkResponse>
{
    public ValueTask<BenchmarkResponse> Handle(
        FullPipelineSwitchMediatorCommand request,
        global::Mediator.Switch.ValueRequestHandlerDelegate<BenchmarkResponse> next,
        CancellationToken cancellationToken)
        => next(cancellationToken);
}

public sealed class FullPipelineSwitchMediatorPostBehavior
    : global::Mediator.Switch.IValuePipelineBehavior<FullPipelineSwitchMediatorCommand, BenchmarkResponse>
{
    public ValueTask<BenchmarkResponse> Handle(
        FullPipelineSwitchMediatorCommand request,
        global::Mediator.Switch.ValueRequestHandlerDelegate<BenchmarkResponse> next,
        CancellationToken cancellationToken)
        => next(cancellationToken);
}

// ─── Immediate.Handlers - 3 behaviors to match pipeline depth ───

public sealed class ImmediateFullPipelinePreBehavior<TRequest, TResponse>
    : Behavior<TRequest, TResponse>
{
    public override async ValueTask<TResponse> HandleAsync(
        TRequest request,
        CancellationToken cancellationToken)
        => await Next(request, cancellationToken);
}

public sealed class ImmediateFullPipelineMainBehavior<TRequest, TResponse>
    : Behavior<TRequest, TResponse>
{
    public override async ValueTask<TResponse> HandleAsync(
        TRequest request,
        CancellationToken cancellationToken)
        => await Next(request, cancellationToken);
}

public sealed class ImmediateFullPipelinePostBehavior<TRequest, TResponse>
    : Behavior<TRequest, TResponse>
{
    public override async ValueTask<TResponse> HandleAsync(
        TRequest request,
        CancellationToken cancellationToken)
        => await Next(request, cancellationToken);
}

[Handler]
[Behaviors(
    typeof(ImmediateFullPipelinePreBehavior<,>),
    typeof(ImmediateFullPipelineMainBehavior<,>),
    typeof(ImmediateFullPipelinePostBehavior<,>))]
public static partial class ImmediateFullPipelineCommandHandler
{
    public sealed record Command(Guid Id);

    private static readonly BenchmarkResponse _response = new(Guid.NewGuid());

    private static ValueTask<BenchmarkResponse> HandleAsync(
        Command command,
        CancellationToken ct)
        => new(_response);
}
