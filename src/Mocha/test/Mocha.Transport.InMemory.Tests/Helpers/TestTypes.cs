namespace Mocha.Transport.InMemory.Tests.Helpers;

public sealed class OrderCreatedHandler2 : IEventHandler<OrderCreated>
{
    public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken) => default;
}
