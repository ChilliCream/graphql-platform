namespace Mocha.Mediator.Benchmarks.Messaging;

// Command that implements both Mocha and MediatR interfaces
public sealed record BenchmarkCommand(Guid Id)
    : ICommand<BenchmarkResponse>,
      MediatR.IRequest<BenchmarkResponse>;

// Response type shared by both
public sealed record BenchmarkResponse(Guid Id);

// Notification that implements both Mocha and MediatR interfaces
public sealed record BenchmarkNotification(Guid Id)
    : INotification,
      MediatR.INotification;

// Command handler implementing both interfaces
public sealed class BenchmarkCommandHandler
    : ICommandHandler<BenchmarkCommand, BenchmarkResponse>,
      MediatR.IRequestHandler<BenchmarkCommand, BenchmarkResponse>
{
    private static readonly BenchmarkResponse s_response = new(Guid.NewGuid());

    public async ValueTask<BenchmarkResponse> HandleAsync(
        BenchmarkCommand command,
        CancellationToken cancellationToken)
    {
        await Task.Yield();
        return s_response;
    }

    async Task<BenchmarkResponse> MediatR.IRequestHandler<BenchmarkCommand, BenchmarkResponse>.Handle(
        BenchmarkCommand request,
        CancellationToken cancellationToken)
    {
        await Task.Yield();
        return s_response;
    }
}

// Notification handler implementing both interfaces
public sealed class BenchmarkNotificationHandler
    : INotificationHandler<BenchmarkNotification>,
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
    public async ValueTask InvokeAsync(IMediatorContext context, MediatorDelegate next)
    {
        await next(context);
    }

    public static MediatorMiddlewareConfiguration Create()
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
