using Microsoft.Extensions.DependencyInjection;
using Mocha.Sagas;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.Descriptions;

public class MessageBusDescriptionVisitorTests
{
    [Fact]
    public void Visit_Should_DescribeEventHandler_When_Registered()
    {
        // arrange
        var runtime = CreateRuntime(b =>
            b.AddEventHandler<TestEventHandler>());

        // act
        var description = MessageBusDescriptionVisitor.Visit(runtime);

        // assert
        Assert.NotNull(description.Host);
        Assert.NotNull(description.Host.InstanceId);
        Assert.NotEmpty(description.Host.InstanceId);

        Assert.NotEmpty(description.MessageTypes);
        Assert.Contains(description.MessageTypes, mt => mt.RuntimeType == nameof(TestEvent));

        Assert.NotEmpty(description.Consumers);
        Assert.Contains(description.Consumers, c => c.Name == nameof(TestEventHandler));

        Assert.NotEmpty(description.Routes.Inbound);
        Assert.NotEmpty(description.Transports);
        Assert.Null(description.Sagas);
    }

    [Fact]
    public void Visit_Should_DescribeRequestHandler_When_Registered()
    {
        // arrange
        var runtime = CreateRuntime(b =>
            b.AddRequestHandler<TestRequestHandler>());

        // act
        var description = MessageBusDescriptionVisitor.Visit(runtime);

        // assert
        Assert.NotNull(description.Host);
        Assert.NotEmpty(description.Consumers);
        Assert.NotNull(description.Routes);
        Assert.Null(description.Sagas);
    }

    [Fact]
    public void Visit_Should_DescribeMultipleHandlers_When_Registered()
    {
        // arrange
        var runtime = CreateRuntime(b =>
        {
            b.AddEventHandler<TestEventHandler>();
            b.AddRequestHandler<TestRequestHandler>();
        });

        // act
        var description = MessageBusDescriptionVisitor.Visit(runtime);

        // assert
        Assert.True(description.Consumers.Count >= 2);
        Assert.Contains(description.Consumers, c => c.Name == nameof(TestEventHandler));
        Assert.NotEmpty(description.Routes.Inbound);
    }

    [Fact]
    public void Visit_Should_DescribeSaga_When_Registered()
    {
        // arrange
        var runtime = CreateRuntime(b =>
            b.ConfigureMessageBus(h => ((MessageBusBuilder)h).AddSaga<TestOrderSaga>()));

        // act
        var description = MessageBusDescriptionVisitor.Visit(runtime);

        // assert
        Assert.NotNull(description.Sagas);
        Assert.NotEmpty(description.Sagas!);

        var saga = Assert.Single(description.Sagas!);
        Assert.NotEmpty(saga.States);
        Assert.Contains(saga.States, s => s.Name == "__Initial");

        var sagaConsumer = description.Consumers.FirstOrDefault(c => c.SagaName is not null);
        Assert.NotNull(sagaConsumer);
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

    public sealed class TestEvent
    {
        public string Data { get; init; } = "";
    }

    public sealed class TestEventHandler : IEventHandler<TestEvent>
    {
        public ValueTask HandleAsync(TestEvent message, CancellationToken cancellationToken) => default;
    }

    public sealed class TestRequest
    {
        public string RequestData { get; init; } = "";
    }

    public sealed class TestRequestHandler : IEventRequestHandler<TestRequest>
    {
        public ValueTask HandleAsync(TestRequest request, CancellationToken cancellationToken) => default;
    }

    public sealed class OrderPlaced
    {
        public string OrderId { get; init; } = "";
        public decimal Total { get; init; }
    }

    public sealed class PaymentReceived
    {
        public string OrderId { get; init; } = "";
    }

    public sealed class TestOrderSagaState : SagaStateBase
    {
        public string OrderId { get; set; } = "";
        public decimal Total { get; set; }
    }

    public sealed class TestOrderSaga : Saga<TestOrderSagaState>
    {
        protected override void Configure(ISagaDescriptor<TestOrderSagaState> descriptor)
        {
            descriptor
                .Initially()
                .OnEvent<OrderPlaced>()
                .StateFactory(e => new TestOrderSagaState { OrderId = e.OrderId, Total = e.Total })
                .TransitionTo("AwaitingPayment");

            descriptor
                .During("AwaitingPayment")
                .OnEvent<PaymentReceived>()
                .Then((_, _) => { })
                .TransitionTo("Completed");

            descriptor.Finally("Completed");
        }
    }
}
