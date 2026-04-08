namespace Mocha.Mediator.Benchmarks.Messaging;

// Command for martinothamar Mediator (source-generated)
public sealed record MediatorSgCommand(Guid Id) : global::Mediator.IRequest<BenchmarkResponse>;

// Notification for martinothamar Mediator
public sealed record MediatorSgNotification(Guid Id) : global::Mediator.INotification;

// Command handler for martinothamar Mediator
public sealed class MediatorSgCommandHandler
    : global::Mediator.IRequestHandler<MediatorSgCommand, BenchmarkResponse>
{
    private static readonly BenchmarkResponse _response = new(Guid.NewGuid());

    public async ValueTask<BenchmarkResponse> Handle(
        MediatorSgCommand request,
        CancellationToken cancellationToken)
        {
            await Task.Yield();
            return _response;
        }
}

// Notification handler for martinothamar Mediator
public sealed class MediatorSgNotificationHandler
    : global::Mediator.INotificationHandler<MediatorSgNotification>
{
    public ValueTask Handle(
        MediatorSgNotification notification,
        CancellationToken cancellationToken)
        => default;
}

// Pipeline behavior for martinothamar Mediator
public sealed class MediatorSgPipelineBehavior
    : global::Mediator.IPipelineBehavior<MediatorSgCommand, BenchmarkResponse>
{
    public ValueTask<BenchmarkResponse> Handle(
        MediatorSgCommand message,
        global::Mediator.MessageHandlerDelegate<MediatorSgCommand, BenchmarkResponse> next,
        CancellationToken cancellationToken)
        => next(message, cancellationToken);
}
