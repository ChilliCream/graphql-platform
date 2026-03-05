using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;

namespace Mocha.Tests;

public class MessageTypeRegistryTests
{
    [Fact]
    public void GetMessageType_Should_ReturnSameInstance_When_LookingUpByTypeAndIdentity()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // act
        var byType = runtime.Messages.GetMessageType(typeof(OrderCreated));
        Assert.NotNull(byType);
        var byIdentity = runtime.Messages.GetMessageType(byType.Identity);

        // assert
        Assert.Same(byType, byIdentity);
    }

    [Fact]
    public void GetMessageType_Should_ReturnCorrectType_When_MultipleTypesRegistered()
    {
        // arrange
        var runtime = CreateRuntime(b =>
        {
            b.AddEventHandler<OrderCreatedHandler>();
            b.AddEventHandler<ItemShippedHandler>();
        });

        // act
        var orderType = runtime.Messages.GetMessageType(typeof(OrderCreated));
        var itemType = runtime.Messages.GetMessageType(typeof(ItemShipped));

        // assert
        Assert.NotNull(orderType);
        Assert.NotNull(itemType);
        Assert.Equal(typeof(OrderCreated), orderType.RuntimeType);
        Assert.Equal(typeof(ItemShipped), itemType.RuntimeType);
        Assert.NotSame(orderType, itemType);
        Assert.NotEqual(orderType.Identity, itemType.Identity);
    }

    [Fact]
    public void AddMessageType_Should_NotDuplicate_When_SameTypeAddedTwice()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated))!;
        var countBefore = runtime.Messages.MessageTypes.Count;

        // act
        runtime.Messages.AddMessageType(messageType);

        // assert
        Assert.Equal(countBefore, runtime.Messages.MessageTypes.Count);
    }

    [Fact]
    public void GetOrAdd_Should_ReturnExistingMessageType_When_TypeAlreadyRegistered()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var existing = runtime.Messages.GetMessageType(typeof(OrderCreated));
        Assert.NotNull(existing);

        // act
        var result = runtime.GetMessageType(typeof(OrderCreated));

        // assert
        Assert.Same(existing, result);
    }

    [Fact]
    public void GetOrAdd_Should_CreateAndRegisterNewType_When_TypeNotRegistered()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        Assert.Null(runtime.Messages.GetMessageType(typeof(UnregisteredEvent)));

        // act
        var result = runtime.GetMessageType(typeof(UnregisteredEvent));

        // assert
        Assert.NotNull(result);
        Assert.Equal(typeof(UnregisteredEvent), result.RuntimeType);

        // verify retrievable by both Type and Identity
        Assert.Same(result, runtime.Messages.GetMessageType(typeof(UnregisteredEvent)));
        Assert.Same(result, runtime.Messages.GetMessageType(result.Identity));
    }

    [Fact]
    public void GetOrAdd_Should_MarkTypeAsCompleted_When_Created()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // act
        var result = runtime.GetMessageType(typeof(UnregisteredEvent));

        // assert
        Assert.True(result.IsCompleted);
    }

    [Fact]
    public void GetOrAdd_Should_ReturnSameInstance_When_ConcurrentCallsForSameType()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var results = new MessageType[10];
        var barrier = new Barrier(10);

        // act
        var threads = Enumerable
            .Range(0, 10)
            .Select(i => new Thread(() =>
            {
                barrier.SignalAndWait();
                results[i] = runtime.GetMessageType(typeof(UnregisteredEvent));
            }))
            .ToArray();

        foreach (var thread in threads)
            thread.Start();
        foreach (var thread in threads)
            thread.Join();

        // assert
        Assert.All(results, r => Assert.NotNull(r));
        Assert.All(results, r => Assert.Same(results[0], r));
    }

    [Fact]
    public void MessageTypes_Should_ContainCorrectCount_When_MultipleTypesRegistered()
    {
        // arrange & act
        var runtime = CreateRuntime(b =>
        {
            b.AddEventHandler<OrderCreatedHandler>();
            b.AddEventHandler<ItemShippedHandler>();
            b.AddRequestHandler<ProcessPaymentHandler>();
        });

        // assert — count includes concrete types plus hierarchy types (IEventRequest, etc.)
        Assert.True(runtime.Messages.MessageTypes.Count >= 3);
    }

    [Fact]
    public void MessageTypes_Should_ContainAllRegisteredTypes_When_Queried()
    {
        // arrange
        var runtime = CreateRuntime(b =>
        {
            b.AddEventHandler<OrderCreatedHandler>();
            b.AddEventHandler<ItemShippedHandler>();
        });

        // act
        var orderType = runtime.Messages.GetMessageType(typeof(OrderCreated));
        var itemType = runtime.Messages.GetMessageType(typeof(ItemShipped));

        // assert
        Assert.NotNull(orderType);
        Assert.NotNull(itemType);
        Assert.Contains(orderType, runtime.Messages.MessageTypes);
        Assert.Contains(itemType, runtime.Messages.MessageTypes);
    }

    public sealed class OrderCreated
    {
        public string OrderId { get; init; } = "";
    }

    public sealed class ItemShipped
    {
        public string TrackingNumber { get; init; } = "";
    }

    public sealed class ProcessPayment
    {
        public decimal Amount { get; init; }
    }

    public sealed class UnregisteredEvent
    {
        public string Data { get; init; } = "";
    }

    public sealed class OrderCreatedHandler : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken) => default;
    }

    public sealed class ItemShippedHandler : IEventHandler<ItemShipped>
    {
        public ValueTask HandleAsync(ItemShipped message, CancellationToken cancellationToken) => default;
    }

    public sealed class ProcessPaymentHandler : IEventRequestHandler<ProcessPayment>
    {
        public ValueTask HandleAsync(ProcessPayment request, CancellationToken cancellationToken) => default;
    }

    private static MessagingRuntime CreateRuntime(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        return (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
    }
}
