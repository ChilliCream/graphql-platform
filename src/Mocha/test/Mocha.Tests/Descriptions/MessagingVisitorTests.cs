using Microsoft.Extensions.DependencyInjection;
using Mocha.Sagas;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.Descriptions;

public class MessagingVisitorTests
{
    [Fact]
    public void Visitor_Should_CallEnterAndLeaveRuntime_When_Visiting()
    {
        // arrange
        var runtime = CreateRuntime(b =>
            b.AddEventHandler<TestEventHandler>());
        var visitor = new RecordingVisitor();

        // act
        visitor.Visit(runtime, new RecordingContext());

        // assert
        Assert.Contains("Enter:Runtime", visitor.Context.Calls);
        Assert.Contains("Leave:Runtime", visitor.Context.Calls);
    }

    [Fact]
    public void Visitor_Should_CallEnterMessageType_When_RuntimeHasMessageTypes()
    {
        // arrange
        var runtime = CreateRuntime(b =>
            b.AddEventHandler<TestEventHandler>());
        var visitor = new RecordingVisitor();

        // act
        visitor.Visit(runtime, new RecordingContext());

        // assert
        Assert.Contains(visitor.Context.Calls, c => c.StartsWith("Enter:MessageType:"));
    }

    [Fact]
    public void Visitor_Should_CallEnterConsumer_When_RuntimeHasConsumers()
    {
        // arrange
        var runtime = CreateRuntime(b =>
            b.AddEventHandler<TestEventHandler>());
        var visitor = new RecordingVisitor();

        // act
        visitor.Visit(runtime, new RecordingContext());

        // assert
        Assert.Contains(visitor.Context.Calls, c => c.StartsWith("Enter:Consumer:"));
    }

    [Fact]
    public void Visitor_Should_CallEnterTransport_When_RuntimeHasTransports()
    {
        // arrange
        var runtime = CreateRuntime(b =>
            b.AddEventHandler<TestEventHandler>());
        var visitor = new RecordingVisitor();

        // act
        visitor.Visit(runtime, new RecordingContext());

        // assert
        Assert.Contains(visitor.Context.Calls, c => c.StartsWith("Enter:Transport:"));
    }

    [Fact]
    public void Visitor_Should_CallEnterInboundRoute_When_RuntimeHasRoutes()
    {
        // arrange
        var runtime = CreateRuntime(b =>
            b.AddEventHandler<TestEventHandler>());
        var visitor = new RecordingVisitor();

        // act
        visitor.Visit(runtime, new RecordingContext());

        // assert
        Assert.Contains(visitor.Context.Calls, c => c.StartsWith("Enter:InboundRoute"));
    }

    [Fact]
    public void Visitor_Should_VisitInCorrectOrder_When_FullRuntime()
    {
        // arrange
        var runtime = CreateRuntime(b =>
            b.AddEventHandler<TestEventHandler>());
        var visitor = new RecordingVisitor();

        // act
        visitor.Visit(runtime, new RecordingContext());

        // assert - Enter:Runtime should be first, Leave:Runtime should be last
        Assert.Equal("Enter:Runtime", visitor.Context.Calls.First());
        Assert.Equal("Leave:Runtime", visitor.Context.Calls.Last());

        // MessageTypes come before Consumers in the visitor
        var firstMessageType = visitor.Context.Calls.FindIndex(c => c.StartsWith("Enter:MessageType:"));
        var firstConsumer = visitor.Context.Calls.FindIndex(c => c.StartsWith("Enter:Consumer:"));
        Assert.True(firstMessageType < firstConsumer, "MessageTypes should be visited before Consumers");

        // Consumers come before InboundRoutes
        var firstInboundRoute = visitor.Context.Calls.FindIndex(c => c.StartsWith("Enter:InboundRoute"));
        Assert.True(firstConsumer < firstInboundRoute, "Consumers should be visited before InboundRoutes");
    }

    [Fact]
    public void Visitor_Should_StopImmediately_When_EnterRuntime_ReturnsBreak()
    {
        // arrange
        var runtime = CreateRuntime(b =>
            b.AddEventHandler<TestEventHandler>());
        var visitor = new BreakOnRuntimeVisitor();

        // act
        visitor.Visit(runtime, new RecordingContext());

        // assert - only the Enter:Runtime call, nothing else
        Assert.Single(visitor.Context.Calls);
        Assert.Equal("Enter:Runtime", visitor.Context.Calls[0]);
    }

    [Fact]
    public void Visitor_Should_Stop_When_EnterMessageType_ReturnsBreak()
    {
        // arrange
        var runtime = CreateRuntime(b =>
            b.AddEventHandler<TestEventHandler>());
        var visitor = new BreakOnFirstMessageTypeVisitor();

        // act
        visitor.Visit(runtime, new RecordingContext());

        // assert - should have Enter:Runtime + Enter:MessageType, but no consumers/transports
        Assert.Contains("Enter:Runtime", visitor.Context.Calls);
        Assert.Contains(visitor.Context.Calls, c => c.StartsWith("Enter:MessageType:"));
        // No consumers should be visited after break in VisitChildren
        Assert.DoesNotContain(visitor.Context.Calls, c => c.StartsWith("Enter:Consumer:"));
        // Leave:Runtime IS called because Visit() calls Leave after VisitChildren returns
        Assert.Contains("Leave:Runtime", visitor.Context.Calls);
    }

    [Fact]
    public void Visitor_Should_SkipLeave_When_EnterMessageType_ReturnsSkip()
    {
        // arrange
        var runtime = CreateRuntime(b =>
            b.AddEventHandler<TestEventHandler>());
        var visitor = new SkipAllMessageTypesVisitor();

        // act
        visitor.Visit(runtime, new RecordingContext());

        // assert - Enter:MessageType calls should exist but no Leave:MessageType calls
        Assert.Contains(visitor.Context.Calls, c => c.StartsWith("Enter:MessageType:"));
        Assert.DoesNotContain(visitor.Context.Calls, c => c.StartsWith("Leave:MessageType:"));
        // But other node types should still be visited
        Assert.Contains(visitor.Context.Calls, c => c.StartsWith("Enter:Consumer:"));
    }

    [Fact]
    public void Visitor_Should_ContinueNormally_When_Enter_ReturnsContinue()
    {
        // arrange
        var runtime = CreateRuntime(b =>
            b.AddEventHandler<TestEventHandler>());
        var visitor = new RecordingVisitor();

        // act
        visitor.Visit(runtime, new RecordingContext());

        // assert - both Enter and Leave calls should be present for each message type
        var enterCount = visitor.Context.Calls.Count(c => c.StartsWith("Enter:MessageType:"));
        var leaveCount = visitor.Context.Calls.Count(c => c.StartsWith("Leave:MessageType:"));
        Assert.Equal(enterCount, leaveCount);
    }

    [Fact]
    public void Visitor_Should_StopTransportTraversal_When_EnterTransport_ReturnsBreak()
    {
        // arrange
        var runtime = CreateRuntime(b =>
            b.AddEventHandler<TestEventHandler>());
        var visitor = new BreakOnTransportVisitor();

        // act
        visitor.Visit(runtime, new RecordingContext());

        // assert - should have Enter:Transport but no Leave:Transport (break prevents it)
        Assert.Contains(visitor.Context.Calls, c => c.StartsWith("Enter:Transport:"));
        Assert.DoesNotContain(visitor.Context.Calls, c => c.StartsWith("Leave:Transport:"));
        // Leave:Runtime IS still called - Break from VisitChildren does not prevent
        // Visit() from calling Leave(runtime) since VisitChildren has void return type
        Assert.Contains("Leave:Runtime", visitor.Context.Calls);
        // But no saga visits should occur after the transport break
        Assert.DoesNotContain(visitor.Context.Calls, c => c.StartsWith("Enter:Saga:"));
    }

    [Fact]
    public void Visitor_Should_SkipTransportChildren_When_EnterTransport_ReturnsSkip()
    {
        // arrange
        var runtime = CreateRuntime(b =>
            b.AddEventHandler<TestEventHandler>());
        var visitor = new SkipTransportVisitor();

        // act
        visitor.Visit(runtime, new RecordingContext());

        // assert - Enter:Transport present but no endpoint visits under it
        Assert.Contains(visitor.Context.Calls, c => c.StartsWith("Enter:Transport:"));
        // No ReceiveEndpoint or DispatchEndpoint enters since transport was skipped
        Assert.DoesNotContain(visitor.Context.Calls, c => c.StartsWith("Enter:ReceiveEndpoint:"));
        Assert.DoesNotContain(visitor.Context.Calls, c => c.StartsWith("Enter:DispatchEndpoint:"));
        // Leave:Runtime should still be called since Skip doesn't break the entire traversal
        Assert.Contains("Leave:Runtime", visitor.Context.Calls);
    }

    [Fact]
    public void Visitor_Should_VisitSaga_When_SagaConsumer_Present()
    {
        // arrange
        var runtime = CreateRuntime(b =>
            b.ConfigureMessageBus(h => ((MessageBusBuilder)h).AddSaga<TestOrderSaga>()));
        var visitor = new RecordingVisitor();

        // act
        visitor.Visit(runtime, new RecordingContext());

        // assert
        Assert.Contains(visitor.Context.Calls, c => c.StartsWith("Enter:Saga:"));
        Assert.Contains(visitor.Context.Calls, c => c.StartsWith("Leave:Saga:"));
    }

    [Fact]
    public void Visitor_Should_NotVisitSaga_When_NoSagaConsumer()
    {
        // arrange
        var runtime = CreateRuntime(b =>
            b.AddEventHandler<TestEventHandler>());
        var visitor = new RecordingVisitor();

        // act
        visitor.Visit(runtime, new RecordingContext());

        // assert
        Assert.DoesNotContain(visitor.Context.Calls, c => c.StartsWith("Enter:Saga:"));
    }

    [Fact]
    public void Visitor_Should_VisitReceiveEndpointsUnderTransport_When_Present()
    {
        // arrange
        var runtime = CreateRuntime(b =>
            b.AddEventHandler<TestEventHandler>());
        var visitor = new RecordingVisitor();

        // act
        visitor.Visit(runtime, new RecordingContext());

        // assert - there should be receive endpoints under the transport
        Assert.Contains(visitor.Context.Calls, c => c.StartsWith("Enter:ReceiveEndpoint:"));
        Assert.Contains(visitor.Context.Calls, c => c.StartsWith("Leave:ReceiveEndpoint:"));
    }

    [Fact]
    public void Visitor_Should_CallLeaveTransportAfterEndpoints_When_Visiting()
    {
        // arrange
        var runtime = CreateRuntime(b =>
            b.AddEventHandler<TestEventHandler>());
        var visitor = new RecordingVisitor();

        // act
        visitor.Visit(runtime, new RecordingContext());

        // assert - Leave:Transport should come after any endpoint visits
        var transportEnterIdx = visitor.Context.Calls.FindIndex(c => c.StartsWith("Enter:Transport:"));
        var transportLeaveIdx = visitor.Context.Calls.FindIndex(c => c.StartsWith("Leave:Transport:"));

        Assert.True(transportEnterIdx >= 0, "Should have Enter:Transport");
        Assert.True(transportLeaveIdx >= 0, "Should have Leave:Transport");
        Assert.True(transportLeaveIdx > transportEnterIdx, "Leave:Transport should come after Enter:Transport");

        // Any endpoints should be between Enter and Leave of transport
        var endpointCalls = visitor
            .Context.Calls.Select((call, idx) => (call, idx))
            .Where(x => x.call.StartsWith("Enter:ReceiveEndpoint:") || x.call.StartsWith("Enter:DispatchEndpoint:"))
            .ToList();

        foreach (var (call, idx) in endpointCalls)
        {
            Assert.True(
                idx > transportEnterIdx && idx < transportLeaveIdx,
                $"Endpoint '{call}' at index {idx} should be between transport Enter ({transportEnterIdx}) and Leave ({transportLeaveIdx})");
        }
    }

    [Fact]
    public void Visitor_Should_StopOnConsumerBreak_When_VisitChildren_Iterating()
    {
        // arrange
        var runtime = CreateRuntime(b =>
        {
            b.AddEventHandler<TestEventHandler>();
            b.AddRequestHandler<TestRequestHandler>();
        });
        var visitor = new BreakOnFirstConsumerVisitor();

        // act
        visitor.Visit(runtime, new RecordingContext());

        // assert - should have exactly one Enter:Consumer (the first one triggers break)
        var consumerEnters = visitor.Context.Calls.Count(c => c.StartsWith("Enter:Consumer:"));
        Assert.Equal(1, consumerEnters);
        // No transports visited after consumer break
        Assert.DoesNotContain(visitor.Context.Calls, c => c.StartsWith("Enter:Transport:"));
    }

    [Fact]
    public void VisitorAction_Should_HaveThreeValues()
    {
        // assert
        var values = Enum.GetValues<VisitorAction>();
        Assert.Equal(3, values.Length);
        Assert.Contains(VisitorAction.Continue, values);
        Assert.Contains(VisitorAction.Skip, values);
        Assert.Contains(VisitorAction.Break, values);
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

    public sealed class RecordingContext
    {
        public List<string> Calls { get; } = [];
    }

    internal class RecordingVisitor : MessagingVisitor<RecordingContext>
    {
        public RecordingContext Context { get; private set; } = new();

        public new void Visit(MessagingRuntime runtime, RecordingContext context)
        {
            Context = context;
            base.Visit(runtime, context);
        }

        protected override VisitorAction Enter(MessagingRuntime runtime, RecordingContext context)
        {
            context.Calls.Add("Enter:Runtime");
            return VisitorAction.Continue;
        }

        protected override VisitorAction Leave(MessagingRuntime runtime, RecordingContext context)
        {
            context.Calls.Add("Leave:Runtime");
            return VisitorAction.Continue;
        }

        protected override VisitorAction Enter(MessageType messageType, RecordingContext context)
        {
            context.Calls.Add($"Enter:MessageType:{messageType.Identity}");
            return VisitorAction.Continue;
        }

        protected override VisitorAction Leave(MessageType messageType, RecordingContext context)
        {
            context.Calls.Add($"Leave:MessageType:{messageType.Identity}");
            return VisitorAction.Continue;
        }

        protected override VisitorAction Enter(Consumer consumer, RecordingContext context)
        {
            context.Calls.Add($"Enter:Consumer:{consumer.Name}");
            return VisitorAction.Continue;
        }

        protected override VisitorAction Leave(Consumer consumer, RecordingContext context)
        {
            context.Calls.Add($"Leave:Consumer:{consumer.Name}");
            return VisitorAction.Continue;
        }

        protected override VisitorAction Enter(InboundRoute route, RecordingContext context)
        {
            context.Calls.Add($"Enter:InboundRoute:{route.Kind}");
            return VisitorAction.Continue;
        }

        protected override VisitorAction Leave(InboundRoute route, RecordingContext context)
        {
            context.Calls.Add($"Leave:InboundRoute:{route.Kind}");
            return VisitorAction.Continue;
        }

        protected override VisitorAction Enter(OutboundRoute route, RecordingContext context)
        {
            context.Calls.Add($"Enter:OutboundRoute:{route.Kind}");
            return VisitorAction.Continue;
        }

        protected override VisitorAction Leave(OutboundRoute route, RecordingContext context)
        {
            context.Calls.Add($"Leave:OutboundRoute:{route.Kind}");
            return VisitorAction.Continue;
        }

        protected override VisitorAction Enter(MessagingTransport transport, RecordingContext context)
        {
            context.Calls.Add($"Enter:Transport:{transport.Name}");
            return VisitorAction.Continue;
        }

        protected override VisitorAction Leave(MessagingTransport transport, RecordingContext context)
        {
            context.Calls.Add($"Leave:Transport:{transport.Name}");
            return VisitorAction.Continue;
        }

        protected override VisitorAction Enter(ReceiveEndpoint endpoint, RecordingContext context)
        {
            context.Calls.Add($"Enter:ReceiveEndpoint:{endpoint.Name}");
            return VisitorAction.Continue;
        }

        protected override VisitorAction Leave(ReceiveEndpoint endpoint, RecordingContext context)
        {
            context.Calls.Add($"Leave:ReceiveEndpoint:{endpoint.Name}");
            return VisitorAction.Continue;
        }

        protected override VisitorAction Enter(DispatchEndpoint endpoint, RecordingContext context)
        {
            context.Calls.Add($"Enter:DispatchEndpoint:{endpoint.Name}");
            return VisitorAction.Continue;
        }

        protected override VisitorAction Leave(DispatchEndpoint endpoint, RecordingContext context)
        {
            context.Calls.Add($"Leave:DispatchEndpoint:{endpoint.Name}");
            return VisitorAction.Continue;
        }

        protected override VisitorAction Enter(Saga saga, RecordingContext context)
        {
            context.Calls.Add($"Enter:Saga:{saga.Name}");
            return VisitorAction.Continue;
        }

        protected override VisitorAction Leave(Saga saga, RecordingContext context)
        {
            context.Calls.Add($"Leave:Saga:{saga.Name}");
            return VisitorAction.Continue;
        }
    }

    internal sealed class BreakOnRuntimeVisitor : RecordingVisitor
    {
        protected override VisitorAction Enter(MessagingRuntime runtime, RecordingContext context)
        {
            context.Calls.Add("Enter:Runtime");
            return VisitorAction.Break;
        }
    }

    internal sealed class BreakOnFirstMessageTypeVisitor : RecordingVisitor
    {
        protected override VisitorAction Enter(MessageType messageType, RecordingContext context)
        {
            context.Calls.Add($"Enter:MessageType:{messageType.Identity}");
            return VisitorAction.Break;
        }
    }

    internal sealed class SkipAllMessageTypesVisitor : RecordingVisitor
    {
        protected override VisitorAction Enter(MessageType messageType, RecordingContext context)
        {
            context.Calls.Add($"Enter:MessageType:{messageType.Identity}");
            return VisitorAction.Skip;
        }
    }

    internal sealed class BreakOnTransportVisitor : RecordingVisitor
    {
        protected override VisitorAction Enter(MessagingTransport transport, RecordingContext context)
        {
            context.Calls.Add($"Enter:Transport:{transport.Name}");
            return VisitorAction.Break;
        }
    }

    internal sealed class SkipTransportVisitor : RecordingVisitor
    {
        protected override VisitorAction Enter(MessagingTransport transport, RecordingContext context)
        {
            context.Calls.Add($"Enter:Transport:{transport.Name}");
            return VisitorAction.Skip;
        }
    }

    internal sealed class BreakOnFirstConsumerVisitor : RecordingVisitor
    {
        protected override VisitorAction Enter(Consumer consumer, RecordingContext context)
        {
            context.Calls.Add($"Enter:Consumer:{consumer.Name}");
            return VisitorAction.Break;
        }
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
