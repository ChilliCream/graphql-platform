using MassTransit;

namespace Mocha.Mediator.Benchmarks.Messaging;

// Command for MassTransit (plain POCO, no marker interfaces)
public sealed record MassTransitCommand(Guid Id);

// Response contract for MassTransit request/response
public sealed record MassTransitCommandResponse(Guid Id);

// Notification for MassTransit
public sealed record MassTransitNotification(Guid Id);

// Consumer handling request/response
public sealed class MassTransitCommandConsumer : IConsumer<MassTransitCommand>
{
    private static readonly MassTransitCommandResponse _response = new(Guid.NewGuid());

    public Task Consume(ConsumeContext<MassTransitCommand> context)
        => context.RespondAsync(_response);
}

// Consumer handling notification (event)
public sealed class MassTransitNotificationConsumer : IConsumer<MassTransitNotification>
{
    public Task Consume(ConsumeContext<MassTransitNotification> context)
        => Task.CompletedTask;
}
