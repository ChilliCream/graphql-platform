namespace Mocha.Testing.Example;

// Commands — intent to perform an action.

public sealed class PlaceOrder
{
    public required string OrderId { get; init; }
    public required decimal Amount { get; init; }
    public bool IsCancelled { get; init; }
}

public sealed class ProcessPayment
{
    public required string OrderId { get; init; }
    public required decimal Amount { get; init; }
}

public sealed class ShipOrder
{
    public required string OrderId { get; init; }
}

// Events — something that happened.

public sealed class OrderPlaced
{
    public required string OrderId { get; init; }
    public required decimal Amount { get; init; }
}

public sealed class PaymentCompleted
{
    public required string OrderId { get; init; }
}

public sealed class OrderShipped
{
    public required string OrderId { get; init; }
    public required string TrackingNumber { get; init; }
}

public sealed class OrderCancelled
{
    public required string OrderId { get; init; }
}
