namespace Mocha.Mediator.Benchmarks.Messaging;

// SwitchMediator partial classsource generator populates the dispatch logic
[global::Mediator.Switch.SwitchMediator]
public partial class BenchmarkSwitchMediator;

// Command for SwitchMediator (ValueTask path)
public sealed record SwitchMediatorCommand(Guid Id)
    : global::Mediator.Switch.IRequest<BenchmarkResponse>;

// Notification for SwitchMediator
public sealed record SwitchMediatorNotification(Guid Id)
    : global::Mediator.Switch.INotification;

// Command handler (ValueTask variant for zero-alloc hot path)
public sealed class SwitchMediatorCommandHandler
    : global::Mediator.Switch.IValueRequestHandler<SwitchMediatorCommand, BenchmarkResponse>
{
    private static readonly BenchmarkResponse _response = new(Guid.NewGuid());

    public ValueTask<BenchmarkResponse> Handle(
        SwitchMediatorCommand request,
        CancellationToken cancellationToken)
        => new(_response);
}

// Notification handler (ValueTask variant)
public sealed class SwitchMediatorNotificationHandler
    : global::Mediator.Switch.IValueNotificationHandler<SwitchMediatorNotification>
{
    public ValueTask Handle(
        SwitchMediatorNotification notification,
        CancellationToken cancellationToken)
        => default;
}

// Pipeline behavior (ValueTask variant)
public sealed class SwitchMediatorPipelineBehavior
    : global::Mediator.Switch.IValuePipelineBehavior<SwitchMediatorCommand, BenchmarkResponse>
{
    public ValueTask<BenchmarkResponse> Handle(
        SwitchMediatorCommand request,
        global::Mediator.Switch.ValueRequestHandlerDelegate<BenchmarkResponse> next,
        CancellationToken cancellationToken)
        => next(cancellationToken);
}
