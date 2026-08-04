namespace Mocha.Mediator.Benchmarks.Messaging;

// Command that implements both Mocha and MediatR interfaces
public sealed record BenchmarkCommand(Guid Id)
    : Mocha.Mediator.ICommand<BenchmarkResponse>,
      MediatR.IRequest<BenchmarkResponse>;

// Response type shared by both
public sealed record BenchmarkResponse(Guid Id);

// Notification that implements both Mocha and MediatR interfaces
public sealed record BenchmarkNotification(Guid Id)
    : Mocha.Mediator.INotification,
      MediatR.INotification;

// Command handler implementing both interfaces
public sealed class BenchmarkCommandHandler
    : Mocha.Mediator.ICommandHandler<BenchmarkCommand, BenchmarkResponse>,
      MediatR.IRequestHandler<BenchmarkCommand, BenchmarkResponse>
{
    private static readonly BenchmarkResponse _response = new(Guid.NewGuid());
    private static readonly Task<BenchmarkResponse> _taskResponse = Task.FromResult(_response);

    public async ValueTask<BenchmarkResponse> HandleAsync(
        BenchmarkCommand command,
        CancellationToken cancellationToken)
        {
            await Task.Yield();
            return _response;
        }

    async Task<BenchmarkResponse> MediatR.IRequestHandler<BenchmarkCommand, BenchmarkResponse>.Handle(
        BenchmarkCommand request,
        CancellationToken cancellationToken)
        {
            await Task.Yield();
            return _response;
        }
}

// Notification handler implementing both interfaces
public sealed class BenchmarkNotificationHandler
    : Mocha.Mediator.INotificationHandler<BenchmarkNotification>,
      MediatR.INotificationHandler<BenchmarkNotification>
{
    public ValueTask HandleAsync(
        BenchmarkNotification notification,
        CancellationToken cancellationToken)
        => default;

    Task MediatR.INotificationHandler<BenchmarkNotification>.Handle(
        BenchmarkNotification notification,
        CancellationToken cancellationToken)
        => Task.CompletedTask;
}

// Mocha middleware (replaces pipeline behavior)
public sealed class BenchmarkMochaMiddleware
{
    public async ValueTask InvokeAsync(Mocha.Mediator.IMediatorContext context, Mocha.Mediator.MediatorDelegate next)
    {
        await next(context);
    }

    public static Mocha.Mediator.MediatorMiddlewareConfiguration Create()
        => new(
            static (_, next) =>
            {
                var middleware = new BenchmarkMochaMiddleware();
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "BenchmarkBehavior");
}

// MediatR pipeline behavior
public sealed class BenchmarkMediatRPipelineBehavior
    : MediatR.IPipelineBehavior<BenchmarkCommand, BenchmarkResponse>
{
    public async Task<BenchmarkResponse> Handle(
        BenchmarkCommand request,
        MediatR.RequestHandlerDelegate<BenchmarkResponse> next,
        CancellationToken cancellationToken)
        => await next();
}
