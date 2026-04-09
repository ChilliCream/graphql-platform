namespace Mocha.Testing.Example;

/// <summary>
/// Validates the order and publishes <see cref="OrderPlaced"/> (or <see cref="OrderCancelled"/>).
/// </summary>
public sealed class PlaceOrderHandler(IMessageBus bus) : IEventHandler<PlaceOrder>
{
    public async ValueTask HandleAsync(PlaceOrder message, CancellationToken cancellationToken)
    {
        if (message.IsCancelled)
        {
            await bus.PublishAsync(new OrderCancelled { OrderId = message.OrderId }, cancellationToken);
            return;
        }

        await bus.PublishAsync(
            new OrderPlaced { OrderId = message.OrderId, Amount = message.Amount },
            cancellationToken);
    }
}

/// <summary>
/// Reacts to <see cref="OrderPlaced"/> by sending <see cref="ProcessPayment"/>.
/// </summary>
public sealed class OrderPlacedHandler(IMessageBus bus) : IEventHandler<OrderPlaced>
{
    public async ValueTask HandleAsync(OrderPlaced message, CancellationToken cancellationToken)
    {
        await bus.SendAsync(
            new ProcessPayment { OrderId = message.OrderId, Amount = message.Amount },
            cancellationToken);
    }
}

/// <summary>
/// Reacts to <see cref="PaymentCompleted"/> by sending <see cref="ShipOrder"/>.
/// </summary>
public sealed class PaymentCompletedHandler(IMessageBus bus) : IEventHandler<PaymentCompleted>
{
    public async ValueTask HandleAsync(PaymentCompleted message, CancellationToken cancellationToken)
    {
        await bus.SendAsync(new ShipOrder { OrderId = message.OrderId }, cancellationToken);
    }
}

/// <summary>
/// Handles <see cref="ShipOrder"/> and publishes <see cref="OrderShipped"/>.
/// </summary>
public sealed class ShipOrderHandler(IMessageBus bus) : IEventRequestHandler<ShipOrder>
{
    public async ValueTask HandleAsync(ShipOrder message, CancellationToken cancellationToken)
    {
        await bus.PublishAsync(
            new OrderShipped { OrderId = message.OrderId, TrackingNumber = $"TRK-{message.OrderId}" },
            cancellationToken);
    }
}

/// <summary>
/// Handles <see cref="ProcessPayment"/> and publishes <see cref="PaymentCompleted"/>.
/// </summary>
public sealed class ProcessPaymentHandler(IMessageBus bus) : IEventRequestHandler<ProcessPayment>
{
    public async ValueTask HandleAsync(ProcessPayment message, CancellationToken cancellationToken)
    {
        await bus.PublishAsync(new PaymentCompleted { OrderId = message.OrderId }, cancellationToken);
    }
}

/// <summary>
/// A handler that always throws — used to demonstrate failure tracking.
/// </summary>
public sealed class FailingPaymentHandler : IEventRequestHandler<ProcessPayment>
{
    public ValueTask HandleAsync(ProcessPayment message, CancellationToken cancellationToken)
        => throw new InvalidOperationException($"Payment gateway unavailable for order {message.OrderId}");
}

// Leaf handlers — terminal consumers that don't produce further messages.
// Required because WaitForCompletionAsync waits for ALL dispatched messages to be consumed.

public sealed class OrderPlacedSink : IEventHandler<OrderPlaced>
{
    public ValueTask HandleAsync(OrderPlaced message, CancellationToken cancellationToken) => default;
}

public sealed class OrderShippedSink : IEventHandler<OrderShipped>
{
    public ValueTask HandleAsync(OrderShipped message, CancellationToken cancellationToken) => default;
}

public sealed class OrderCancelledSink : IEventHandler<OrderCancelled>
{
    public ValueTask HandleAsync(OrderCancelled message, CancellationToken cancellationToken) => default;
}
